namespace ProductWebManager.Models
{
    public class Allergy
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ICollection<ProductAllergy> ProductAllergies { get; set; }
            = new List<ProductAllergy>();
    }
}
