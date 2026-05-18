namespace ProductWebManager.Models
{
    public class RecipeIngredient
    {
        public int Id { get; set; }
        public int RecipeId { get; set; }
        public int ProductId  { get; set; }
        public double Quantity { get; set; }
        public int? UnitId { get; set; }
        public Unit? Unit { get; set; }
        public Recipe Recipe { get; set; }
        public Product Product { get; set; }
    }
}
