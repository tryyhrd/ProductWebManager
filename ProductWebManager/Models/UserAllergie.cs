namespace ProductWebManager.Models
{
    public class UserAllergie
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int Allergieid { get; set; }
        public User User { get; set; }
        public Allergie Allergie { get; set; }
    }
}
