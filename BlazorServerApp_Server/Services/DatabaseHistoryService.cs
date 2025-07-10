//using BlazorServerApp_Server.Data;
//using BlazorServerApp_Server.Redis;
//using Microsoft.EntityFrameworkCore;
//using StackExchange.Redis;

//namespace BlazorServerApp_Server.Services
//{
//    public class DatabaseHistoryService
//    {
//        private readonly ILogger<DatabaseHistoryService> _logger;
//        private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
//        private readonly IDatabase _redisDb;


//        private const int RedisHistoryMaxCount = 4;

//        public DatabaseHistoryService(ILogger<DatabaseHistoryService> logger,
//            IDbContextFactory<ApplicationDbContext> dbContextFactory, IDatabase redisDb)
//        {
//            _logger = logger;
//            _dbContextFactory = dbContextFactory;
//            _redisDb = redisDb;
//        }

//        public async Task AddPageVisitAsync(string userId, string pageUrl, string deviceType) // deviceType is logged but not stored in Redis List here
//        {
//            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(pageUrl))
//            {
//                _logger.LogWarning("Attempted to log a page visit with null/empty userId or pageUrl.");
//                return;
//            }

//            string redisKey = $"user:{userId}:history";

//            try
//            {
//                RedisValue[] historyValues = await _redisDb.ListRangeAsync(redisKey, 0, RedisHistoryMaxCount - 1);

//                var logEntry = new AdvancedNavigationLogEntry
//                {
//                    PreviousPage1Url = historyValues[1].ToString(), // P1: Page immediately preceding NextPageUrl
//                    PreviousPage2Url = historyValues[2].ToString(), // P2: Page two before NextPageUrl
//                    PreviousPage3Url = historyValues[3].ToString(), // P3: Page three before NextPageUrl
//                    TimeOfDayInHours =
//                        (float)DateTime.UtcNow.TimeOfDay.TotalHours, // Time of day for the NextPageUrl visit
//                    UserId = userId,
//                    DeviceType = deviceType, // Device type recorded for the NextPageUrl visit
//                    NextPageUrl = historyValues[0].ToString() // The current page is the 'NextPageUrl' for this sequence
//                };

//                await SaveAdvancedNavigationLogEntryAsync(logEntry);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex,
//                    $"RedisPageHistoryService: Error adding page visit for User={userId}");
//            }
//        }


//        private async Task SaveAdvancedNavigationLogEntryAsync(AdvancedNavigationLogEntry entry)
//        {
//            using (var context = _dbContextFactory.CreateDbContext())
//            {
//                context.AdvancedNavigationLogEntries.Add(entry);
//                await context.SaveChangesAsync();
//                _logger.LogDebug(
//                    $"Saved AdvancedNavigationLogEntry to DB for user {entry.UserId}. Next page: {entry.NextPageUrl}");
//            }
//        }
//    }
//}