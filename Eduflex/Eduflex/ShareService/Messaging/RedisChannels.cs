namespace ShareService.Messaging
{
    // Central place for Redis pub/sub channel names, so publisher and subscriber
    // can't drift on the string.
    public static class RedisChannels
    {
        //public const string FeedbackPublished = "eduflex:feedback:published";

        public const string Notifications = "eduflex:notifications";
    }
}