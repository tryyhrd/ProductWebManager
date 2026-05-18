namespace ProductWebManager.Models
{
    public class FridgeItem
    {
        public int Id { get; set; }
        public User User { get; set; }
        public Product Product { get; set; }
        public int UserId { get; set; }
        public int ProductId  { get; set; }
        public double? Quantity { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpirationDate {  get; set; }
    }
}
