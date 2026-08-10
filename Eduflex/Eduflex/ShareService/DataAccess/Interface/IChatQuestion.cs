using ShareService.Models.Chat;

namespace ShareService.DataAccess.Interface
{
    public interface IChatQuestion
    {
        Task<bool> CreateChatQuestionAsync(ChatQuestionModel question);
        Task<List<QuestionFrequency>> GetTopQuestionsAsync(int limit);
        Task<ChatQuestionModel?> FindRecentAnswerAsync(string normalizedQuestion, DateTime notBefore);
    }
}