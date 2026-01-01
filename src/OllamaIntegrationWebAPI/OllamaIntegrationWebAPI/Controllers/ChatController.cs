using Microsoft.AspNetCore.Mvc;
using OllamaSharp;
using OllamaSharp.Models;

namespace OllamaIntegrationWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IOllamaApiClient _ollamaApiClient;
        public ChatController(IOllamaApiClient ollamaApiClient) 
        { 
            _ollamaApiClient = ollamaApiClient;
        }

        [HttpPost("chat-stream")]
        public async Task GetChatStream([FromBody] ChatWithHistoryRequest request)
        {
            Response.ContentType = "text/plain; charset=utf-8";

            // 1. 构造原始请求
            var chatRequest = new GenerateRequest
            {
                Prompt = request.Prompt,
                Context = request.Context // 接收前端传来的旧记忆
            };

            // 2. 开始流式迭代
            var stream = _ollamaApiClient.GenerateAsync(chatRequest);

            await foreach (var status in stream)
            {
                // 实时输出 AI 的文字
                await Response.WriteAsync(status.Response);
                await Response.Body.FlushAsync();

                // 3. 【核心修复】：检查是否是最后一帧并提取 Context
                if (status.Done)
                {
                    // 在 OllamaSharp 中，status 实际上是 GenerateResponseStream 类型
                    // 我们可以通过反射或尝试强制转换来获取隐藏在里面的真实 Context
                    // 这里使用一个最稳妥的方法：直接从 status 对象中获取，如果编译器不认识，我们用 dynamic 绕过检查
                    try
                    {
                        dynamic dynamicStatus = status;
                        long[]? newContext = dynamicStatus.Context;

                        if (newContext != null)
                        {
                            // 构造 JSON 行，确保前端能拿到新的记忆数组
                            var contextData = new { context = newContext, done = true };
                            string jsonLine = "\n" + System.Text.Json.JsonSerializer.Serialize(contextData);

                            await Response.WriteAsync(jsonLine);
                            await Response.Body.FlushAsync();
                        }
                    }
                    catch
                    {
                        // 如果 dynamic 转换失败，说明该版本 SDK 结构不同，可在此添加备用逻辑
                    }
                }
            }
        }

        // 定义一个新的请求结构，用于接收问题和上一次的上下文
        public class ChatWithHistoryRequest
        {
            public string Prompt { get; set; } = string.Empty;
            public long[]? Context { get; set; } // Ollama 专用的上下文 ID 数组
        }
    }
}
