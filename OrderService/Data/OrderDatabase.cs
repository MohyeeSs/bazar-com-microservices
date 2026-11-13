
using Microsoft.Data.Sqlite;

namespace OrderService.Data;

public class OrderDatabase
{
    private readonly string _connectionString;

    public OrderDatabase(IWebHostEnvironment environment)
    {
        var directory = Path.Combine(
            environment.ContentRootPath, "Data");

        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, "orders.db");

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path
        }.ToString();
    }

    public async Task InitializeAsync()
    {
        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync();

        var command = connection.CreateCommand();

        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Orders (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                BookId INTEGER NOT NULL,
                Title TEXT NOT NULL,
                Price REAL NOT NULL,
                Status TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );
            """;

        await command.ExecuteNonQueryAsync();
    }

    public async Task<long> CreatePendingAsync(
        int bookId, string title, decimal price)
    {
        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync();

        var command = connection.CreateCommand();

        command.CommandText = """
            INSERT INTO Orders
                (BookId, Title, Price, Status, CreatedAt)
            VALUES
                ($bookId, $title, $price, 'Pending', $date);
            """;

        command.Parameters.AddWithValue("$bookId", bookId);
        command.Parameters.AddWithValue("$title", title);
        command.Parameters.AddWithValue("$price", (double)price);
        command.Parameters.AddWithValue(
            "$date", DateTime.UtcNow.ToString("O"));

        await command.ExecuteNonQueryAsync();

        command.CommandText = "SELECT last_insert_rowid();";
        command.Parameters.Clear();

        return Convert.ToInt64(
            await command.ExecuteScalarAsync());
    }

    public async Task SetStatusAsync(long orderId, string status)
    {
        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync();

        var command = connection.CreateCommand();

        command.CommandText = """
            UPDATE Orders
            SET Status = $status
            WHERE Id = $id;
            """;

        command.Parameters.AddWithValue("$status", status);
        command.Parameters.AddWithValue("$id", orderId);

        await command.ExecuteNonQueryAsync();
    }
}
