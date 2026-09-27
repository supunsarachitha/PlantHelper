using System.IO.Compression;
using System.Text;
using PlantHelper.Core.Data;
using PlantHelper.Core.Models;
using PlantHelper.Core.Services;

namespace PlantHelper.Tests;

public class BackupTests
{
    private static BackupService Backup(TestDatabase t) => new(t.Database, t.Photos, Path.Combine(t.Root, "staging"));

    private static async Task<(Plant Plant, CareLog Log, PlantPhoto Photo, Nutrient Nutrient)> SeedAsync(TestDatabase t)
    {
        var plant = new Plant { Name = "Tomato", Variety = "Sweet 100", Stage = GrowthStage.Flowering, WaterEveryDays = 2, ContainerLiters = 11 };
        await t.Plants.SaveAsync(plant);
        var log = new CareLog { PlantId = plant.Id, Type = LogType.Feed, AmountMl = 1500, Ph = 6.1, Doses = [new NutrientDose { Name = "Bloom", MlPerLiter = 2 }] };
        await t.Logs.SaveAsync(log);
        using var bytes = new MemoryStream(Encoding.UTF8.GetBytes("fake-jpeg-bytes"));
        var file = await t.Photos.SaveAsync(bytes, ".jpg");
        var photo = new PlantPhoto { PlantId = plant.Id, FileName = file, HeightCm = 42, Caption = "Week 6" };
        await t.PhotoRows.SaveAsync(photo);
        plant.CoverPhotoId = photo.Id;
        await t.Plants.SaveAsync(plant);
        var nutrient = new Nutrient { Name = "Bloom", N = 1, P = 5, K = 4, DefaultMlPerLiter = 2 };
        await t.Nutrients.SaveAsync(nutrient);
        return (plant, log, photo, nutrient);
    }

    [Fact]
    public async Task Export_ThenReplaceImport_OnAnotherDevice_RestoresEverything()
    {
        await using var source = new TestDatabase();
        var seed = await SeedAsync(source);
        using var zip = new MemoryStream();
        var summary = await Backup(source).ExportAsync(zip, "1.0.0", new Dictionary<string, string> { ["unit_system"] = "1" });

        Assert.Equal((1, 1, 1, 1, 1), (summary.Plants, summary.Logs, summary.Photos, summary.Nutrients, summary.PhotoFiles));

        await using var target = new TestDatabase();
        zip.Position = 0;
        var preview = await Backup(target).ReadSummaryAsync(zip);
        Assert.Equal(1, preview.PhotoFiles);
        Assert.Equal("1.0.0", preview.AppVersion);

        zip.Position = 0;
        var result = await Backup(target).ImportAsync(zip, ImportMode.Replace);

        Assert.Equal(4, result.Added);
        Assert.Equal(1, result.PhotoFilesRestored);
        Assert.Equal("1", result.Settings!["unit_system"]);

        var plant = (await target.Plants.GetAsync(seed.Plant.Id))!;
        Assert.Equal("Sweet 100", plant.Variety);
        Assert.Equal(GrowthStage.Flowering, plant.Stage);
        Assert.Equal(seed.Photo.Id, plant.CoverPhotoId);
        var log = (await target.Logs.GetAsync(seed.Log.Id))!;
        Assert.Equal(1500, log.AmountMl);
        Assert.Equal("Bloom", Assert.Single(log.Doses).Name);
        var photo = (await target.PhotoRows.GetAsync(seed.Photo.Id))!;
        Assert.Equal(42, photo.HeightCm);
        Assert.Equal("fake-jpeg-bytes", await File.ReadAllTextAsync(target.Photos.GetPath(photo.FileName)));
        Assert.Equal("1-5-4", (await target.Nutrients.GetAsync(seed.Nutrient.Id))!.NpkText);
    }

    [Fact]
    public async Task Replace_RemovesLocalDataNotInBackup()
    {
        await using var source = new TestDatabase();
        await SeedAsync(source);
        using var zip = new MemoryStream();
        await Backup(source).ExportAsync(zip, null, null);

        await using var target = new TestDatabase();
        var local = new Plant { Name = "Local only" };
        await target.Plants.SaveAsync(local);
        using var localBytes = new MemoryStream([9]);
        var localFile = await target.Photos.SaveAsync(localBytes, ".jpg");

        zip.Position = 0;
        await Backup(target).ImportAsync(zip, ImportMode.Replace);

        Assert.Null(await target.Plants.GetAsync(local.Id));
        Assert.False(target.Photos.Exists(localFile));
        Assert.Single(await target.Plants.GetAllAsync());
    }

