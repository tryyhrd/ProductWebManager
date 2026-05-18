using ProductWebManager.Models;

namespace ProductWebManager.Models
{
    public class MealPlan
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; }
        public List<MealPlanItem> Items { get; set; } = new List<MealPlanItem>();
    }
}
