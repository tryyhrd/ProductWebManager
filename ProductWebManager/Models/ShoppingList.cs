using ProductWebManager.Models;

namespace ProductWebManager.Models
{
    public class ShoppingList
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; }
        public ICollection<ShoppingListItem> Items { get; set; } = new List<ShoppingListItem>();
    }
}
