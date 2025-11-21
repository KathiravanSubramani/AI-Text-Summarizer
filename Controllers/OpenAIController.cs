using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using AITextSummarizer.Models;

namespace AITextSummarizer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OpenAIController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public OpenAIController(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpPost("summarize")]
        public async Task<IActionResult> Summarize([FromBody] TextRequest request)
        {
            if (string.IsNullOrEmpty(request.InputText))
                return BadRequest("Input text is required.");

            var apiKey = _config["OpenAI:ApiKey"];
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var body = new
            {
                model = "gpt-4o-mini",  // You can use gpt-4o or gpt-3.5-turbo
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant that summarizes text." },
                    new { role = "user", content = $"Summarize the following text:\n{request.InputText}" }
                }
            };

            var response = await _httpClient.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", body);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, responseContent);

            using var doc = JsonDocument.Parse(responseContent);
            var summary = doc.RootElement
                             .GetProperty("choices")[0]
                             .GetProperty("message")
                             .GetProperty("content")
                             .GetString();

            return Ok(new { summary });
        }
    }
}
