namespace BlazorServerApp_Server.Data
{
    public class ReportData
    {
        public string Title { get; set; } = "Default Report Title";
        public string Content { get; set; } = "Report content will appear here.";
        public DateTime GeneratedAt { get; set; }
        public string DataSource { get; set; } = "Client-side fetch";
        public int TotalItems { get; set; }
        public List<string> Details { get; set; } = new List<string>();
    }
}