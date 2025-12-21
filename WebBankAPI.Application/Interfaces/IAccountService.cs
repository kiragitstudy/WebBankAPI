using WebBankAPI.Application.DTOs;

namespace WebBankAPI.Application.Interfaces;

public interface IAccountService
{
    Task<AccountDto> CreateAccountAsync(Guid userId, CreateAccountDto dto);
    Task<List<AccountDto>> GetUserAccountsAsync(Guid userId);
    Task<AccountDto?> GetAccountByIdAsync(Guid accountId, Guid userId);
}