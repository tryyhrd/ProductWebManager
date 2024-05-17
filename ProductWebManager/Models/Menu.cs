using ProductManager.Models;

namespace ProductWebManager.Models
{
    public class Menu
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; }
        public List<MenuItem> Items { get; set; } = new List<MenuItem>();
    }
}
