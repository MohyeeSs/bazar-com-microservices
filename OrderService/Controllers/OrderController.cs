
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using OrderService.Data;
using OrderService.Models;

namespace OrderService.Controllers;

[ApiController]
[Route("")]
public class OrderController : ControllerBase
{
    private readonly IHttpClientFactory _clients;
    private readonly OrderDatabase _database;

    public OrderController(
        IHttpClientFactory clients,
        OrderDatabase database)
    {
        _clients = clients;
        _database = database;
    }

    [HttpPost("purchase/{id:int}")]
    public async Task<IActionResult> Purchase(int id)
    {
        if (id <= 0)
            return BadRequest(new { error = "Invalid book ID." });

        var catalog = _clients.CreateClient("catalog");

        CatalogBookInfo? book;

        try
        {
            using var infoResponse =
                await catalog.GetAsync($"info/{id}");

            if (infoResponse.StatusCode == HttpStatusCode.NotFound)
                return NotFound(new { error = "Book not found." });

            if (!infoResponse.IsSuccessStatusCode)
                return StatusCode(502,
                    new { error = "Catalog service error." });

            book = await infoResponse.Content
                .ReadFromJsonAsync<CatalogBookInfo>();
        }
        catch (Exception ex) when (
            ex is HttpRequestException ||
            ex is TaskCanceledException ||
            ex is JsonException)
        {
            return StatusCode(503,
                new { error = "Catalog service unavailable." });
        }

        if (book is null)
            return StatusCode(502,
                new { error = "Invalid catalog response." });

        if (book.Quantity <= 0)
            return Conflict(new { error = "Book is out of stock." });

        long orderId = await _database.CreatePendingAsync(
            id, book.Title, book.Price);

        try
        {
            using var updateResponse = await catalog.PutAsJsonAsync(
                $"update/{id}",
                new { quantityChange = -1 });

            if (updateResponse.StatusCode == HttpStatusCode.Conflict)
            {
                await _database.SetStatusAsync(orderId, "Rejected");

                return Conflict(new
                {
                    orderId,
                    error = "Insufficient stock."
                });
            }

            if (updateResponse.StatusCode == HttpStatusCode.NotFound)
            {
                await _database.SetStatusAsync(orderId, "Rejected");

                return NotFound(new
                {
                    orderId,
                    error = "Book not found."
                });
            }

            if (!updateResponse.IsSuccessStatusCode)
            {
                return StatusCode(502, new
                {
                    orderId,
                    status = "Pending",
                    error = "Catalog update could not be confirmed."
                });
            }

            await _database.SetStatusAsync(orderId, "Completed");

            return Ok(new
            {
                orderId,
                bookId = id,
                title = book.Title,
                price = book.Price,
                status = "Completed"
            });
        }
        catch (Exception ex) when (
            ex is HttpRequestException ||
            ex is TaskCanceledException)
        {
            return StatusCode(502, new
            {
                orderId,
                status = "Pending",
                error = "Purchase result could not be confirmed."
            });
        }
    }
}
