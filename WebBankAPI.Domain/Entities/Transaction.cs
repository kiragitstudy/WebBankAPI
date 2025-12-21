namespace WebBankAPI.Domain.Entities;

public class Transaction
{
    public Guid Id { get; set; }
    public Guid FromAccountId { get; set; }
    public Guid ToAccountId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public TransactionStatus Status { get; set; }
    public TransactionType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? FailureReason { get; set; }
    
    public Account FromAccount { get; set; } = null!;
    public Account ToAccount { get; set; } = null!;
}

public enum TransactionStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}

public enum TransactionType
{
    Transfer = 1,
    Deposit = 2,
    Withdrawal = 3,
    Payment = 4,
    CashDeposit = 5,
    CashWithdrawal = 6,
    CardPayment = 7,
    Refund = 8
}