using System.Text.Json;
using Eduflex.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using ShareService.Messaging;
using StackExchange.Redis;

namespace Eduflex.API.BackgroundServices
{
    // Subscribes to the shared eduflex:notifications channel for the app's whole lifetime.
    // One listener handles every event type — adding a new notification source later means
    // publishing a NotificationMessage from that service, not writing a new listener.
    public class NotificationListener : BackgroundService
    {
        private readonly IConnectionMultiplexer _multiplexer;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationListener> _logger;

        public NotificationListener(
            IConnectionMultiplexer multiplexer,
            IHubContext<NotificationHub> hubContext,
            ILogger<NotificationListener> logger)
        {
            _multiplexer = multiplexer;
            _hubContext = hubContext;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var subscriber = _multiplexer.GetSubscriber();
            var channel = RedisChannel.Literal(RedisChannels.Notifications);

            await subscriber.SubscribeAsync(channel, (redisChannel, message) =>
            {
                _ = HandleMessageAsync(message);
            });
            _logger.LogInformation("Subscribed to Redis channel {Channel}", RedisChannels.Notifications);

            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // expected on app shutdown
            }
            finally
            {
                await subscriber.UnsubscribeAsync(channel);
            }
        }

        private async Task HandleMessageAsync(RedisValue message)
        {
            try
            {
                var payload = JsonSerializer.Deserialize<NotificationMessage>(message.ToString());
                if (payload == null)
                {
                    return;
                }

                var groups = payload.RecipientUserIds.Select(NotificationHub.UserGroupName).ToList();
                if (groups.Count > 0)
                {
                    await _hubContext.Clients.Groups(groups).SendAsync("ReceiveNotification", payload);
                }

                _logger.LogInformation(
                    "Notified {Count} recipient(s) ({TargetType}): {Module} {EntityId} — {Summary}",
                    groups.Count, payload.TargetType, payload.Module, payload.EntityId, payload.Summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process notification message");
            }
        }
    }
}
