using HouseholdStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace HouseholdStore.Controllers;

[ApiController]
[Route("api/belarus-geo")]
public sealed class BelarusGeoController : ControllerBase
{
    private readonly BelarusGeoSuggestService _geoSuggest;

    public BelarusGeoController(BelarusGeoSuggestService geoSuggest)
    {
        _geoSuggest = geoSuggest;
    }

    [HttpGet("cities")]
    public async Task<IActionResult> Cities([FromQuery] string? term, [FromQuery] int limit = 30)
    {
        if (string.IsNullOrWhiteSpace(term)) return Ok(Array.Empty<string>());
        var result = await _geoSuggest.SuggestCitiesAsync(term, limit);
        return Ok(result);
    }

    [HttpGet("streets")]
    public async Task<IActionResult> Streets(
        [FromQuery] string? city,
        [FromQuery] string? term,
        [FromQuery] int limit = 30)
    {
        if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(term))
            return Ok(Array.Empty<string>());

        var result = await _geoSuggest.SuggestStreetsAsync(city, term, limit);
        return Ok(result);
    }
}
