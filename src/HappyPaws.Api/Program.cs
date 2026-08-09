using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using HappyPaws.Api.Authorization;
using HappyPaws.Api.Extensions;
using HappyPaws.Api.Middleware;
using HappyPaws.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Enrichers.Sensitive;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure Serilog
builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.WithSensitiveDataMasking(options => { })
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
});

// 2. Add Database & Infrastructure Services
builder.Services.AddInfrastructure(builder.Configuration);

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDevServices();
}

// 3. Add Authentication & Authorization
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/chat"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Verified", policy => policy.Requirements.Add(new IsVerifiedRequirement()))
    .AddPolicy("Admin", policy => policy.RequireRole("Admin"));

builder.Services.AddScoped<IAuthorizationHandler, IsVerifiedAuthorizationHandler>();

// 4. Add CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 5. Add Caching & Compression
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("Listings30", p => p.Expire(TimeSpan.FromSeconds(30)).Tag("listings"));
    options.AddPolicy("UserProfile30", p => p.Expire(TimeSpan.FromSeconds(30)).Tag("users"));
});
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// 6. Add Rate Limiting
var rateLimitingDisabled = builder.Configuration.GetValue<bool>("RateLimiting:Disabled");
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("AuthLimiter", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = rateLimitingDisabled ? 10000 : 5;
    });

    options.AddPolicy("RegisterLimiter", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(1),
            PermitLimit = rateLimitingDisabled ? 10000 : 10
        });
    });

    options.AddPolicy("OtpLimiter", httpContext =>
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var key = userId ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(10),
            PermitLimit = rateLimitingDisabled ? 10000 : 3
        });
    });

    options.AddPolicy("ForgotPasswordLimiter", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(10),
            PermitLimit = rateLimitingDisabled ? 10000 : 3
        });
    });

    options.AddPolicy("SignupLimiter", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter($"signup:{ip}", _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(10),
            PermitLimit = rateLimitingDisabled ? 10000 : 3
        });
    });

    options.AddSlidingWindowLimiter("UploadLimiter", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = rateLimitingDisabled ? 10000 : 10;
        opt.SegmentsPerWindow = 2;
    });
});

// 7. Add MediatR & FluentValidation
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// 8. Add Exception Handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// 9. Add OpenAPI
builder.Services.AddOpenApi();

// 10. Add Health Checks
builder.Services.AddHealthChecks();

// Add SignalR
builder.Services.AddSignalR();

// 11. Initialize Firebase
var firebaseServiceAccountJsonBase64 = builder.Configuration["Firebase:ServiceAccountJson"];
if (!string.IsNullOrEmpty(firebaseServiceAccountJsonBase64))
{
    var serviceAccountJson = Encoding.UTF8.GetString(Convert.FromBase64String(firebaseServiceAccountJsonBase64));
    FirebaseAdmin.FirebaseApp.Create(new FirebaseAdmin.AppOptions
    {
#pragma warning disable CS0618
        Credential = Google.Apis.Auth.OAuth2.GoogleCredential.FromJson(serviceAccountJson)
#pragma warning restore CS0618
    });
}

var app = builder.Build();

// --- Configure HTTP Request Pipeline ---

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseStaticFiles();
}
else
{
    app.UseHsts();
}

if (app.Configuration.GetValue<bool>("Features:EnableApiDocs"))
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseCors("DefaultCors");
app.UseRateLimiter();
app.UseOutputCache();

app.UseAuthentication();
app.UseAuthorization();

// Map Health Check
app.MapHealthChecks("/healthz");

// Map SignalR Hub
app.MapHub<HappyPaws.Api.Hubs.ChatHub>("/hubs/chat");

// Map all Minimal API endpoints via reflection
app.MapEndpoints();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HappyPaws.Infrastructure.Data.HappyPawsDbContext>();
    
    // Automatically apply any pending EF Core migrations to the database on startup.
    // This is required because GitHub Actions cannot reach the private RDS instance to run it.
    await Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.MigrateAsync(db.Database);

    // TODO: Uncomment after testing the one-time admin setup wizard
    // if (app.Environment.IsDevelopment())
    // {
    //     var hasher = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.IPasswordHasher<HappyPaws.Core.Entities.User>>();
    //     await HappyPaws.Infrastructure.Data.Seeder.DemoDataSeeder.SeedAsync(db, hasher);
    // }
}

app.Run();
