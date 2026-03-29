using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using rag_experiment.Services.FilingDownloader;

namespace rag_experiment.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CompaniesController : ControllerBase
{
    private readonly ICompanyDirectoryService _companyDirectoryService;
    private readonly ICompanyFilingsService _companyFilingsService;

    public CompaniesController(
        ICompanyDirectoryService companyDirectoryService,
        ICompanyFilingsService companyFilingsService)
    {
        _companyDirectoryService = companyDirectoryService;
        _companyFilingsService = companyFilingsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCompanies(CancellationToken ct)
    {
        try
        {
            var companies = await _companyDirectoryService.GetCompaniesAsync(ct);

            return Ok(companies.Select(company => new
            {
                company.Cik,
                company.CikNumber,
                company.Name,
                company.Ticker,
                company.Exchange
            }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred while retrieving companies: {ex.Message}");
        }
    }

    [HttpGet("{companyIdentifier}/filings")]
    public async Task<IActionResult> GetCompanyFilings(
        string companyIdentifier,
        CancellationToken ct)
    {
        try
        {
            var filings = await _companyFilingsService.GetAvailableFilingsAsync(companyIdentifier, ct);
            if (filings == null)
            {
                return NotFound("Company filings not found");
            }

            return Ok(new
            {
                filings.Cik,
                filings.Name,
                filings.Tickers,
                filings.Exchanges,
                AvailableFilingTypes = filings.AvailableFilingTypes.Select(form => new
                {
                    form.FormType,
                    form.FilingCount,
                    LatestFilingDate = form.LatestFilingDate?.ToString("yyyy-MM-dd")
                })
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred while retrieving company filings: {ex.Message}");
        }
    }
}
