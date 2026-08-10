using ShareService.Models.Chat;

namespace ShareService.Services.Interface.Integration
{
    public interface IChatService
    {
        Task<string> AskQuestionAsync(string question);
        Task<List<QuestionFrequency>> GetTopQuestionsAsync(int limit);
    }
}