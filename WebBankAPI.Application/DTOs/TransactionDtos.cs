using System.ComponentModel.DataAnnotations;

namespace WebBankAPI.Application.DTOs;


public class CreateTransactionDto
{
    [Required]
    public Guid FromAccountId { get; set; }
    
    [Required]
    public Guid ToAccountId { get; set; }
    
    [Required, Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }
    
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;
}

public class DepositDto
{
    [Required]
    public Guid AccountId { get; set; }
    
    [Required, Range(0.01, 1000000)]
    public decimal Amount { get; set; }
    
    [MaxLength(200)]
    public string Description { get; set; } = "Пополнение счёта";
}

public class WithdrawalDto
{
    [Required]
    public Guid AccountId { get; set; }
    
    [Required, Range(0.01, 50000)]
    public decimal Amount { get; set; }
    
    [MaxLength(200)]
    public string Description { get; set; } = "Снятие наличных";
}

public class PaymentDto
{
    [Required]
    public Guid FromAccountId { get; set; }
    
    [Required, MinLength(2)]
    public string RecipientName { get; set; } = string.Empty;
    
    [Required, MinLength(10)]
    public string RecipientAccount { get; set; } = string.Empty;
    
    [Required, Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }
    
    [Required]
    public PaymentCategory Category { get; set; }
    
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;
}

public class CardPaymentDto
{
    [Required]
    public Guid AccountId { get; set; }
    
    [Required, MinLength(2)]
    public string MerchantName { get; set; } = string.Empty;
    
    [Required, Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }
    
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;
}

public enum PaymentCategory
{
    Utilities = 1,      // Коммунальные услуги
    Internet = 2,       // Интернет
    Mobile = 3,         // Мобильная связь
    Insurance = 4,      // Страхование
    Loan = 5,           // Кредит
    Tax = 6,            // Налоги
    Rent = 7,           // Аренда
    Other = 99          // Прочее
}

public class TransactionDto
{
    public Guid Id { get; set; }
    public Guid FromAccountId { get; set; }
    public Guid ToAccountId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? RecipientInfo { get; set; }
}

public class AccountStatementDto
{
    public Guid AccountId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public decimal TotalDeposits { get; set; }
    public decimal TotalWithdrawals { get; set; }
    public decimal TotalPayments { get; set; }
    public int TransactionCount { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public List<TransactionDto> Transactions { get; set; } = new();
}

public class BankStatisticsDto
{
    public decimal TotalDeposits { get; set; }
    public decimal TotalWithdrawals { get; set; }
    public decimal TotalPayments { get; set; }
    public decimal TotalMerchantPayments { get; set; }
    public decimal TotalTransfers { get; set; }
    public int TotalTransactionCount { get; set; }
    public Dictionary<string, decimal> SystemAccountBalances { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}
