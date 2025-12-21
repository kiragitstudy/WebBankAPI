using System.ComponentModel.DataAnnotations;
using WebBankAPI.Domain.Entities;

namespace WebBankAPI.Application.DTOs;

public class CreateAccountDto
{
    [Required]
    public AccountType Type { get; set; }
    
    [Required, MinLength(3), MaxLength(3)]
    public string Currency { get; set; } = "USD";
}

public class AccountDto
{
    public Guid Id { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}
