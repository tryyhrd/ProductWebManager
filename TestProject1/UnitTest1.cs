using Xunit;
using ProductWebManager.Models;

namespace TestProject1
{
    public class FridgeModuleTests
    {
        [Fact]
        public void Test_ExpiryDateCalculation()
        {
            var item = new FridgeItem { ExpirationDate = DateTime.Now, Quanity = 1 };
            var daysLeft = (item.ExpirationDate.Value - DateTime.Now).TotalDays;
            var statusText = daysLeft <= 0 ? "Просрочен" : $"Годен ещё {Math.Ceiling(daysLeft)} дн.";
            Assert.Equal("Просрочен", statusText);
        }

        [Fact]
        public void Test_StatsCalculation()
        {
            var products = new List<FridgeItem>
                {
                new FridgeItem { ExpirationDate = DateTime.Now },
                new FridgeItem { ExpirationDate = DateTime.Now.AddDays(1) },
                new FridgeItem { ExpirationDate = DateTime.Now.AddDays(5) }
                };
            var expiringToday = products.Count(p => p.ExpirationDate.HasValue 
            && p.ExpirationDate.Value.Date == DateTime.Now.Date);
            Assert.Equal(1, expiringToday);
        }
    }
}