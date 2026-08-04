namespace ShareService.Messaging
{
    // One shape for every notification type — the receiving side (SignalR clients) always
    // gets the same fields regardless of which module/event produced it. RecipientUserIds
    // is how NotificationListener knows which per-user SignalR groups to fan out to.
    public record NotificationMessage(
        string Id,
        string Module,
        string EntityId,
        string Summary,
        string TargetType,
        string? TargetDepartmentId,
        List<string> RecipientUserIds);
}
