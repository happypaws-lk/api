using Microsoft.AspNetCore.Authorization;

namespace HappyPaws.Api.Authorization;

/// <summary>
/// Marks the requirement that the requesting user must have a verified identity.
/// </summary>
public class IsVerifiedRequirement : IAuthorizationRequirement;
