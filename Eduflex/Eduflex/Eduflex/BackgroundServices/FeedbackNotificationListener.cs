using System.Text.Json;
using ShareService.Messaging;
using StackExchange.Redis;

namespace Eduflex.API.BackgroundServices
{
    // Subscribes to the feedback:published Redis channel for the app's whole lifetime and
    // logs each notification. Runs independently of FeedbackService (the publisher) — this
    // is the point of pub/sub: publisher and subscriber don't know about each other.
    public class FeedbackNotificationListener : BackgroundService
    {
        private readonly IConnectionMultiplexer _multiplexer;
        private readonly ILogger<FeedbackNotificationListener> _logger;

        public FeedbackNotificationListener(IConnectionMultiplexer multiplexer, ILogger<FeedbackNotificationListener> logger)
        {
            _multiplexer = multiplexer;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //var subscriber = _multiplexer.GetSubscriber();
            //var channel = RedisChannel.Literal(RedisChannels.FeedbackPublished);

            //await subscriber.SubscribeAsync(channel, (_, message) => HandleMessage(message));
            //_logger.LogInformation("Subscribed to Redis channel {Channel}", RedisChannels.FeedbackPublished);

            //try
            //{
            //    // Keeps the hosted service alive; the subscription itself is driven by the
            //    // multiplexer's connection, not by this loop.
            //    await Task.Delay(Timeout.Infinite, stoppingToken);
            //}
            //catch (TaskCanceledException)
            //{
            //    // expected on app shutdown
            //}
            //finally
            //{
            //    await subscriber.UnsubscribeAsync(channel);
            //}
        }

        private void HandleMessage(RedisValue message)
        {
            try
            {
                var payload = JsonSerializer.Deserialize<FeedbackPublishedMessage>(message.ToString());
                _logger.LogInformation(
                    "New feedback received: {FeedbackId} from {Name} about {CourseName}",
                    payload?.FeedbackId, payload?.Name, payload?.CourseName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process feedback:published message");
            }
        }
    }
}