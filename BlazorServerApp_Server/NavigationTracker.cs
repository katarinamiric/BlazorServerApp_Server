namespace BlazorServerApp_Server
{
    public class NavigationTracker
    {
        public string? LastVisitedPage { get; private set; }

        public void Update(string currentUri)
        {
            LastVisitedPage = currentUri;
        }
    }

}
