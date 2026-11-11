using Microsoft.Data.Sqlite;

namespace CatalogService.Data;

public class CatalogDatabase
{
    private readonly string _connectionString;

    public CatalogDatabase(IWebHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath,"Data");

        Directory.CreateDirectory(dataDirectory);

        var databasePath = Path.Combine(dataDirectory,"catalog.db");

        _connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();

    }

    public async Task InitializeAsync()
    {
        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync();

        var createTableCommand = connection.CreateCommand();

        createTableCommand.CommandText = """
            CREATE TABLE IF NOT EXISTS Books (
                Id INTEGER PRIMARY KEY,
                Title TEXT NOT NULL,
                Topic TEXT NOT NULL,
                Quantity INTEGER NOT NULL CHECK (Quantity >= 0),
                Price REAL NOT NULL CHECK (Price >= 0)
            );
            """;

        await createTableCommand.ExecuteNonQueryAsync();

        var seedCommand = connection.CreateCommand();

        seedCommand.CommandText = """
            INSERT OR IGNORE INTO Books
                (Id, Title, Topic, Quantity, Price)
            VALUES
                (1, 'How to get a good grade in DOS in 40 minutes a day',
                    'distributed systems', 10, 40),

                (2, 'RPCs for Noobs',
                    'distributed systems', 5, 50),

                (3, 'Xen and the Art of Surviving Undergraduate School',
                    'undergraduate school', 10, 60),

                (4, 'Cooking for the Impatient Undergrad',
                    'undergraduate school', 10, 45);
            """;

        await seedCommand.ExecuteNonQueryAsync();
    }

    public SqliteConnection CreateConnection()
    {
        return new SqliteConnection(_connectionString);
    }
}
