using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProductWebManager.Data;
using ProductWebManager.Models;

namespace ProductWebManager.Services
{
    public class FridgeExpirationService
    {
        private readonly IDbContextFactory<ProductManagerContext> _contextFactory;

        public FridgeExpirationService(IDbContextFactory<ProductManagerContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<FridgeItem>> GetExpiringItemsAsync(int userId, int daysThreshold = 3)
        {
            var thresholdDate = DateTime.UtcNow.AddDays(daysThreshold);

            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.FridgeItems
                .Include(f => f.Product)
                .Where(f => f.UserId == userId 
                            && f.ExpirationDate != null 
                            && f.ExpirationDate <= thresholdDate)
                .OrderBy(f => f.ExpirationDate)
                .ToListAsync();
        }

        public async Task<List<FridgeItem>> GetExpiredItemsAsync(int userId)
        {
            var now = DateTime.UtcNow;

            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.FridgeItems
                .Include(f => f.Product)
                .Where(f => f.UserId == userId 
                            && f.ExpirationDate != null 
                            && f.ExpirationDate < now)
                .OrderBy(f => f.ExpirationDate)
                .ToListAsync();
        }
    }
}
