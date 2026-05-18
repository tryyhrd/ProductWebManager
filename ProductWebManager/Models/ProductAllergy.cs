namespace ProductWebManager.Models
{
    public class ProductAllergy
    {
        public int Id { get; set; }
        public int ProductId { get; set; }

        public int AllergyId { get; set; }

        public Product Product { get; set; }

        public Allergy Allergy { get; set; }
    }
}
