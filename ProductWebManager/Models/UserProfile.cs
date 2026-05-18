using System.Reflection;

namespace ProductWebManager.Models
{
    public class UserProfile
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int Age { get; set; }

        public double Height { get; set; }

        public double Weight { get; set; }

        public Gender Gender { get; set; }

        public ActivityLevel ActivityLevel { get; set; }

        public GoalType Goal { get; set; }

        public double TargetCalories { get; set; }

        public User User { get; set; }


    }
    public enum Gender
    {
        Male,
        Female
    }
    public enum ActivityLevel
    {
        Low,
        Medium,
        High
    }
    public enum GoalType
    {
        Maintain,
        LoseWeight,
        GainWeight
    }
}