    [Fact]
    public async Task Merge_KeepsNewerSide_AddsMissing()
    {
        await using var device = new TestDatabase();
        var seed = await SeedAsync(device);
        using var zip = new MemoryStream();
        await Backup(device).ExportAsync(zip, null, null);

        // After the backup: rename locally (newer), add a local plant, delete the log.
        await Task.Delay(20);
        seed.Plant.Name = "Tomato (renamed)";
        await device.Plants.SaveAsync(seed.Plant);
        await device.Plants.SaveAsync(new Plant { Name = "Pepper" });
        await device.Logs.DeleteAsync(seed.Log.Id);

        zip.Position = 0;
        var result = await Backup(device).ImportAsync(zip, ImportMode.Merge);

        Assert.Equal("Tomato (renamed)", (await device.Plants.GetAsync(seed.Plant.Id))!.Name); // local edit is newer
        Assert.NotNull(await device.Logs.GetAsync(seed.Log.Id)); // deleted log came back
        Assert.Equal(2, (await device.Plants.GetAllAsync()).Count); // local plant kept
        Assert.Equal(1, result.Added);
        Assert.True(result.Unchanged >= 3);
    }

    [Fact]
    public async Task Merge_OlderLocalRecord_IsUpdatedFromBackup()
    {
        await using var device = new TestDatabase();
        var seed = await SeedAsync(device);
        await Task.Delay(20);
        seed.Plant.Notes = "edited later on another phone";
        await device.Plants.SaveAsync(seed.Plant);
        using var zip = new MemoryStream();
        await Backup(device).ExportAsync(zip, null, null);

        await using var other = new TestDatabase();
        var old = seed.Plant;
        old.Notes = null;
        old.UpdatedAt = DateTime.UtcNow.AddDays(-1);
        var db = await other.Database.GetConnectionAsync();
        await db.InsertAsync(old);

        zip.Position = 0;
        var result = await Backup(other).ImportAsync(zip, ImportMode.Merge);

        Assert.Equal("edited later on another phone", (await other.Plants.GetAsync(old.Id))!.Notes);
        Assert.Equal(1, result.Updated);
    }

    [Fact]
    public async Task NotAZip_Throws()
    {
        await using var t = new TestDatabase();
        using var junk = new MemoryStream(Encoding.UTF8.GetBytes("hello"));
        var ex = await Assert.ThrowsAsync<BackupException>(() => Backup(t).ReadSummaryAsync(junk));
        Assert.Contains("not a valid zip", ex.Message);
    }

    [Theory]
    [InlineData("{\"format\":\"Other.App\",\"version\":1}", "isn't a PlantHelper backup")]
    [InlineData("{\"format\":\"PlantHelper.Backup\",\"version\":99}", "newer version")]
    [InlineData("{not json", "damaged")]
    public async Task BadManifest_Throws(string manifest, string message)
    {
        await using var t = new TestDatabase();
        using var zip = MakeZip(("manifest.json", manifest), ("data.json", "{}"));
        var ex = await Assert.ThrowsAsync<BackupException>(() => Backup(t).ImportAsync(zip, ImportMode.Merge));
        Assert.Contains(message, ex.Message);
        Assert.Empty(await t.Plants.GetAllAsync());
    }

    [Fact]
    public async Task MissingData_Throws()
    {
        await using var t = new TestDatabase();
        using var zip = MakeZip(("manifest.json", "{\"format\":\"PlantHelper.Backup\",\"version\":1}"));
        await Assert.ThrowsAsync<BackupException>(() => Backup(t).ImportAsync(zip, ImportMode.Merge));
    }

