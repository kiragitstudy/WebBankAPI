using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebBankAPI.Application.DTOs;
using WebBankAPI.Application.Interfaces;

namespace WebBankAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController(ITransactionService transactionService) : ControllerBase
{
    /// <summary>
    /// Перевод между своими счетами или другому пользователю
    /// </summary>
    [HttpPost("transfer")]
    public async Task<ActionResult<ApiResponse<TransactionDto>>> Transfer([FromBody] CreateTransactionDto dto)
    {
        try
        {
            var userId = GetUserId();
            var result = await transactionService.CreateTransactionAsync(userId, dto);
            return Ok(new ApiResponse<TransactionDto>
            {
                Success = true,
                Data = result,
                Message = "Перевод выполнен успешно"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<TransactionDto>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Пополнение счёта наличными
    /// </summary>
    [HttpPost("deposit")]
    public async Task<ActionResult<ApiResponse<TransactionDto>>> Deposit([FromBody] DepositDto dto)
    {
        try
        {
            var userId = GetUserId();
            var result = await transactionService.DepositAsync(userId, dto);
            return Ok(new ApiResponse<TransactionDto>
            {
                Success = true,
                Data = result,
                Message = "Счёт успешно пополнен"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<TransactionDto>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Снятие наличных (лимит 50,000 в день)
    /// </summary>
    [HttpPost("withdraw")]
    public async Task<ActionResult<ApiResponse<TransactionDto>>> Withdraw([FromBody] WithdrawalDto dto)
    {
        try
        {
            var userId = GetUserId();
            var result = await transactionService.WithdrawAsync(userId, dto);
            return Ok(new ApiResponse<TransactionDto>
            {
                Success = true,
                Data = result,
                Message = "Снятие выполнено успешно"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<TransactionDto>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Оплата услуг (коммуналка, интернет, кредит и т.д.)
    /// </summary>
    [HttpPost("payment")]
    public async Task<ActionResult<ApiResponse<TransactionDto>>> Payment([FromBody] PaymentDto dto)
    {
        try
        {
            var userId = GetUserId();
            var result = await transactionService.PaymentAsync(userId, dto);
            return Ok(new ApiResponse<TransactionDto>
            {
                Success = true,
                Data = result,
                Message = "Оплата выполнена успешно"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<TransactionDto>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Оплата картой в магазине
    /// </summary>
    [HttpPost("card-payment")]
    public async Task<ActionResult<ApiResponse<TransactionDto>>> CardPayment([FromBody] CardPaymentDto dto)
    {
        try
        {
            var userId = GetUserId();
            var result = await transactionService.CardPaymentAsync(userId, dto);
            return Ok(new ApiResponse<TransactionDto>
            {
                Success = true,
                Data = result,
                Message = "Оплата картой выполнена"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<TransactionDto>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// История транзакций счёта (с пагинацией)
    /// </summary>
    [HttpGet("account/{accountId}")]
    public async Task<ActionResult<ApiResponse<PagedResult<TransactionDto>>>> GetAccountTransactions(
        Guid accountId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var userId = GetUserId();
            var result = await transactionService.GetAccountTransactionsAsync(accountId, userId, page, pageSize);
            return Ok(new ApiResponse<PagedResult<TransactionDto>>
            {
                Success = true,
                Data = result,
                Message = "История транзакций получена"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<PagedResult<TransactionDto>>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Выписка по счёту за период
    /// </summary>
    [HttpGet("statement/{accountId}")]
    public async Task<ActionResult<ApiResponse<AccountStatementDto>>> GetStatement(
        Guid accountId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var userId = GetUserId();
            var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
            var end = endDate ?? DateTime.UtcNow;
            
            var result = await transactionService.GetAccountStatementAsync(accountId, userId, start, end);
            return Ok(new ApiResponse<AccountStatementDto>
            {
                Success = true,
                Data = result,
                Message = "Выписка сформирована"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<AccountStatementDto>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Статистика банка (общая информация по всем транзакциям и системным счетам)
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<ApiResponse<BankStatisticsDto>>> GetBankStatistics()
    {
        try
        {
            var result = await transactionService.GetBankStatisticsAsync();
            return Ok(new ApiResponse<BankStatisticsDto>
            {
                Success = true,
                Data = result,
                Message = "Статистика банка получена"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<BankStatisticsDto>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim!);
    }
}