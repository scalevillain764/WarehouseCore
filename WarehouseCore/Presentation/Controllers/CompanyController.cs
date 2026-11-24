using Application.Companies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Companies.Requests;

namespace Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CompanyController : BaseController
{
    private readonly ICompanyService _companyService;
    public CompanyController(ICompanyService companyService)
    {
        _companyService = companyService;
    }

    [HttpGet("current_company")]
    public async Task<IActionResult> GetCurrentCompany(CancellationToken token)
        => ProcessResult(await _companyService.GetCurrentCompanyAsync(currentUserId, token));

    [HttpPatch]
    public async Task<IActionResult> EditCurrentCompanyAsync([FromBody] EditCompanyRequest request, CancellationToken token)
        => ProcessResult(await _companyService.EditCurrentCompanyAsync(currentUserId, request, token));  
}