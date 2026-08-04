using MongoDB.Driver;
using ShareService.Common;
using ShareService.DataAccess.Common;
using ShareService.DataAccess.Interface;
using ShareService.Models.Notification;

namespace ShareService.DataAccess
{
    public class Notification : AuditableCollectionBase<NotificationModel>, INotification
    {
        public Notification(IMongoDatabase database, ICurrentUserService currentUser)
            : base(database.GetCollection<NotificationModel>("Notifications"), currentUser)
        {
        }

        public async Task<bool> CreateNotificationAsync(NotificationModel notification)
        {
            await InsertOneAsync(notification);
            return true;
        }

        // "Active" for a given user = they're in the resolved recipient snapshot AND they
        // haven't cleared it yet.
        public async Task<List<NotificationModel>> GetActiveNotificationsForUserAsync(string userId)
        {
            var filter = Builders<NotificationModel>.Filter.And(
                Builders<NotificationModel>.Filter.AnyEq(n => n.RecipientUserIds, userId),
                Builders<NotificationModel>.Filter.Not(Builders<NotificationModel>.Filter.AnyEq(n => n.ClearedByUserIds, userId)));

            return await Collection
                .Find(filter)
                .SortByDescending(n => n.CreatedAt)
                .Limit(50)
                .ToListAsync();
        }

        public async Task<bool> ClearNotificationAsync(string id, string userId)
        {
            var update = Builders<NotificationModel>.Update.AddToSet(n => n.ClearedByUserIds, userId);
            var result = await UpdateOneAsync(n => n.Id == id, update);
            return result.ModifiedCount > 0;
        }
    }
}
