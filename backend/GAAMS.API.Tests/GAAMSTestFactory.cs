
using GAAMS.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GAAMS.API.Tests;

public class GAAMSTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<GAAMSDbContext>>();

            services.AddDbContext<GAAMSDbContext>(options =>
                options.UseNpgsql(
                    Environment.GetEnvironmentVariable("GAAMS_TEST_CONNECTION")
                ));
        });
    }
}
