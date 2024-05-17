using ProductManager.Models;

namespace ProductWebManager.Models
{
    public class MenuItem
    {
        public int Id { get; set; }
        public int MenuId { get; set; }
        public int RecipeId { get; set; }
        public DateTime Date { get; set; }
        public string MealType { get; set; }
        public Menu Menu { get; set; }
        public Recipe Recipe { get; set; }
    }
}
