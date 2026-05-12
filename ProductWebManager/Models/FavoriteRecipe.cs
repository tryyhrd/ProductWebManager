namespace ProductWebManager.Models
{
    public class FavoriteRecipe
    {
        public int Id { get; set; }
        public int RecipeId { get; set; }
        public int UserId { get; set; }

        public Recipe Recipe { get; set; }
        public User User { get; set; }
    }
}
