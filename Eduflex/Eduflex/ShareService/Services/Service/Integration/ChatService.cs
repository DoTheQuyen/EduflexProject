using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShareService.DataAccess.Interface;
using ShareService.Models.Chat;
using ShareService.Models.Setting;
using ShareService.Services.Interface;
using ShareService.Services.Interface.Integration;

namespace ShareService.Services.Service.Integration
{
    public class ChatService : IChatService
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiSettings _settings;
        private readonly IChatQuestion _chatQuestionDataAccess;
        private readonly ISettingsService _settingsService;
        private readonly ILogger<ChatService> _logger;

        public ChatService(HttpClient httpClient, IOptions<GeminiSettings> settings, IChatQuestion chatQuestionDataAccess, ISettingsService settingsService, ILogger<ChatService> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _chatQuestionDataAccess = chatQuestionDataAccess;
            _settingsService = settingsService;
            _logger = logger;
        }

        private const int CacheDays = 3;

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

            var answer = await AskGeminiAsync(question);
            if (answer == null)
            {
                // Rate-limited by Gemini — tell the student, but don't log/cache this as a real
                // answer, or the placeholder would keep getting served for days after the limit clears.
                return "We're getting a lot of questions right now - please wait a moment and try again.";
            }

            await _chatQuestionDataAccess.CreateChatQuestionAsync(new ChatQuestionModel
            {
                Question = question,
                NormalizedQuestion = normalizedQuestion,
                Answer = answer,
                CreatedAt = DateTime.UtcNow
            });

            return answer;
        }

        private async Task<string?> AskGeminiAsync(string question)
        {
            var appSettings = await _settingsService.GetSettingsAsync();

            var requestBody = new
            {
                systemInstruction = new { parts = new[] { new { text = appSettings.ChatSystemPrompt } } },
                contents = new[] { new { parts = new[] { new { text = question } } } }
            };

            var url = $"{appSettings.ChatApiUrl.Replace("{model}", _settings.Model)}?key={_settings.ApiKey}";

            try
            {
                using var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                using var response = await _httpClient.PostAsync(url, content);

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning("Gemini API rate limit hit for question: {Question}", question);
                    return null;
                }

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
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
                _logger.LogError(ex, "Error asking Gemini for question: {Question}", question);
                throw new Exception("Error getting an answer. Please try again.", ex);
            }
        }

        public Task<List<QuestionFrequency>> GetTopQuestionsAsync(int limit)
        {
            return _chatQuestionDataAccess.GetTopQuestionsAsync(limit);
        }
    }
}