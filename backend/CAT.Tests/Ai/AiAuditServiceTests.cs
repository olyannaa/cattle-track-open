using System.Security.Claims;
using System.Text.Json;
using CAT.Services.Ai;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace CAT.Tests.Ai;

public sealed class AiAuditServiceTests
{
    private static readonly Guid OrgId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    [Fact]
    public void Log_RedactsSensitiveFieldsBeforeQueueing()
    {
        var queue = new UserActionQueue();
        var service = new AiAuditService(CreateHttpContextAccessor(), queue);

        service.Log(OrgId, AiAuditActionTypes.LlmTurn, "success", new
        {
            ToolName = "find_animal",
            ApiKey = "real-key",
            Nested = new { Password = "secret", Tag = "523" }
        });

        Assert.True(queue.Reader.TryRead(out var dto));
        Assert.Equal(AiAuditActionTypes.LlmTurn, dto.ActionType);
        Assert.Equal("ai_assistant", dto.Table);
        Assert.NotNull(dto.AdditionalInfo);

        using var document = JsonDocument.Parse(dto.AdditionalInfo!);
        var details = document.RootElement.GetProperty("details");
        Assert.Equal("[REDACTED]", details.GetProperty("apiKey").GetString());
        Assert.Equal("[REDACTED]", details.GetProperty("nested").GetProperty("password").GetString());
        Assert.Equal("523", details.GetProperty("nested").GetProperty("tag").GetString());
    }

    private static IHttpContextAccessor CreateHttpContextAccessor()
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, UserId.ToString())
            }))
        };

        return new HttpContextAccessor { HttpContext = context };
    }
}
