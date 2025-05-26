using BlazorServerApp_Server.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorServerApp_Server.Services
{
    public class ReportDataService
    {
        private readonly ILogger<ReportDataService> _logger;
        private readonly ApplicationDbContext _context;
        private static readonly Random _random = new Random();


        public ReportDataService(ILogger<ReportDataService> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<ReportData> GetComplexReportDataAsync()
        {
            _logger.LogInformation("ReportDataService: Simulating complex report data generation...");

            // Simulate a significant delay (e.g., 3 to 6 seconds)
            int delaySeconds = _random.Next(3, 7); // Random delay between 3 and 6 seconds
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));

            var itemsCount = _random.Next(50, 151); // Between 50 and 150 items
            var details = new List<string>();
            for (int i = 0; i < itemsCount; i++)
            {
                details.Add($"Detail item {i + 1}: Some generated text to make the HTML larger. Lorem ipsum dolor sit amet, consectetur adipiscing elit.");
            }

            _logger.LogInformation($"ReportDataService: Complex report data generated after {delaySeconds} seconds.");


            return new ReportData
            {
                Title = $"Detailed Performance Report ({DateTime.Now.ToShortDateString()})",
                Content = $"This report contains a summary of recent performance metrics. Generated on {DateTime.Now}.",
                GeneratedAt = DateTime.Now, // This timestamp will be crucial for testing prerendering
                DataSource = "Server-side (fresh fetch)", // Default assumption, overridden if prerendered
                TotalItems = itemsCount,
                Details = details
            };
        }
    }
}