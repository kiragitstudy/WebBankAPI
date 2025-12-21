using WebBankAPI.Application.DTOs;
using WebBankAPI.Application.Interfaces;
using WebBankAPI.Domain.Entities;
using WebBankAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace WebBankAPI.Application.Services;

public class TransactionService(
    BankDbContext context,
    IDistributedCache cache,
    ILogger<TransactionService> logger)
    : ITransactionService
{
    // Перевод между счетами
    public async Task<TransactionDto> CreateTransactionAsync(Guid userId, CreateTransactionDto dto)
    {
        using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var fromAccount = await ValidateAccountOwnership(dto.FromAccountId, userId);
            var toAccount = await ValidateAccountExists(dto.ToAccountId);

            ValidateBalance(fromAccount, dto.Amount);
            ValidateCurrency(fromAccount, toAccount);

            var txn = new Transaction
            {
                Id = Guid.NewGuid(),
                FromAccountId = dto.FromAccountId,
                ToAccountId = dto.ToAccountId,
                Amount = dto.Amount,
                Description = dto.Description,
                Status = TransactionStatus.Pending,
                Type = TransactionType.Transfer,
                CreatedAt = DateTime.UtcNow
            };

            context.Transactions.Add(txn);

            fromAccount.Balance -= dto.Amount;
            toAccount.Balance += dto.Amount;

            txn.Status = TransactionStatus.Completed;
            txn.CompletedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            await InvalidateCache(dto.FromAccountId, dto.ToAccountId);

            logger.LogInformation("Transfer completed: {Amount} from {From} to {To}", 
                dto.Amount, dto.FromAccountId, dto.ToAccountId);

            return MapToDto(txn);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Transfer failed");
            throw new InvalidOperationException($"Ошибка перевода: {ex.Message}", ex);
        }
    }

    // Пополнение счёта
    public async Task<TransactionDto> DepositAsync(Guid userId, DepositDto dto)
    {
        using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var account = await ValidateAccountOwnership(dto.AccountId, userId);

            // Создаём системный счёт для депозитов (если не существует)
            var systemAccount = await GetOrCreateSystemAccount("DEPOSITS", account.Currency);

            var txn = new Transaction
            {
                Id = Guid.NewGuid(),
                FromAccountId = systemAccount.Id,
                ToAccountId = dto.AccountId,
                Amount = dto.Amount,
                Description = dto.Description,
                Status = TransactionStatus.Pending,
                Type = TransactionType.CashDeposit,
                CreatedAt = DateTime.UtcNow
            };

            context.Transactions.Add(txn);

            // Увеличиваем баланс пользователя и уменьшаем системный
            account.Balance += dto.Amount;
            systemAccount.Balance -= dto.Amount;

            txn.Status = TransactionStatus.Completed;
            txn.CompletedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            await InvalidateCache(dto.AccountId);

            logger.LogInformation("Deposit completed: {Amount} to account {Account}", 
                dto.Amount, dto.AccountId);

            return MapToDto(txn);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Deposit failed");
            throw new InvalidOperationException($"Ошибка пополнения: {ex.Message}", ex);
        }
    }

    // Снятие наличных
    public async Task<TransactionDto> WithdrawAsync(Guid userId, WithdrawalDto dto)
    {
        using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var account = await ValidateAccountOwnership(dto.AccountId, userId);
            ValidateBalance(account, dto.Amount);

            // Дневной лимит на снятие
            var todayWithdrawals = await context.Transactions
                .Where(t => t.FromAccountId == dto.AccountId 
                    && t.Type == TransactionType.CashWithdrawal
                    && t.Status == TransactionStatus.Completed
                    && t.CreatedAt.Date == DateTime.UtcNow.Date)
                .SumAsync(t => t.Amount);

            if (todayWithdrawals + dto.Amount > 50000)
                throw new InvalidOperationException("Превышен дневной лимит на снятие наличных (50,000)");

            var systemAccount = await GetOrCreateSystemAccount("WITHDRAWALS", account.Currency);

            var txn = new Transaction
            {
                Id = Guid.NewGuid(),
                FromAccountId = dto.AccountId,
                ToAccountId = systemAccount.Id,
                Amount = dto.Amount,
                Description = dto.Description,
                Status = TransactionStatus.Pending,
                Type = TransactionType.CashWithdrawal,
                CreatedAt = DateTime.UtcNow
            };

            context.Transactions.Add(txn);

            account.Balance -= dto.Amount;
            systemAccount.Balance += dto.Amount;

            txn.Status = TransactionStatus.Completed;
            txn.CompletedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            await InvalidateCache(dto.AccountId);

            logger.LogInformation("Withdrawal completed: {Amount} from account {Account}", 
                dto.Amount, dto.AccountId);

            return MapToDto(txn);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Withdrawal failed");
            throw new InvalidOperationException($"Ошибка снятия: {ex.Message}", ex);
        }
    }

    // Оплата услуг
    public async Task<TransactionDto> PaymentAsync(Guid userId, PaymentDto dto)
    {
        using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var account = await ValidateAccountOwnership(dto.FromAccountId, userId);
            ValidateBalance(account, dto.Amount);

            var systemAccount = await GetOrCreateSystemAccount("PAYMENTS", account.Currency);

            var txn = new Transaction
            {
                Id = Guid.NewGuid(),
                FromAccountId = dto.FromAccountId,
                ToAccountId = systemAccount.Id,
                Amount = dto.Amount,
                Description = $"Оплата: {dto.Category} - {dto.RecipientName} ({dto.RecipientAccount}). {dto.Description}",
                Status = TransactionStatus.Pending,
                Type = TransactionType.Payment,
                CreatedAt = DateTime.UtcNow
            };

            context.Transactions.Add(txn);

            account.Balance -= dto.Amount;
            systemAccount.Balance += dto.Amount;

            txn.Status = TransactionStatus.Completed;
            txn.CompletedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            await InvalidateCache(dto.FromAccountId);

            logger.LogInformation("Payment completed: {Amount} from {Account} to {Recipient}", 
                dto.Amount, dto.FromAccountId, dto.RecipientName);

            return MapToDto(txn, $"{dto.RecipientName} ({dto.RecipientAccount})");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Payment failed");
            throw new InvalidOperationException($"Ошибка оплаты: {ex.Message}", ex);
        }
    }

    // Оплата картой
    public async Task<TransactionDto> CardPaymentAsync(Guid userId, CardPaymentDto dto)
    {
        using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var account = await ValidateAccountOwnership(dto.AccountId, userId);
            ValidateBalance(account, dto.Amount);

            var systemAccount = await GetOrCreateSystemAccount("MERCHANTS", account.Currency);

            var txn = new Transaction
            {
                Id = Guid.NewGuid(),
                FromAccountId = dto.AccountId,
                ToAccountId = systemAccount.Id,
                Amount = dto.Amount,
                Description = $"Покупка в {dto.MerchantName}. {dto.Description}",
                Status = TransactionStatus.Pending,
                Type = TransactionType.CardPayment,
                CreatedAt = DateTime.UtcNow
            };

            context.Transactions.Add(txn);

            account.Balance -= dto.Amount;
            systemAccount.Balance += dto.Amount;

            txn.Status = TransactionStatus.Completed;
            txn.CompletedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            await InvalidateCache(dto.AccountId);

            logger.LogInformation("Card payment completed: {Amount} at {Merchant}", 
                dto.Amount, dto.MerchantName);

            return MapToDto(txn, dto.MerchantName);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Card payment failed");
            throw new InvalidOperationException($"Ошибка оплаты картой: {ex.Message}", ex);
        }
    }

    // История транзакций
    public async Task<PagedResult<TransactionDto>> GetAccountTransactionsAsync(
        Guid accountId, Guid userId, int page, int pageSize)
    {
        await ValidateAccountOwnership(accountId, userId);

        var cacheKey = $"account_transactions_{accountId}_page_{page}_size_{pageSize}";
        var cachedData = await cache.GetStringAsync(cacheKey);
        
        if (!string.IsNullOrEmpty(cachedData))
        {
            return JsonSerializer.Deserialize<PagedResult<TransactionDto>>(cachedData)!;
        }

        var query = context.Transactions
            .Where(t => t.FromAccountId == accountId || t.ToAccountId == accountId)
            .OrderByDescending(t => t.CreatedAt);

        var totalCount = await query.CountAsync();
        var transactions = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new PagedResult<TransactionDto>
        {
            Items = transactions.Select(t => MapToDto(t)).ToList(),
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };

        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };
        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), cacheOptions);

        return result;
    }

    // Выписка по счёту
    public async Task<AccountStatementDto> GetAccountStatementAsync(
        Guid accountId, Guid userId, DateTime startDate, DateTime endDate)
    {
        var account = await ValidateAccountOwnership(accountId, userId);

        var transactions = await context.Transactions
            .Where(t => (t.FromAccountId == accountId || t.ToAccountId == accountId)
                && t.CreatedAt >= startDate
                && t.CreatedAt <= endDate
                && t.Status == TransactionStatus.Completed)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        var deposits = transactions
            .Where(t => t.ToAccountId == accountId && 
                (t.Type == TransactionType.Deposit || 
                 t.Type == TransactionType.CashDeposit || 
                 t.Type == TransactionType.Transfer))
            .Sum(t => t.Amount);

        var withdrawals = transactions
            .Where(t => t.FromAccountId == accountId && 
                (t.Type == TransactionType.Withdrawal || 
                 t.Type == TransactionType.CashWithdrawal))
            .Sum(t => t.Amount);

        var payments = transactions
            .Where(t => t.FromAccountId == accountId && 
                (t.Type == TransactionType.Payment || 
                 t.Type == TransactionType.CardPayment))
            .Sum(t => t.Amount);

        return new AccountStatementDto
        {
            AccountId = accountId,
            AccountNumber = account.AccountNumber,
            CurrentBalance = account.Balance,
            TotalDeposits = deposits,
            TotalWithdrawals = withdrawals,
            TotalPayments = payments,
            TransactionCount = transactions.Count,
            PeriodStart = startDate,
            PeriodEnd = endDate,
            Transactions = transactions.Select(t => MapToDto(t)).ToList()
        };
    }

    // Статистика банка
    public async Task<BankStatisticsDto> GetBankStatisticsAsync()
    {
        var completedTransactions = await context.Transactions
            .Where(t => t.Status == TransactionStatus.Completed)
            .ToListAsync();

        var totalDeposits = completedTransactions
            .Where(t => t.Type == TransactionType.CashDeposit || t.Type == TransactionType.Deposit)
            .Sum(t => t.Amount);

        var totalWithdrawals = completedTransactions
            .Where(t => t.Type == TransactionType.CashWithdrawal || t.Type == TransactionType.Withdrawal)
            .Sum(t => t.Amount);

        var totalPayments = completedTransactions
            .Where(t => t.Type == TransactionType.Payment)
            .Sum(t => t.Amount);

        var totalMerchantPayments = completedTransactions
            .Where(t => t.Type == TransactionType.CardPayment)
            .Sum(t => t.Amount);

        var totalTransfers = completedTransactions
            .Where(t => t.Type == TransactionType.Transfer)
            .Sum(t => t.Amount);

        var systemAccounts = await context.Accounts
            .Where(a => a.AccountNumber.StartsWith("SYSTEM_"))
            .ToDictionaryAsync(a => a.AccountNumber, a => a.Balance);

        return new BankStatisticsDto
        {
            TotalDeposits = totalDeposits,
            TotalWithdrawals = totalWithdrawals,
            TotalPayments = totalPayments,
            TotalMerchantPayments = totalMerchantPayments,
            TotalTransfers = totalTransfers,
            TotalTransactionCount = completedTransactions.Count,
            SystemAccountBalances = systemAccounts,
            GeneratedAt = DateTime.UtcNow
        };
    }

    // Вспомогательные методы
    private async Task<Account> ValidateAccountOwnership(Guid accountId, Guid userId)
    {
        var account = await context.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId && a.IsActive);
        
        if (account == null)
            throw new InvalidOperationException("Счёт не найден или доступ запрещён");

        return account;
    }

    private async Task<Account> ValidateAccountExists(Guid accountId)
    {
        var account = await context.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId && a.IsActive);
        
        if (account == null)
            throw new InvalidOperationException("Счёт получателя не найден");

        return account;
    }

    private void ValidateBalance(Account account, decimal amount)
    {
        if (account.Balance < amount)
            throw new InvalidOperationException($"Недостаточно средств. Доступно: {account.Balance:F2}");
    }

    private void ValidateCurrency(Account from, Account to)
    {
        if (from.Currency != to.Currency)
            throw new InvalidOperationException("Валюты счетов не совпадают");
    }

    private async Task<Account> GetOrCreateSystemAccount(string type, string currency)
    {
        var accountNumber = $"SYSTEM_{type}_{currency}";
        var systemAccount = await context.Accounts
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);

        if (systemAccount == null)
        {
            // Создаём системного пользователя если его нет
            var systemUser = await context.Users
                .FirstOrDefaultAsync(u => u.Email == "system@bank.internal");

            if (systemUser == null)
            {
                systemUser = new User
                {
                    Id = Guid.NewGuid(),
                    Email = "system@bank.internal",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                    FullName = "System Account",
                    PhoneNumber = "+00000000000",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                context.Users.Add(systemUser);
            }

            systemAccount = new Account
            {
                Id = Guid.NewGuid(),
                UserId = systemUser.Id,
                AccountNumber = accountNumber,
                Balance = 0,
                Currency = currency,
                Type = AccountType.Checking,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            context.Accounts.Add(systemAccount);
            await context.SaveChangesAsync();
        }

        return systemAccount;
    }

    private async Task InvalidateCache(params Guid[] accountIds)
    {
        foreach (var accountId in accountIds)
        {
            await cache.RemoveAsync($"account_transactions_{accountId}");
        }
    }

    private TransactionDto MapToDto(Transaction transaction, string? recipientInfo = null) => new()
    {
        Id = transaction.Id,
        FromAccountId = transaction.FromAccountId,
        ToAccountId = transaction.ToAccountId,
        Amount = transaction.Amount,
        Description = transaction.Description,
        Status = transaction.Status.ToString(),
        Type = transaction.Type.ToString(),
        CreatedAt = transaction.CreatedAt,
        CompletedAt = transaction.CompletedAt,
        RecipientInfo = recipientInfo
    };
}