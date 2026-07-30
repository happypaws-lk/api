FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "./"]
COPY ["src/HappyPaws.Api/HappyPaws.Api.csproj", "src/HappyPaws.Api/"]
COPY ["src/HappyPaws.Core/HappyPaws.Core.csproj", "src/HappyPaws.Core/"]
COPY ["src/HappyPaws.Infrastructure/HappyPaws.Infrastructure.csproj", "src/HappyPaws.Infrastructure/"]
RUN dotnet restore "src/HappyPaws.Api/HappyPaws.Api.csproj"
COPY . .
WORKDIR "/src/src/HappyPaws.Api"
RUN dotnet build "./HappyPaws.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./HappyPaws.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HappyPaws.Api.dll"]
