using Microsoft.AspNetCore.Mvc;

namespace FrontendService.Controllers;

[ApiController]
[Route("")]
public class FrontendController : ControllerBase
{
    private readonly IHttpClientFactory _clients;

    public FrontendController(IHttpClientFactory clients)
    {
        _clients = clients;
    }

    [HttpGet("search/{topic}")]
    public async Task<IActionResult> Search(string topic)
    {
        var catalog = _clients.CreateClient("catalog");

        try
        {
            using var response = await catalog.GetAsync($"search/{Uri.EscapeDataString(topic)}");
            return await ForwardResponse(response);
        }
        catch (HttpRequestException)
        {
            return StatusCode(503, new
            {
                error = "Catalog service unavailable."
            });
        }
        catch (TaskCanceledException)
        {
            return StatusCode(504, new
            {
                error = "Catalog service request timed out."
            });
        }
    }

    [HttpGet("info/{id:int}")]
    public async Task<IActionResult> GetInfo(int id)
    {
        if (id <= 0)
            return BadRequest(new { error = "Invalid book ID." });

        var catalog = _clients.CreateClient("catalog");

        try
        {
            using var response = await catalog.GetAsync($"info/{id}");
            return await ForwardResponse(response);
        }
        catch (HttpRequestException)
        {
            return StatusCode(503, new
            {
                error = "Catalog service unavailable."
            });
        }
        catch (TaskCanceledException)
        {
            return StatusCode(504, new
            {
                error = "Catalog service request timed out."
            });
        }
    }

    [HttpPost("purchase/{id:int}")]
    public async Task<IActionResult> Purchase(int id)
    {
        if (id <= 0)
            return BadRequest(new { error = "Invalid book ID." });

        var order = _clients.CreateClient("order");

        try
        {
            using var response = await order.PostAsync(
                $"purchase/{id}", null);

            return await ForwardResponse(response);
        }
        catch (HttpRequestException)
        {
            return StatusCode(503, new
            {
                error = "Order service unavailable."
            });
        }
        catch (TaskCanceledException)
        {
            return StatusCode(504, new
            {
                error = "Order service request timed out."
            });
        }
    }

    private static async Task<IActionResult> ForwardResponse(
        HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        return new ContentResult
        {
            Content = content,
            ContentType = response.Content.Headers.ContentType?.ToString()
                ?? "application/json",
            StatusCode = (int)response.StatusCode
        };
    }
}
