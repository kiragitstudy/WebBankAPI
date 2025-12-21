using WebBankAPI.Application.Interfaces;
using WebBankAPI.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace WebBankAPI.Application.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ITransactionService, TransactionService>();
        
        return services;
    }
}