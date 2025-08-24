using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using BlazorServerApp_Server.Data;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // IMPORTANT: You need to specify a connection string here that the migration tool can use.
        // It's a common practice to use a hardcoded or a local development connection string here
        // as the tool cannot access your app's configuration at runtime.
        optionsBuilder.UseSqlServer("Server=localhost,1433;Database=testapp2;User Id=sa;Password=K@tarina2808!;MultipleActiveResultSets=true;TrustServerCertificate=True");

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}