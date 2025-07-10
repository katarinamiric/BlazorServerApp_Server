namespace BlazorServerApp_Server
{
    public interface IPrerenderDataService<TComponent, TData> where TData : class
    {
        Task<TData> GetPrerenderDataAsync();
    }
}
