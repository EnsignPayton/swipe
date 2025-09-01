using Dapper;
using Microsoft.Data.Sqlite;

namespace wowup;

[DapperAot]
public sealed class AddOnDatabase : IAsyncDisposable
{
    private const string CurrentGameKey = "current_game";

    private readonly SqliteConnection _connection = new($"Data Source={Paths.Db}");

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    public async Task InitializeAsync()
    {
        if (!Directory.Exists(Paths.Config))
            Directory.CreateDirectory(Paths.Config);

        await _connection.OpenAsync();
        await _connection.ExecuteAsync("PRAGMA journal_mode=WAL");
        await _connection.ExecuteAsync(
            """
            CREATE TABLE IF NOT EXISTS game
            (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL UNIQUE,
                path TEXT NOT NULL UNIQUE
            );

            CREATE TABLE IF NOT EXISTS addon
            (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                version TEXT NOT NULL,
                zipId INTEGER,
                zipName TEXT,
                zipHash TEXT,
                timestamp DATETIME DEFAULT current_timestamp,
                UNIQUE (name, version)
            );

            CREATE TABLE IF NOT EXISTS addon_component
            (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                addonId INTEGER NOT NULL,
                name TEXT NOT NULL,
                FOREIGN KEY (addonId) REFERENCES addon(id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS game_addon
            (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                gameId INTEGER NOT NULL,
                addonId INTEGER NOT NULL,
                FOREIGN KEY (gameId) REFERENCES game(id) ON DELETE CASCADE,
                FOREIGN KEY (addonId) REFERENCES addon(id) ON DELETE CASCADE,
                UNIQUE (gameId, addonId)
            );

            CREATE TABLE IF NOT EXISTS config
            (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                key TEXT NOT NULL UNIQUE,
                value TEXT
            )
            """);
    }

    public async Task<List<Game>> GetAllGames()
    {
        var result = await _connection.QueryAsync<Game>(
            "SELECT id, name, path FROM game");
        return result.ToList();
    }

    public async Task<Game?> GetGame(string name) =>
        await _connection.QueryFirstOrDefaultAsync<Game>(
            "SELECT id, name, path FROM game WHERE name = @name LIMIT 1", new { name });

    public async Task<Game?> GetCurrentGame() =>
        await _connection.QueryFirstOrDefaultAsync<Game>(
            """
            SELECT id, name, path
            FROM game
            WHERE name = (SELECT value FROM config WHERE key = @key LIMIT 1)
            LIMIT 1
            """, new { key = CurrentGameKey });

    public async Task AddGame(Game value)
    {
        await _connection.ExecuteAsync(
            """
            INSERT INTO game(name, path) VALUES(@name, @path)
            """, new { name = value.Name, path = value.Path });
    }

    public async Task SaveCurrentGame(string name)
    {
        await _connection.ExecuteAsync(
            """
            INSERT INTO config(key, value)
            VALUES(@key, @value)
            ON CONFLICT DO UPDATE SET value = @value
            """, new { key = CurrentGameKey, value = name });
    }

    public async Task DeleteGame(string name)
    {
        await _connection.ExecuteAsync(
            "DELETE FROM game WHERE name = @name", new { name });
    }

    public async Task<List<AddOn>> GetAddOns(int gameId, bool includeComponents)
    {
        var result = (await _connection.QueryAsync<AddOn>(
            """
            WITH game_addons AS (
                SELECT a.*
                FROM addon a
                JOIN game_addon ga ON ga.addonId = a.id
                WHERE ga.gameId = @gameId
            ),
            ranked_addons AS (
                SELECT *,
                       ROW_NUMBER() OVER (PARTITION BY name ORDER BY timestamp DESC) AS rn
                FROM game_addons
            )
            SELECT id, name, version, zipId, zipName, zipHash
            FROM ranked_addons
            WHERE rn = 1;
            """, new { gameId })).ToList();
        if (result.Count == 0) return result;

        if (includeComponents)
        {
            foreach (var addon in result)
            {
                var components = await _connection.QueryAsync<AddOnComponent>(
                    "SELECT id, addonId, name FROM addon_component WHERE addonId = @addonId", new { addonId = addon.Id });
                addon.Components = components.ToList();
            }
        }

        return result;
    }

    public async Task<AddOn?> GetAddOn(int gameId, string addonName)
    {
        var result = await _connection.QueryFirstOrDefaultAsync<AddOn>(
            """
            SELECT id, name, version, zipId, zipName, zipHash
            FROM addon
            WHERE name = @addonName AND EXISTS(
                SELECT 1
                FROM game_addon
                WHERE addonId = id AND gameId = @gameId)
            LIMIT 1
            """, new { gameId, addonName });
        if (result is null) return result;

        var components = await _connection.QueryAsync<AddOnComponent>(
            "SELECT id, addonId, name FROM addon_component WHERE id = @id", new { id = result.Id });
        result.Components = components.ToList();

        return result;
    }

    public async Task SaveAddOn(int gameId, AddOn value)
    {
        await using var tran = await _connection.BeginTransactionAsync();

        try
        {
            await _connection.ExecuteAsync(
                """
                INSERT INTO addon(name, version, zipId, zipName, zipHash)
                VALUES(@name, @version, @zipId, @zipName, @zipHash)
                ON CONFLICT (name, version) DO NOTHING
                """,
                new
                {
                    name = value.Name, version = value.Version,
                    zipId = value.ZipId, zipName = value.ZipName, zipHash = value.ZipHash
                });

            var addonId = await _connection.QueryFirstOrDefaultAsync<int>(
                "SELECT id FROM addon WHERE name = @name AND version = @version LIMIT 1",
                new { name = value.Name, version = value.Version });

            await _connection.ExecuteAsync(
                "DELETE FROM addon_component WHERE addonId = @addonId", new { addonId });

            if (value.Components.Count > 0)
            {
                await _connection.ExecuteAsync(
                    """
                    INSERT INTO addon_component(addonId, name)
                    VALUES(@addonId, @name)
                    """, value.Components.Select(x => new { addonId, name = x.Name }));
            }

            await _connection.ExecuteAsync(
                """
                INSERT INTO game_addon(gameId, addonId)
                VALUES(@gameId, @addonId)
                ON CONFLICT (gameId, addonId) DO NOTHING
                """, new { gameId, addonId });

            await tran.CommitAsync();
        }
        catch
        {
            await tran.RollbackAsync();
            throw;
        }
    }

    public async Task UnlinkAddOn(int gameId, int addonId)
    {
        await _connection.ExecuteAsync(
            "DELETE FROM game_addon WHERE gameId = @gameId AND addonId = @addonId",
            new { gameId, addonId });
    }
}

public class Game
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
}

public class AddOn
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public int ZipId { get; set; }
    public string ZipName { get; set; } = string.Empty;
    public string ZipHash { get; set; } = string.Empty;

    public List<AddOnComponent> Components { get; set; } = [];
}

public class AddOnComponent
{
    public int Id { get; set; }
    public int AddOnId { get; set; }
    public string Name { get; set; } = string.Empty;
}