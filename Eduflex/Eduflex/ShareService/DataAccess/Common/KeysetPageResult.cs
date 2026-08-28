namespace ShareService.Common
{
    public class KeysetPageResult<T>
    {
        public List<T> Items { get; set; } = new();
        public DateTime? NextCursorCreatedAt { get; set; }
        public string? NextCursorId { get; set; }
        public bool HasMore { get; set; }
    }
}