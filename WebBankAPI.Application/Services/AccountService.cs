using WebBankAPI.Application.Interfaces;
using WebBankAPI.Application.DTOs;
using WebBankAPI.Domain.Entities;
using WebBankAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace WebBankAPI.Application.Services;

public class AccountService(BankDbContext context) : IAccountService
{
    public async Task<AccountDto> CreateAccountAsync(Guid userId, CreateAccountDto dto)
    {
        var user = await context.Users.FindAsync(userId);
        if (user == null)
            throw new InvalidOperationException("User not found");

        var accountNumber = GenerateAccountNumber();

        var account = new Account
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AccountNumber = accountNumber,
            Balance = 0,
            Currency = dto.Currency,
            Type = dto.Type,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        return MapToDto(account);
    }

    public async Task<List<AccountDto>> GetUserAccountsAsync(Guid userId)
    {
        var accounts = await context.Accounts
            .Where(a => a.UserId == userId && a.IsActive)
            .ToListAsync();

        return accounts.Select(MapToDto).ToList();
    }

    public async Task<AccountDto?> GetAccountByIdAsync(Guid accountId, Guid userId)
    {
        var account = await context.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId);

        return account != null ? MapToDto(account) : null;
    }

    private string GenerateAccountNumber()
    {
        return $"ACC{DateTime.UtcNow:yyyyMMdd}{Random.Shared.Next(100000, 999999)}";
    }

    private AccountDto MapToDto(Account account) => new()
    {
        Id = account.Id,
        AccountNumber = account.AccountNumber,
        Balance = account.Balance,
        Currency = account.Currency,
        Type = account.Type,
        CreatedAt = account.CreatedAt,
        IsActive = account.IsActive
    };
}