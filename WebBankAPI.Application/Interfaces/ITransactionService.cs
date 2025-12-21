using WebBankAPI.Application.DTOs;

namespace WebBankAPI.Application.Interfaces;

public interface ITransactionService
{
    Task<TransactionDto> CreateTransactionAsync(Guid userId, CreateTransactionDto dto);
    Task<TransactionDto> DepositAsync(Guid userId, DepositDto dto);
    Task<TransactionDto> WithdrawAsync(Guid userId, WithdrawalDto dto);
    Task<TransactionDto> PaymentAsync(Guid userId, PaymentDto dto);
    Task<TransactionDto> CardPaymentAsync(Guid userId, CardPaymentDto dto);
    Task<PagedResult<TransactionDto>> GetAccountTransactionsAsync(Guid accountId, Guid userId, int page, int pageSize);
    Task<AccountStatementDto> GetAccountStatementAsync(Guid accountId, Guid userId, DateTime startDate, DateTime endDate);
    Task<BankStatisticsDto> GetBankStatisticsAsync();
}