    [Fact]
    public async Task MaliciousEntries_AreIgnored_AndOrphansSkipped()
    {
        await using var t = new TestDatabase();
        const string data = """
        {
          "plants": [ { "id": "p1", "name": "Basil" }, { "id": "", "name": "no id" } ],
          "logs": [ { "id": "l1", "plantId": "p1", "type": "Water" }, { "id": "l2", "plantId": "ghost", "type": "Water" } ],
          "photos": [
            { "id": "f1", "plantId": "p1", "fileName": "good.jpg" },
            { "id": "f2", "plantId": "p1", "fileName": "../../evil.jpg" }
          ]
        }
        """;
        using var zip = MakeZip(
            ("manifest.json", "{\"format\":\"PlantHelper.Backup\",\"version\":1}"),
            ("data.json", data),
            ("photos/good.jpg", "ok"),
            ("photos/../../evil.jpg", "bad"),
            ("photos/sub/good.jpg", "bad"));

        var result = await Backup(t).ImportAsync(zip, ImportMode.Merge);

        Assert.Equal(3, result.Skipped); // plant without id, orphan log, bad photo name
        Assert.Equal(1, result.PhotoFilesRestored);
        Assert.Equal("ok", await File.ReadAllTextAsync(t.Photos.GetPath("good.jpg")));
        Assert.False(File.Exists(Path.Combine(t.Root, "evil.jpg")));
        Assert.False(File.Exists(Path.Combine(t.Photos.PhotosDirectory, "..", "..", "evil.jpg")));
        Assert.Equal(["good.jpg"], Directory.GetFiles(t.Photos.PhotosDirectory).Select(Path.GetFileName));
        Assert.Empty(Directory.GetDirectories(Path.Combine(t.Root, "staging"))); // staging cleaned up
    }

    [Fact]
    public async Task OversizedPhoto_AbortsWithoutChanges()
    {
        await using var t = new TestDatabase();
        await t.Plants.SaveAsync(new Plant { Name = "Existing" });
        const string data = """{ "plants": [ { "id": "p1", "name": "Basil" } ], "photos": [ { "id": "f1", "plantId": "p1", "fileName": "big.jpg" } ] }""";
        using var zip = MakeZip(("manifest.json", "{\"format\":\"PlantHelper.Backup\",\"version\":1}"), ("data.json", data), ("photos/big.jpg", new string('x', 5000)));
        var backup = new BackupService(t.Database, t.Photos, Path.Combine(t.Root, "staging")) { MaxPhotoBytes = 1000 };

        await Assert.ThrowsAsync<BackupException>(() => backup.ImportAsync(zip, ImportMode.Replace));

        Assert.Equal(["Existing"], (await t.Plants.GetAllAsync()).Select(p => p.Name)); // nothing was wiped
    }

    private static MemoryStream MakeZip(params (string Name, string Content)[] entries)
    {
        var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                using var w = new StreamWriter(zip.CreateEntry(name).Open());
                w.Write(content);
            }
        }
        ms.Position = 0;
        return ms;
    }
}

public class CsvExporterTests
{
    [Fact]
    public async Task WritesHeaderAndRows_WithEscaping()
    {
        var plant = new Plant { Id = "p", Name = "Tomato, \"Big\"", Variety = "=HYPERLINK()" };
        var logs = new[]
        {
            new CareLog { PlantId = "p", Type = LogType.Water, Date = new DateTime(2026, 9, 2, 8, 5, 0), AmountMl = 500, Ph = 6.2, Notes = "line1\nline2" },
            new CareLog { PlantId = "p", Type = LogType.Measure, Date = new DateTime(2026, 9, 1), HeightCm = 12.5 },
        };
        using var writer = new StringWriter();

        await CsvExporter.WriteLogsAsync(writer, [plant], logs);
        var lines = writer.ToString().Split(Environment.NewLine);

        Assert.StartsWith("Date,Time,Plant,Variety,Type", lines[0]);
        Assert.StartsWith("2026-09-01,00:00,\"Tomato, \"\"Big\"\"\",'=HYPERLINK(),Measurement", lines[1]); // oldest first
        Assert.Contains(",12.5,", lines[1]);
        Assert.Contains("\"line1\nline2\"", writer.ToString());
        Assert.Contains(",500,6.2,", lines[2]);
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("plain", "plain")]
    [InlineData("-5", "'-5")]
    [InlineData("+cmd", "'+cmd")]
    [InlineData("@SUM(A1)", "'@SUM(A1)")]
    [InlineData("a,b", "\"a,b\"")]
    public void TextEscaping(string? input, string expected) => Assert.Equal(expected, CsvExporter.Text(input));
}
