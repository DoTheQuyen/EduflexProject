using MongoDB.Driver;
using ShareService.Common;
using ShareService.DataAccess.Common;
using ShareService.DataAccess.Interface;
using ShareService.Models.Chat;

namespace ShareService.DataAccess
{
    public class ChatQuestion : AuditableCollectionBase<ChatQuestionModel>, IChatQuestion
    {
        public ChatQuestion(IMongoDatabase database, ICurrentUserService currentUser)
         : base(database.GetCollection<ChatQuestionModel>("ChatQuestions"), currentUser)
        {
        }

        public async Task<bool> CreateChatQuestionAsync(ChatQuestionModel question)
        {
            await InsertOneAsync(question);
            return true;
        }

        public async Task<List<QuestionFrequency>> GetTopQuestionsAsync(int limit)
        {
            return await Collection.Aggregate()
                .Group(q => q.NormalizedQuestion, g => new QuestionFrequency { Question = g.First().Question, Count = g.Count() })
                .SortByDescending(g => g.Count)
                .Limit(limit)
                .ToListAsync();
        }

        public async Task<ChatQuestionModel?> FindRecentAnswerAsync(string normalizedQuestion, DateTime notBefore)
        {
            return await Collection
                .Find(q => q.NormalizedQuestion == normalizedQuestion && q.CreatedAt >= notBefore)
                .SortByDescending(q => q.CreatedAt)
                .FirstOrDefaultAsync();
        }

    }
}