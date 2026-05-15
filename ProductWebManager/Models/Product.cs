namespace ProductWebManager.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Unit Unit { get; set; }
        public Category Category { get; set; }
        public int UnitId { get; set; }
        public int CategoryId { get; set; }
        public decimal? Price { get; set; }
        public double Proteins { get; set; }
        public double Fats { get; set; }
        public double Carbohydrates { get; set; }
        public double Calories { get; set; }
    }
}
