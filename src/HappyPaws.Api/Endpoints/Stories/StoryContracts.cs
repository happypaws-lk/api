using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Endpoints.Stories;

public record CommunityStoryResponse(
    Guid Id,
    string Title,
    string Content,
    string? PhotoUrl,
    string? VideoUrl,
    List<string> Tags,
    Guid AuthorId,
    string AuthorName,
    DateTimeOffset CreatedAt
);

public record CreateCommunityStoryRequest(
    [FromForm] string Title,
    [FromForm] string Content,
    [FromForm] string? Tags
);
