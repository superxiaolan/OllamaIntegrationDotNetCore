using Microsoft.AspNetCore.Mvc;
using OllamaSharp;

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

        [HttpPost("api-stream")]
        public async Task GetStream([FromBody] string prompt) 
        {
            Response.ContentType = "text/plain";

            // 异步流式获取 Ollama 的响应
            await foreach (var status in _ollamaApiClient.GenerateAsync(prompt))
            {
                if(status != null)
                {
                    // 实时写入 HTTP 响应体
                    await Response.WriteAsync(status.Response);
                    await Response.Body.FlushAsync();
                }
            }
        }
    }
}
