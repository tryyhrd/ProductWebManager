using ProductWebManager.Models;

namespace ProductWebManager.Models
{
    public class ShoppingListItem
    {
        public int Id { get; set; }
        public int ShoppingListId { get; set; }
        public int ProductId  { get; set; }
        public decimal Quantity { get; set; }
        public bool IsPurchased { get; set; }
        public Product Product { get; set; }
        public ShoppingList ShoppingList { get; set; }
    }
}
