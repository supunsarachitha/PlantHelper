using PlantHelper.Core.Data;

namespace PlantHelper.Tests;

/// <summary>A throw-away database + photo folder per test.</summary>
public sealed class TestDatabase : IAsyncDisposable
{
    public TestDatabase()
    {
        Root = Path.Combine(Path.GetTempPath(), "PlantHelperTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
        Database = new AppDatabase(Path.Combine(Root, "test.db3"));
        Photos = new PhotoStore(Path.Combine(Root, "photos"));
        Plants = new PlantRepository(Database);
        Logs = new CareLogRepository(Database);
        PhotoRows = new PhotoRepository(Database);
        Nutrients = new NutrientRepository(Database);
    }

    public string Root { get; }
    public AppDatabase Database { get; }
    public PhotoStore Photos { get; }
    public PlantRepository Plants { get; }
    public CareLogRepository Logs { get; }
    public PhotoRepository PhotoRows { get; }
    public NutrientRepository Nutrients { get; }

    public async ValueTask DisposeAsync()
    {
        await Database.CloseAsync();
        try { Directory.Delete(Root, recursive: true); } catch (IOException) { }
    }
}

/// <summary>A TimeProvider frozen at a fixed local time.</summary>
public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;
    public override DateTimeOffset GetUtcNow() => Now.ToUniversalTime();
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Local;
}
