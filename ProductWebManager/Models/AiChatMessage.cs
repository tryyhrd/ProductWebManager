namespace ProductWebManager.Models
{
    public class AiChatMessage
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool IsUser { get; set; }
        public string? ImagePreviewUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
