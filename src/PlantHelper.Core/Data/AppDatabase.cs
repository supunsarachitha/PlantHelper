using PlantHelper.Core.Models;
using SQLite;

namespace PlantHelper.Core.Data;

/// <summary>Owns the single SQLite connection and creates the schema on first use.</summary>
public sealed class AppDatabase
{
    private readonly SQLiteAsyncConnection _connection;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public AppDatabase(string databasePath)
    {
        DatabasePath = databasePath;
        _connection = new SQLiteAsyncConnection(
            databasePath,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache | SQLiteOpenFlags.FullMutex,
            storeDateTimeAsTicks: true);
    }

    public string DatabasePath { get; }

    public async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (!_initialized)
            await InitializeAsync().ConfigureAwait(false);
        return _connection;
    }

    private async Task InitializeAsync()
    {
        await _initLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_initialized)
                return;
            await _connection.EnableWriteAheadLoggingAsync().ConfigureAwait(false);
            await _connection.CreateTablesAsync<Plant, CareLog, PlantPhoto, Nutrient>().ConfigureAwait(false);
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <summary>Removes every row from every table (used by "Delete all data" and backup replace).</summary>
    public async Task DeleteAllAsync()
    {
        var db = await GetConnectionAsync().ConfigureAwait(false);
        await db.RunInTransactionAsync(c =>
        {
            c.DeleteAll<CareLog>();
            c.DeleteAll<PlantPhoto>();
            c.DeleteAll<Plant>();
            c.DeleteAll<Nutrient>();
        }).ConfigureAwait(false);
    }

    public Task CloseAsync() => _connection.CloseAsync();
}
