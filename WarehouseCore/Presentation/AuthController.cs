using Application.Auth;
using Application.Auth.Requests;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : BaseController
{
    private readonly IAuthService _authService;
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("registrate/company")]
    public async Task<IActionResult> RegistrateCompanyAsync([FromBody] RegistrateCompanyRequest request, CancellationToken token)
        => ProcessResult(await _authService.RegistrateCompanyAsync(request, token));
}