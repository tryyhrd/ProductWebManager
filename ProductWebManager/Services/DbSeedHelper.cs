using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProductWebManager.Data;
using ProductWebManager.Models;
using System.Collections.Generic;

namespace ProductWebManager.Services
{
    public static class DbSeedHelper
    {
        public static async Task SeedRecipesAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ProductManagerContext>();

            // 1. Delete recipes without images
            var badRecipes = await db.Recipes
                .Include(r => r.RecipeIngredients)
                .Where(r => string.IsNullOrEmpty(r.ImageUrl) || r.ImageUrl == "")
                .ToListAsync();

            if (badRecipes.Any())
            {
                // Remove ingredients first (though EF might cascade delete)
                foreach(var br in badRecipes)
                {
                    db.RecipeIngredients.RemoveRange(br.RecipeIngredients);
                }
                db.Recipes.RemoveRange(badRecipes);
                await db.SaveChangesAsync();
            }

            // 2. Check if recipes are already seeded
            if (await db.Recipes.CountAsync(r => r.ImageUrl != null && r.ImageUrl.Contains("unsplash")) >= 10)
                return;

            // Load products and units to link them correctly
            var products = await db.Products.ToDictionaryAsync(p => p.Name.ToLower());
            var units = await db.Units.ToDictionaryAsync(u => u.Name.ToLower());

            int GetProductId(string name) => products.TryGetValue(name.ToLower(), out var p) ? p.Id : products.First().Value.Id;
            int GetUnitId(string name) => units.TryGetValue(name.ToLower(), out var u) ? u.Id : units.First().Value.Id;

            var r1 = new Recipe { Title = "Куриная грудка с рисом", Description = "Классическое и сытное блюдо с высоким содержанием белка.", Instructions = "1. Рис промыть и сварить.\n2. Куриную грудку обжарить.", PrepTime = 10, CookTime = 25, ImageUrl = "https://images.unsplash.com/photo-1490645935967-10de6ba17061?w=800" };
            var r2 = new Recipe { Title = "Греческий салат", Description = "Лёгкий и свежий салат.", Instructions = "1. Нарезать овощи.\n2. Заправить маслом.", PrepTime = 15, CookTime = 0, ImageUrl = "https://images.unsplash.com/photo-1540189549336-e6e99c3679fe?w=800" };
            var r3 = new Recipe { Title = "Борщ классический", Description = "Наваристый борщ.", Instructions = "1. Сварить бульон.\n2. Добавить овощи.", PrepTime = 20, CookTime = 90, ImageUrl = "https://images.unsplash.com/photo-1548943487-a2e4e43b4853?w=800" };
            var r4 = new Recipe { Title = "Овсяная каша с бананом", Description = "Питательный завтрак.", Instructions = "1. Залить овсянку.\n2. Добавить банан.", PrepTime = 3, CookTime = 7, ImageUrl = "https://images.unsplash.com/photo-1517673400267-0251440c45dc?w=800" };
            var r5 = new Recipe { Title = "Омлет с овощами", Description = "Быстрый завтрак.", Instructions = "1. Взбить яйца.\n2. Обжарить с овощами.", PrepTime = 10, CookTime = 10, ImageUrl = "https://images.unsplash.com/photo-1510693206972-df098062cb71?w=800" };
            var r6 = new Recipe { Title = "Запечённый лосось", Description = "Лосось в духовке.", Instructions = "1. Замариновать.\n2. Запечь.", PrepTime = 10, CookTime = 25, ImageUrl = "https://images.unsplash.com/photo-1519708227418-c8fd9a32b7a2?w=800" };

            db.Recipes.AddRange(r1, r2, r3, r4, r5, r6);
            await db.SaveChangesAsync();

            // Link ingredients safely
            var uG = GetUnitId("г");
            var uShts = GetUnitId("шт.");
            var uMl = GetUnitId("мл");

            db.RecipeIngredients.AddRange(
                new RecipeIngredient { RecipeId = r1.Id, ProductId = GetProductId("Куриная грудка"), Quantity = 300, UnitId = uG },
                new RecipeIngredient { RecipeId = r1.Id, ProductId = GetProductId("Рис белый"), Quantity = 150, UnitId = uG },
                
                new RecipeIngredient { RecipeId = r2.Id, ProductId = GetProductId("Помидоры"), Quantity = 300, UnitId = uG },
                new RecipeIngredient { RecipeId = r2.Id, ProductId = GetProductId("Огурцы"), Quantity = 200, UnitId = uG },
                
                new RecipeIngredient { RecipeId = r3.Id, ProductId = GetProductId("Говядина (вырезка)"), Quantity = 400, UnitId = uG },
                new RecipeIngredient { RecipeId = r3.Id, ProductId = GetProductId("Свёкла"), Quantity = 300, UnitId = uG },
                
                new RecipeIngredient { RecipeId = r4.Id, ProductId = GetProductId("Овсяные хлопья"), Quantity = 100, UnitId = uG },
                new RecipeIngredient { RecipeId = r4.Id, ProductId = GetProductId("Банан"), Quantity = 1, UnitId = uShts },
                
                new RecipeIngredient { RecipeId = r5.Id, ProductId = GetProductId("Яйцо куриное"), Quantity = 3, UnitId = uShts },
                new RecipeIngredient { RecipeId = r5.Id, ProductId = GetProductId("Помидоры"), Quantity = 100, UnitId = uG },
                
                new RecipeIngredient { RecipeId = r6.Id, ProductId = GetProductId("Лосось"), Quantity = 300, UnitId = uG }
            );

            await db.SaveChangesAsync();
        }
    }
}
