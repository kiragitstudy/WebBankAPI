using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebBankAPI.Application.DTOs;
using WebBankAPI.Application.Interfaces;

namespace WebBankAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountsController(IAccountService accountService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<AccountDto>>> CreateAccount([FromBody] CreateAccountDto dto)
    {
        try
        {
            var userId = GetUserId();
            var result = await accountService.CreateAccountAsync(userId, dto);
            return Ok(new ApiResponse<AccountDto>
            {
                Success = true,
                Data = result,
                Message = "Account created successfully"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<AccountDto>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<AccountDto>>>> GetAccounts()
    {
        try
        {
            var userId = GetUserId();
            var result = await accountService.GetUserAccountsAsync(userId);
            return Ok(new ApiResponse<List<AccountDto>>
            {
                Success = true,
                Data = result,
                Message = "Accounts retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<List<AccountDto>>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<AccountDto>>> GetAccount(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var result = await accountService.GetAccountByIdAsync(id, userId);
            
            if (result == null)
                return NotFound(new ApiResponse<AccountDto>
                {
                    Success = false,
                    Message = "Account not found"
                });

            return Ok(new ApiResponse<AccountDto>
            {
                Success = true,
                Data = result,
                Message = "Account retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<AccountDto>
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