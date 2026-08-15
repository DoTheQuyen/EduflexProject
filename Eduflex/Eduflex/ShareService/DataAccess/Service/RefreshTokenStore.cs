using MongoDB.Driver;
using ShareService.DataAccess.Interface;
using ShareService.Models.Auth;

namespace ShareService.DataAccess
{
    public class RefreshTokenStore : IRefreshTokenStore
    {
        private readonly IMongoCollection<RefreshTokenModel> _tokensCollection;

        public RefreshTokenStore(IMongoDatabase database)
        {
            _tokensCollection = database.GetCollection<RefreshTokenModel>("RefreshTokens");
        }

        public async Task CreateAsync(RefreshTokenModel token)
        {
            await _tokensCollection.InsertOneAsync(token);
        }

        public async Task<RefreshTokenModel?> FindByHashAsync(string tokenHash)
        {
            return await _tokensCollection
                .Find(t => t.TokenHash == tokenHash)
                .FirstOrDefaultAsync();
        }

        public async Task RevokeAsync(string id)
        {
            var update = Builders<RefreshTokenModel>.Update
                .Set(t => t.RevokedAt, DateTime.UtcNow);

            await _tokensCollection.UpdateOneAsync(t => t.Id == id, update);
        }

        public async Task RevokeAllForUserAsync(string userId)
        {
            var update = Builders<RefreshTokenModel>.Update
                .Set(t => t.RevokedAt, DateTime.UtcNow);

            await _tokensCollection.UpdateManyAsync(
                t => t.UserId == userId && t.RevokedAt == null,
                update);
        }
    }
}