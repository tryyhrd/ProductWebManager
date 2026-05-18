namespace ProductWebManager.Models
{
    public class Recipe
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Instructions { get; set; }
        public string? ImageUrl { get; set; } = "";
        public int? PrepTime { get; set; }
        public int? CookTime { get; set; }
        public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
    }
}
