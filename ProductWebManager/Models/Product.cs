namespace ProductManager.Models
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
    }
}
