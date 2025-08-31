using Dapper;
using Microsoft.Data.Sqlite;

namespace wowup;

public sealed class AddOnDatabase : IAsyncDisposable
{
    private static readonly string Home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    private static readonly string ConfigPath = Path.Combine(Home, ".config", "wowup");
    private readonly SqliteConnection _connection = new(new SqliteConnectionStringBuilder
    {
        DataSource = Path.Combine(ConfigPath, "addons.db")
    }.ConnectionString);

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        await _connection.ExecuteAsync("PRAGMA journal_mode=WAL");
        await _connection.ExecuteAsync(
            """
            CREATE TABLE IF NOT EXISTS installation
            (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name VARCHAR,
                version VARCHAR,
                timestamp DATETIME DEFAULT current_timestamp,
                zip_id INTEGER,
                zip_name VARCHAR,
                zip_hash VARCHAR,
                UNIQUE (name, version)
            );

            CREATE TABLE IF NOT EXISTS installation_content
            (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                installation_id INTEGER,
                dir_name VARCHAR,
                FOREIGN KEY (installation_id) REFERENCES installation(id)
            );
            """);
    }

    public async Task<Installation?> GetInstallation(AddOnKey key)
    {
        var result = await _connection.QueryFirstOrDefaultAsync<Installation>(
            """
            SELECT id as Id, name as Name, version as Version, zip_id as ZipId, zip_name as ZipName, zip_hash as ZipHash
            FROM installation
            WHERE name = @name AND version = @version
            LIMIT 1
            """, new { name = key.Name, version = key.Version });

        if (result is null) return null;

        var contents = await _connection.QueryAsync<InstallationContent>(
            """
            SELECT dir_name as DirName
            FROM installation_content
            WHERE installation_id = @id
            """, new { id = result.Id });

        result.Content = contents.ToList();
        return result;
    }

    public async Task<Installation?> GetLatestInstallation(string name)
    {
        var result = await _connection.QueryFirstOrDefaultAsync<Installation>(
            """
            SELECT id as Id, name as Name, version as Version, zip_id as ZipId, zip_name as ZipName, zip_hash as ZipHash
            FROM installation
            WHERE name = @name
            ORDER BY timestamp DESC
            LIMIT 1
            """, new { name });

        if (result is null) return null;

        var contents = await _connection.QueryAsync<InstallationContent>(
            """
            SELECT dir_name as DirName
            FROM installation_content
            WHERE installation_id = @id
            """, new { id = result.Id });

        result.Content = contents.ToList();
        return result;
    }

    public async Task<List<Installation>> GetLatestInstallations()
    {
        var result = (await _connection.QueryAsync<Installation>(
            """
            SELECT id as Id, name as Name, version as Version, zip_id as ZipId, zip_name as ZipName, zip_hash as ZipHash
            FROM (
                SELECT *, ROW_NUMBER() OVER (PARTITION BY name ORDER BY timestamp DESC) as row_num
                FROM installation 
            )
            WHERE row_num = 1
            """)).ToList();

        foreach (var item in result)
        {
            var contents = await _connection.QueryAsync<InstallationContent>(
                """
                SELECT dir_name as DirName
                FROM installation_content
                WHERE installation_id = @id
                """, new { id = item.Id });
            item.Content = contents.ToList();
        }

        return result;
    }

    public async Task SaveInstallation(Installation value)
    {
        await using var tran = await _connection.BeginTransactionAsync();

        try
        {
            await _connection.ExecuteAsync(
                """
                INSERT INTO installation(name, version, zip_id, zip_name, zip_hash)
                VALUES(@name, @version, @zip_id, @zip_name, @zip_hash)
                ON CONFLICT DO UPDATE SET zip_id = @zip_id, zip_name = @zip_name, zip_hash = @zip_hash
                """,
                new
                {
                    name = value.Name, version = value.Version,
                    zip_id = value.ZipId, zip_name = value.ZipName, zip_hash = value.ZipHash
                });

            var id = await _connection.QueryFirstOrDefaultAsync<int>(
                """
                SELECT id
                FROM installation
                WHERE name = @name AND version = @version
                """, new { name = value.Name, version = value.Version });

            await _connection.ExecuteAsync(
                """
                DELETE FROM installation_content
                WHERE installation_id = @id
                """, new { id });

            if (value.Content.Count > 0)
            {
                await _connection.ExecuteAsync(
                    """
                    INSERT INTO installation_content(installation_id, dir_name)
                    VALUES(@id, @dir_name)
                    """, value.Content.Select(x => new { id, dir_name = x.DirName }));
            }

            await tran.CommitAsync();
        }
        catch
        {
            await tran.RollbackAsync();
            throw;
        }
    }
}

public record AddOnKey(string Name, string Version);

public class Installation
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public int ZipId { get; set; }
    public string ZipName { get; set; } = string.Empty;
    public string ZipHash { get; set; } = string.Empty;
    public List<InstallationContent> Content { get; set; } = [];
}

public class InstallationContent
{
    public string DirName { get; set; } = string.Empty;
}