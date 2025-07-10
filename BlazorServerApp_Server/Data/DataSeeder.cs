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
            }
        }
    }
}
