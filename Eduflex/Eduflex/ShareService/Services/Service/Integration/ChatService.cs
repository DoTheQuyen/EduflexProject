using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShareService.DataAccess.Interface;
using ShareService.Enums.Chat;
using ShareService.Models.Chat;
using ShareService.Models.Setting;
using ShareService.Models.Settings;
using ShareService.Services.Interface;
using ShareService.Services.Interface.Integration;

namespace ShareService.Services.Service.Integration
{
    public class ChatService : IChatService
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiSettings _settings;
        private readonly GroqSettings _groqSettings;
        private readonly OpenRouterSettings _openRouterSettings;
        private readonly IChatQuestion _chatQuestionDataAccess;
        private readonly ISettingsService _settingsService;
        private readonly ILogger<ChatService> _logger;

        public ChatService(HttpClient httpClient, IOptions<GeminiSettings> settings, IOptions<GroqSettings> groqSettings, IOptions<OpenRouterSettings> openRouterSettings, IChatQuestion chatQuestionDataAccess, ISettingsService settingsService, ILogger<ChatService> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _groqSettings = groqSettings.Value;
            _openRouterSettings = openRouterSettings.Value;
            _chatQuestionDataAccess = chatQuestionDataAccess;
            _settingsService = settingsService;
            _logger = logger;
        }

        private const int CacheDays = 3;
        private const int ProviderTimeoutSeconds = 8;

        public async Task<string> AskQuestionAsync(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                throw new ArgumentException("Question cannot be empty");
            }

            if (question.Length > 500)
            {
                throw new ArgumentException("Question is too long. Please limit to 500 characters.");
            }

            var normalizedQuestion = question.Trim().ToLowerInvariant();
            var recent = await _chatQuestionDataAccess.FindRecentAnswerAsync(normalizedQuestion, DateTime.UtcNow.AddDays(-CacheDays));

            if (recent != null)
            {
                _logger.LogInformation("Reusing cached answer for question: {Question}", question);
                return recent.Answer;
            }

            var appSettings = await _settingsService.GetSettingsAsync();

            var provider = ChatProvider.Gemini;
            var answer = await AskGeminiAsync(question, appSettings);
            if (answer == null)
            {
                _logger.LogInformation("Gemini unavailable, falling back to Groq for question: {Question}", question);
                provider = ChatProvider.Groq;
                answer = await AskGroqAsync(question, appSettings);
            }

            if (answer == null)
            {
                _logger.LogInformation("Groq unavailable, falling back to OpenRouter for question: {Question}", question);
                provider = ChatProvider.OpenRouter;
                answer = await AskOpenRouterAsync(question, appSettings);
            }

            if (answer == null)
            {
                // All providers exhausted — tell the student, but don't log/cache this as a real
                // answer, or the placeholder would keep getting served for days after the limit clears.
                return "We're getting a lot of questions right now - please wait a moment and try again.";
            }

            await _chatQuestionDataAccess.CreateChatQuestionAsync(new ChatQuestionModel
            {
                Question = question,
                NormalizedQuestion = normalizedQuestion,
                Answer = answer,
                CreatedAt = DateTime.UtcNow,
                Language = DetectLanguage(question),
                Provider = provider.ToString(),
                ReviewStatus = ChatReviewStatus.Unreviewed.ToString()
            });

            return answer;
        }

        // Heuristic, not a real language detector: Vietnamese-specific diacritics are enough
        // to distinguish "en" vs "vi" for this chat's actual traffic without pulling in an
        // NLP dependency for two supported languages.
        private static string DetectLanguage(string text)
        {
            const string vietnameseChars =
                "àáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđ" +
                "ÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴÈÉẸẺẼÊỀẾỆỂỄÌÍỊỈĨÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠÙÚỤỦŨƯỪỨỰỬỮỲÝỴỶỸĐ";

            return text.Any(vietnameseChars.Contains) ? "vi" : "en";
        }

        private async Task<string?> AskGeminiAsync(string question, SettingsModel appSettings)
        {
            var requestBody = new
            {
                systemInstruction = new { parts = new[] { new { text = appSettings.ChatSystemPrompt } } },
                contents = new[] { new { parts = new[] { new { text = question } } } }
            };

            var url = $"{appSettings.ChatApiUrl.Replace("{model}", _settings.Model)}?key={_settings.ApiKey}";

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(ProviderTimeoutSeconds));
                using var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                using var response = await _httpClient.PostAsync(url, content, cts.Token);

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning("Gemini API rate limit hit for question: {Question}", question);
                    return null;
                }

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cts.Token);
                using var document = JsonDocument.Parse(json);

                return document.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error asking Gemini for question: {Question} — falling back to next provider", question);
                return null;
            }
        }

        private async Task<string?> AskGroqAsync(string question, SettingsModel appSettings)
        {
            var requestBody = new
            {
                model = _groqSettings.Model,
                messages = new object[]
                {
                    new { role = "system", content = appSettings.ChatSystemPrompt },
                    new { role = "user", content = question }
                }
            };

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(ProviderTimeoutSeconds));
                using var request = new HttpRequestMessage(HttpMethod.Post, appSettings.ChatGroqApiUrl)
                {
                    Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _groqSettings.GroqApiKey);

                using var response = await _httpClient.SendAsync(request, cts.Token);

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning("Groq API rate limit hit for question: {Question}", question);
                    return null;
                }

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cts.Token);
                using var document = JsonDocument.Parse(json);

                return document.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error asking Groq for question: {Question}", question);
                return null;
            }
        }

        private async Task<string?> AskOpenRouterAsync(string question, SettingsModel appSettings)
        {
            var requestBody = new
            {
                model = _openRouterSettings.Model,
                messages = new object[]
                {
                    new { role = "system", content = appSettings.ChatSystemPrompt },
                    new { role = "user", content = question }
                }
            };

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(ProviderTimeoutSeconds));
                using var request = new HttpRequestMessage(HttpMethod.Post, appSettings.ChatOpenRouterApiUrl)
                {
                    Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _openRouterSettings.OpenRouterApiKey);

                using var response = await _httpClient.SendAsync(request, cts.Token);

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning("OpenRouter API rate limit hit for question: {Question}", question);
                    return null;
                }

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cts.Token);
                using var document = JsonDocument.Parse(json);

                return document.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error asking OpenRouter for question: {Question}", question);
                return null;
            }
        }

        public Task<List<QuestionFrequency>> GetTopQuestionsAsync(int limit)
        {
            return _chatQuestionDataAccess.GetTopQuestionsAsync(limit);
        }
    }
}