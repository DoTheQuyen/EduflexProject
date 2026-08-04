namespace ShareService.Messaging
{
    public record FeedbackPublishedMessage(string FeedbackId, string Name, string CourseName);
}