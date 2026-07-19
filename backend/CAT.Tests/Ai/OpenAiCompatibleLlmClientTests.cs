using System.Net;
using System.Text;
using System.Text.Json;
using CAT.Services.Ai;
using Microsoft.Extensions.Options;
using Xunit;

namespace CAT.Tests.Ai;

public sealed class OpenAiCompatibleLlmClientTests
{
    [Fact]
    public async Task GetNextAsync_UsesConstrainedJsonByDefault()
    {
        var handler = new RecordingHandler();
        var client = new OpenAiCompatibleLlmClient(
            new HttpClient(handler),
            Options.Create(new AiAssistantOptions
            {
                Llm = new AiLlmClientOptions
                {
                    Provider = "ollama",
                    Endpoint = "http://llm.test/v1",
                    Model = "qwen3.5:9b",
                    MaxTokens = 999
                }
            }));

        var output = await client.GetNextAsync(new AiAgentSession(Guid.NewGuid(), "Покажи карточку 523"));

        Assert.Equal(AiAgentOutputType.FinalAnswer, output.Type);
        using var payload = JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal("none", payload.RootElement.GetProperty("reasoning_effort").GetString());
        Assert.Equal(512, payload.RootElement.GetProperty("max_tokens").GetInt32());
        Assert.False(payload.RootElement.GetProperty("stream").GetBoolean());
        Assert.False(payload.RootElement.TryGetProperty("tools", out _));
        Assert.False(payload.RootElement.TryGetProperty("tool_choice", out _));
        Assert.Equal("json_schema", payload.RootElement
            .GetProperty("response_format")
            .GetProperty("type")
            .GetString());
        Assert.EndsWith(
            "/no_think",
            payload.RootElement
                .GetProperty("messages")[1]
                .GetProperty("content")
                .GetString());
    }

    [Fact]
    public async Task GetNextAsync_CanUseNativeToolsWhenExplicitlyConfigured()
    {
        var handler = new RecordingHandler();
        var client = new OpenAiCompatibleLlmClient(
            new HttpClient(handler),
            Options.Create(new AiAssistantOptions
            {
                Llm = new AiLlmClientOptions
                {
                    Provider = "ollama",
                    Endpoint = "http://llm.test/v1",
                    Model = "qwen3.5:9b",
                    OutputMode = "native-tools"
                }
            }));

        await client.GetNextAsync(new AiAgentSession(Guid.NewGuid(), "Покажи карточку 523"));

        using var payload = JsonDocument.Parse(handler.RequestBody!);
        Assert.True(payload.RootElement.TryGetProperty("tools", out _));
        Assert.Equal("auto", payload.RootElement.GetProperty("tool_choice").GetString());
        Assert.False(payload.RootElement.TryGetProperty("response_format", out _));
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"choices":[{"message":{"content":"Готово."}}]}""",
                    Encoding.UTF8,
                    "application/json")
            };
        }
    }
}
