using BlazorServerApp_Server.Data.Model;
using BlazorServerApp_Server.Services;
using Microsoft.EntityFrameworkCore;

namespace BlazorServerApp_Server.Data
{
    public class DataSeeder
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
        private readonly ILogger<DataSeeder> _logger;

        public DataSeeder(IDbContextFactory<ApplicationDbContext> dbContextFactory, ILogger<DataSeeder> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        /// <summary>
        /// Seeds initial AdvancedNavigationLogEntries data into the database if the table is empty.
        /// This provides a baseline dataset for the ML model to train on.
        /// </summary>
        public async Task SeedInitialTrainingDataAsync()
        {
            using (var context = _dbContextFactory.CreateDbContext())
            {
                // Check if the table already contains any data
                if (await context.AdvancedNavigationLogEntries.AnyAsync())
                {
                    _logger.LogInformation("Database already contains AdvancedNavigationLogEntries. Skipping seeding.");
                    return;
                }

                _logger.LogInformation("Seeding initial AdvancedNavigationLogEntries data...");

                // Define your initial, cleaned-up, and patterned data
                var initialData = new List<AdvancedNavigationLogEntry>
                {
                    // User1 (Desktop) - Pattern: / -> /weather -> /heavy-report -> /weather -> /
                    // This creates a loop: Home -> Weather -> Heavy Report -> Weather -> Home -> Weather ...
                    new AdvancedNavigationLogEntry { PreviousPage1Url = "/", PreviousPage2Url = "", PreviousPage3Url = "", TimeOfDayInHours = 9.0f, UserId = "user1", DeviceType = "Desktop", NextPageUrl = "/weather" },
                    new AdvancedNavigationLogEntry { PreviousPage1Url = "/weather", PreviousPage2Url = "/", PreviousPage3Url = "", TimeOfDayInHours = 9.1f, UserId = "user1", DeviceType = "Desktop", NextPageUrl = "/heavy-report" },
                    new AdvancedNavigationLogEntry { PreviousPage1Url = "/heavy-report", PreviousPage2Url = "/weather", PreviousPage3Url = "/", TimeOfDayInHours = 9.2f, UserId = "user1", DeviceType = "Desktop", NextPageUrl = "/weather" },
                    new AdvancedNavigationLogEntry { PreviousPage1Url = "/weather", PreviousPage2Url = "/heavy-report", PreviousPage3Url = "/weather", TimeOfDayInHours = 9.3f, UserId = "user1", DeviceType = "Desktop", NextPageUrl = "/" },
                    new AdvancedNavigationLogEntry { PreviousPage1Url = "/", PreviousPage2Url = "/weather", PreviousPage3Url = "/heavy-report", TimeOfDayInHours = 9.4f, UserId = "user1", DeviceType = "Desktop", NextPageUrl = "/weather" },
                    new AdvancedNavigationLogEntry { PreviousPage1Url = "/weather", PreviousPage2Url = "/", PreviousPage3Url = "/weather", TimeOfDayInHours = 9.5f, UserId = "user1", DeviceType = "Desktop", NextPageUrl = "/heavy-report" },

                    // User2 (Mobile) - Pattern: / -> /heavy-report -> / -> /heavy-report
                    // This user frequently alternates between Home and Heavy Report
                    new AdvancedNavigationLogEntry { PreviousPage1Url = "/", PreviousPage2Url = "", PreviousPage3Url = "", TimeOfDayInHours = 14.0f, UserId = "user2", DeviceType = "Mobile", NextPageUrl = "/heavy-report" },
                    new AdvancedNavigationLogEntry { PreviousPage1Url = "/heavy-report", PreviousPage2Url = "/", PreviousPage3Url = "", TimeOfDayInHours = 14.1f, UserId = "user2", DeviceType = "Mobile", NextPageUrl = "/" },
                    new AdvancedNavigationLogEntry { PreviousPage1Url = "/", PreviousPage2Url = "/heavy-report", PreviousPage3Url = "/", TimeOfDayInHours = 14.2f, UserId = "user2", DeviceType = "Mobile", NextPageUrl = "/heavy-report" },
                    new AdvancedNavigationLogEntry { PreviousPage1Url = "/heavy-report", PreviousPage2Url = "/", PreviousPage3Url = "/heavy-report", TimeOfDayInHours = 14.3f, UserId = "user2", DeviceType = "Mobile", NextPageUrl = "/" },
                    new AdvancedNavigationLogEntry { PreviousPage1Url = "/", PreviousPage2Url = "/heavy-report", PreviousPage3Url = "/", TimeOfDayInHours = 14.4f, UserId = "user2", DeviceType = "Mobile", NextPageUrl = "/heavy-report" },

                    // User3 (Desktop) - Pattern: /weather -> / -> /weather (different time, focusing on weather)
                    new AdvancedNavigationLogEntry { PreviousPage1Url = "/weather", PreviousPage2Url = "", PreviousPage3Url = "", TimeOfDayInHours = 20.0f, UserId = "user3", DeviceType = "Desktop", NextPageUrl = "/" },
                    new AdvancedNavigationLogEntry { PreviousPage1Url = "/", PreviousPage2Url = "/weather", PreviousPage3Url = "", TimeOfDayInHours = 20.1f, UserId = "user3", DeviceType = "Desktop", NextPageUrl = "/weather" },
                    new AdvancedNavigationLogEntry { PreviousPage1Url = "/weather", PreviousPage2Url = "/", PreviousPage3Url = "/weather", TimeOfDayInHours = 20.2f, UserId = "user3", DeviceType = "Desktop", NextPageUrl = "/" },
                    new AdvancedNavigationLogEntry { PreviousPage1Url = "/", PreviousPage2Url = "/weather", PreviousPage3Url = "/", TimeOfDayInHours = 20.3f, UserId = "user3", DeviceType = "Desktop", NextPageUrl = "/weather" },

                    // Mixed pattern for more variety and to test different transitions
                    new AdvancedNavigationLogEntry { PreviousPage1Url = "/weather", PreviousPage2Url = "/heavy-report", PreviousPage3Url = "/", TimeOfDayInHours = 11.0f, UserId = "user4", DeviceType = "Tablet", NextPageUrl = "/" },
                    new AdvancedNavigationLogEntry { PreviousPage1Url = "/", PreviousPage2Url = "/weather", PreviousPage3Url = "/heavy-report", TimeOfDayInHours = 11.1f, UserId = "user4", DeviceType = "Tablet", NextPageUrl = "/heavy-report" },
                    new AdvancedNavigationLogEntry { PreviousPage1Url = "/heavy-report", PreviousPage2Url = "/", PreviousPage3Url = "/weather", TimeOfDayInHours = 11.2f, UserId = "user4", DeviceType = "Tablet", NextPageUrl = "/weather" },
                };

                await context.AdvancedNavigationLogEntries.AddRangeAsync(initialData);
                await context.SaveChangesAsync();
                _logger.LogInformation($"Seeded {initialData.Count} AdvancedNavigationLogEntries.");


                if (await context.Products.AnyAsync())
                {
                    _logger.LogInformation("Database already contains products. Skipping seeding.");
                    return;
                }

                _logger.LogInformation("Seeding database with initial data...");

                // Create main categories
                var womenCategory = new Category { Name = "Women" };
                var menCategory = new Category { Name = "Men" };
                var shoesCategory = new Category { Name = "Shoes" };
                var bagsCategory = new Category { Name = "Bags" };
                var glassesCategory = new Category { Name = "Glasses" };

                await context.Categories.AddRangeAsync(womenCategory, menCategory, shoesCategory, bagsCategory, glassesCategory);
                await context.SaveChangesAsync();

                // Create sub-categories with parent relationships
                var womenShoesCategory = new Category { Name = "Shoes", ParentCategory = womenCategory };
                var womenBagsCategory = new Category { Name = "Bags", ParentCategory = womenCategory };
                var womenGlassesCategory = new Category { Name = "Glasses", ParentCategory = womenCategory };

                var menShoesCategory = new Category { Name = "Shoes", ParentCategory = menCategory };
                var menBagsCategory = new Category { Name = "Bags", ParentCategory = menCategory };
                var menGlassesCategory = new Category { Name = "Glasses", ParentCategory = menCategory };

                await context.Categories.AddRangeAsync(
                    womenShoesCategory, womenBagsCategory, womenGlassesCategory,
                    menShoesCategory, menBagsCategory, menGlassesCategory
                );
                await context.SaveChangesAsync();

                // Seed Products
                var products = new List<Product>
            {
                // Women's Shoes
                new Product { Name = "Elegance Stiletto", Brand = "Massimo Dutti", Price = 129.99m, ImageUrl = "[https://picsum.photos/id/119/1200/1800](https://picsum.photos/id/119/1200/1800)", Description = "A classic stiletto for an elegant look.", Category = womenShoesCategory },
                new Product { Name = "Leather Loafer", Brand = "Zara", Price = 89.50m, ImageUrl = "[https://picsum.photos/id/124/1200/1800](https://picsum.photos/id/124/1200/1800)", Description = "Comfortable and stylish leather loafers.", Category = womenShoesCategory },
                new Product { Name = "White Sneakers", Brand = "Zara", Price = 75.00m, ImageUrl = "[https://picsum.photos/id/111/1200/1800](https://picsum.photos/id/111/1200/1800)", Description = "Versatile sneakers for everyday style.", Category = womenShoesCategory },
                new Product { Name = "Block Heel Sandals", Brand = "Massimo Dutti", Price = 99.00m, ImageUrl = "[https://picsum.photos/id/1053/1200/1800](https://picsum.photos/id/1053/1200/1800)", Description = "Stylish sandals with a comfortable block heel.", Category = womenShoesCategory },
                
                // Women's Bags
                new Product { Name = "Crossbody Leather Bag", Brand = "Massimo Dutti", Price = 159.99m, ImageUrl = "[https://picsum.photos/id/1083/1200/1800](https://picsum.photos/id/1083/1200/1800)", Description = "Minimalist leather bag for all your essentials.", Category = womenBagsCategory },
                new Product { Name = "Tote Bag", Brand = "Zara", Price = 69.99m, ImageUrl = "[https://picsum.photos/id/106/1200/1800](https://picsum.photos/id/106/1200/1800)", Description = "A spacious and practical tote bag.", Category = womenBagsCategory },
                new Product { Name = "Clutch Bag", Brand = "Massimo Dutti", Price = 119.99m, ImageUrl = "[https://picsum.photos/id/125/1200/1800](https://picsum.photos/id/125/1200/1800)", Description = "An elegant clutch for evening occasions.", Category = womenBagsCategory },
                
                // Women's Glasses
                new Product { Name = "Cat-Eye Sunglasses", Brand = "Zara", Price = 39.99m, ImageUrl = "[https://picsum.photos/id/126/1200/1800](https://picsum.photos/id/126/1200/1800)", Description = "Trendy cat-eye sunglasses with UV protection.", Category = womenGlassesCategory },
                new Product { Name = "Aviator Sunglasses", Brand = "Massimo Dutti", Price = 59.99m, ImageUrl = "[https://picsum.photos/id/127/1200/1800](https://picsum.photos/id/127/1200/1800)", Description = "Classic aviator style with a modern twist.", Category = womenGlassesCategory },
                
                // Men's Shoes
                new Product { Name = "Classic Brogues", Brand = "Massimo Dutti", Price = 169.99m, ImageUrl = "[https://picsum.photos/id/1070/1200/1800](https://picsum.photos/id/1070/1200/1800)", Description = "Timeless brogues for a formal look.", Category = menShoesCategory },
                new Product { Name = "Leather Boots", Brand = "Zara", Price = 119.50m, ImageUrl = "[https://picsum.photos/id/1011/1200/1800](https://picsum.photos/id/1011/1200/1800)", Description = "Durable and stylish leather boots.", Category = menShoesCategory },
                new Product { Name = "Casual Sneakers", Brand = "Massimo Dutti", Price = 95.00m, ImageUrl = "[https://picsum.photos/id/1025/1200/1800](https://picsum.photos/id/1025/1200/1800)", Description = "Comfortable everyday sneakers.", Category = menShoesCategory },
                
                // Men's Bags
                new Product { Name = "Leather Briefcase", Brand = "Massimo Dutti", Price = 249.99m, ImageUrl = "[https://picsum.photos/id/1015/1200/1800](https://picsum.photos/id/1015/1200/1800)", Description = "Professional leather briefcase for the office.", Category = menBagsCategory },
                new Product { Name = "Backpack", Brand = "Zara", Price = 79.99m, ImageUrl = "[https://picsum.photos/id/1027/1200/1800](https://picsum.photos/id/1027/1200/1800)", Description = "A practical and modern backpack.", Category = menBagsCategory },
                
                // Men's Glasses
                new Product { Name = "Square Frame Glasses", Brand = "Zara", Price = 49.99m, ImageUrl = "[https://picsum.photos/id/1039/1200/1800](https://picsum.photos/id/1039/1200/1800)", Description = "Stylish square-frame glasses.", Category = menGlassesCategory },
                new Product { Name = "Round Sunglasses", Brand = "Massimo Dutti", Price = 65.99m, ImageUrl = "[https://picsum.photos/id/214/1200/1800](https://picsum.photos/id/214/1200/1800)", Description = "Vintage-inspired round sunglasses.", Category = menGlassesCategory },
                
                // Products for main category pages (just for visual variety)
                new Product { Name = "Wool Sweater", Brand = "Massimo Dutti", Price = 89.99m, ImageUrl = "[https://picsum.photos/id/1041/1200/1800](https://picsum.photos/id/1041/1200/1800)", Description = "A warm and comfortable wool sweater.", Category = menCategory },
                new Product { Name = "Cotton Trousers", Brand = "Zara", Price = 59.99m, ImageUrl = "[https://picsum.photos/id/1042/1200/1800](https://picsum.photos/id/1042/1200/1800)", Description = "Everyday cotton trousers.", Category = menCategory },
                new Product { Name = "Silk Blouse", Brand = "Massimo Dutti", Price = 79.99m, ImageUrl = "[https://picsum.photos/id/1043/1200/1800](https://picsum.photos/id/1043/1200/1800)", Description = "An elegant silk blouse.", Category = womenCategory },
                new Product { Name = "Flowy Skirt", Brand = "Zara", Price = 45.99m, ImageUrl = "[https://picsum.photos/id/1044/1200/1800](https://picsum.photos/id/1044/1200/1800)", Description = "A light and airy summer skirt.", Category = womenCategory },
            };

                await context.Products.AddRangeAsync(products);
                await context.SaveChangesAsync();
                _logger.LogInformation("Database seeded successfully!");
            }
        }
    }
}
