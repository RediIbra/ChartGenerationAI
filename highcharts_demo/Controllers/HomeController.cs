using highcharts_demo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using highcharts_demo.Models;

namespace highcharts_demo.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<HomeController> _logger;
        private readonly string _openAiApiKey;
        public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory, OpenAISettings settings)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _openAiApiKey = settings.ApiKey;
        }

        public IActionResult Index() => View();
        public IActionResult Privacy() => View();
        public IActionResult LineChart() => View();
        public IActionResult BarChart() => View();
        public IActionResult PieChart() => View();
        public IActionResult ArenaChart() => View();
        public IActionResult BubbleChart() => View();

        [HttpPost]
        public async Task<IActionResult> GenerateChartAjax([FromBody] ChartPrompt request)
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
                return BadRequest("Prompt is required");

            try
            {
                var chartJson = await GenerateHighchartsConfigAsync(request.Prompt);
                return Json(new { chartJs = chartJson });
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "OpenAI API request failed");
                return StatusCode(503, "OpenAI API request failed");
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Failed to parse OpenAI response");
                return StatusCode(500, "Error parsing OpenAI response");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected server error");
                return StatusCode(500, $"Unexpected error: {ex.Message}");
            }
        }

        private async Task<JsonElement> GenerateHighchartsConfigAsync(string prompt)
        {
            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new { role = "system", content = "You are a Highcharts expert. Return ONLY the JSON object (no extra text) for Highcharts.chart config based on user's prompt." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.7,
                max_tokens = 600
            };

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", OpenAiApiKey);

            var response = await client.PostAsync(
                "https://api.openai.com/v1/chat/completions",
                new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));

            var rawResponse = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("OpenAI Raw Response: {0}", rawResponse);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("OpenAI Error: {0}", rawResponse);
                throw new HttpRequestException($"OpenAI API error: {response.StatusCode}");
            }

            using var jsonDoc = JsonDocument.Parse(rawResponse);
            var content = jsonDoc.RootElement
                                 .GetProperty("choices")[0]
                                 .GetProperty("message")
                                 .GetProperty("content")
                                 .GetString();

            content = CleanOpenAiJson(content);

            try
            {
                return JsonSerializer.Deserialize<JsonElement>(content);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Invalid JSON content received: {0}", content);
                throw;
            }
        }

        private string CleanOpenAiJson(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return "{}";

            content = content.Trim();

            if (content.StartsWith("```"))
            {
                int firstLineEnd = content.IndexOf('\n');
                int lastFence = content.LastIndexOf("```");

                if (firstLineEnd != -1 && lastFence != -1 && lastFence > firstLineEnd)
                {
                    return content.Substring(firstLineEnd + 1, lastFence - firstLineEnd - 1).Trim();
                }
            }

            return content;
        }

        [HttpPost]
        public async Task<IActionResult> UpdateChartAjax([FromBody] UpdateChartRequest request)
        {
            if (request == null)
            {
                _logger.LogError("UpdateChartAjax: Request body is null");
                return BadRequest("Request body is missing or invalid");
            }
            if (string.IsNullOrWhiteSpace(request.Instruction))
                return BadRequest("Instruction is required");
            if (request.CurrentConfig.ValueKind == JsonValueKind.Undefined)
                return BadRequest("CurrentConfig is required");

            if (string.IsNullOrWhiteSpace(request.Instruction))
                return BadRequest("Instruction is required");

            try
            {
                var updatedChartJson = await UpdateHighchartsConfigAsync(request.CurrentConfig, request.Instruction);
                return Json(new { chartJs = updatedChartJson });
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "OpenAI API request failed");
                return StatusCode(503, "OpenAI API request failed");
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Failed to parse OpenAI response");
                return StatusCode(500, "Error parsing OpenAI response");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected server error");
                return StatusCode(500, $"Unexpected error: {ex.Message}");
            }
        }

        private async Task<JsonElement> UpdateHighchartsConfigAsync(JsonElement currentConfig, string instruction)
        {
            string currentConfigJson = currentConfig.GetRawText();

            var systemMessage = @$"
You are a Highcharts expert.
Here is the current Highcharts config JSON:
{currentConfigJson}
Modify this JSON according to the user's instruction and return ONLY the updated JSON object, no extra text.
";

            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
            new { role = "system", content = systemMessage },
            new { role = "user", content = instruction }
        },
                temperature = 0.7,
                max_tokens = 600
            };

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", OpenAiApiKey);

            var response = await client.PostAsync(
                "https://api.openai.com/v1/chat/completions",
                new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));

            var rawResponse = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("OpenAI Raw Response (Update): {0}", rawResponse);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("OpenAI Error (Update): {0}", rawResponse);
                throw new HttpRequestException($"OpenAI API error: {response.StatusCode}");
            }

            using var jsonDoc = JsonDocument.Parse(rawResponse);
            var content = jsonDoc.RootElement
                                 .GetProperty("choices")[0]
                                 .GetProperty("message")
                                 .GetProperty("content")
                                 .GetString();

            content = CleanOpenAiJson(content);

            try
            {
                return JsonSerializer.Deserialize<JsonElement>(content);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Invalid JSON content received (Update): {0}", content);
                throw;
            }
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }



    public class ChartPrompt
    {
        public string Prompt { get; set; }
    }
}
