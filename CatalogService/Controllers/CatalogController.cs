
using CatalogService.Data;
using CatalogService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

namespace CatalogService.Controllers;

[ApiController]
[Route("")]
public class CatalogController : ControllerBase
{
    private readonly CatalogDatabase _database;

    public CatalogController(CatalogDatabase database)
    {
        _database = database;
    }

    
    [HttpGet("search/{topic}")]
    public async Task<IActionResult> Search(string topic)
    {
        await using var connection = _database.CreateConnection();

        await connection.OpenAsync();

        var command = connection.CreateCommand();

        command.CommandText = """
            SELECT Id, Title
            FROM Books
            WHERE Topic = $topic COLLATE NOCASE
            ORDER BY Id;
            """;

        command.Parameters.AddWithValue("$topic", topic);

        var items = new List<object>();

        await using var reader =await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            items.Add(new
            {
                id = reader.GetInt32(0),
                title = reader.GetString(1)
            });
        }

        return Ok(items);
    }

    [HttpGet("info/{id:int}")]
    public async Task<IActionResult> GetInfo(int id)
    {
        await using var connection = _database.CreateConnection();

        await connection.OpenAsync();

        var command = connection.CreateCommand();

        command.CommandText = """
            SELECT Title, Quantity, Price
            FROM Books
            WHERE Id = $id;
            """;

        command.Parameters.AddWithValue("$id", id);

        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return NotFound(new
            {
                error = "Book not found."
            });
        }

        var book = new
        {
            title = reader.GetString(0),
            quantity = reader.GetInt32(1),
            price = reader.GetDouble(2)
        };

        return Ok(book);
    }

    [HttpPut("update/{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateBookRequest request)
    {
        if (request.Price is null && request.QuantityChange is null)
        {
            return BadRequest(new
            {
                error = "At least one field must be provided."
            });
        }

        if (request.Price.HasValue && (!double.IsFinite(request.Price.Value) || request.Price.Value < 0))
        {
            return BadRequest(new
            {
                error = "Price must be a valid non-negative number."
            });
        }

        await using var connection =
            _database.CreateConnection();

        await connection.OpenAsync();

        var command = connection.CreateCommand();

        command.CommandText = """
            UPDATE Books
            SET
                Price = COALESCE($price, Price),
                Quantity = Quantity + COALESCE($quantityChange, 0)
            WHERE
                Id = $id
                AND Quantity + COALESCE($quantityChange, 0) >= 0;
            """;

        command.Parameters.AddWithValue(
            "$price",
            request.Price.HasValue
                ? request.Price.Value
                : DBNull.Value
        );

        command.Parameters.AddWithValue(
            "$quantityChange",
            request.QuantityChange.HasValue
                ? request.QuantityChange.Value
                : DBNull.Value
        );

        command.Parameters.AddWithValue("$id", id);

        var affectedRows = await command.ExecuteNonQueryAsync();
        if (affectedRows == 0)
        {
            var existsCommand = connection.CreateCommand();

            existsCommand.CommandText = """
                SELECT EXISTS(
                    SELECT 1 FROM Books WHERE Id = $id
                );
                """;

            existsCommand.Parameters.AddWithValue("$id", id);

            var exists = Convert.ToInt32(
                await existsCommand.ExecuteScalarAsync()
            );

            if (exists == 0)
            {
                return NotFound(new
                {
                    error = "Book not found."
                });
            }

            return Conflict(new
            {
                error = "Insufficient stock."
            });
        }

        return await GetInfo(id);
    }
}
