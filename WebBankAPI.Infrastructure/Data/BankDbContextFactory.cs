using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace WebBankAPI.Infrastructure.Data;

public class BankDbContextFactory : IDesignTimeDbContextFactory<BankDbContext>
{
    public BankDbContext CreateDbContext(string[] args)
    {
        // Мы запускаем команду из корня solution: D:\code\Csharp\WebBankAPI
        // Поэтому appsettings лежит в WebBankAPI.API
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "WebBankAPI");

        // На случай запуска из другой папки
        if (!Directory.Exists(basePath))
            basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "WebBankAPI");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var cs = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        var options = new DbContextOptionsBuilder<BankDbContext>()
            .UseNpgsql(cs, x => x.MigrationsAssembly("WebBankAPI.Infrastructure"))
            .Options;

        return new BankDbContext(options);
    }
}