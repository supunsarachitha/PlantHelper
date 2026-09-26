using PlantHelper.Core.Data;

namespace PlantHelper.Tests;

public class PhotoStoreTests
{
    [Theory]
    [InlineData("abc123.jpg", true)]
    [InlineData("a_b-c.PNG", true)]
    [InlineData("../evil.jpg", false)]
    [InlineData("dir/a.jpg", false)]
    [InlineData("..\\a.jpg", false)]
    [InlineData("a.exe", false)]
    [InlineData(".jpg", false)]
    [InlineData("", false)]
    [InlineData("a b.jpg", false)]
    public void ValidFileNames(string name, bool expected) =>
        Assert.Equal(expected, PhotoStore.IsValidFileName(name));

    [Fact]
    public async Task Save_Delete_Roundtrip()
    {
        await using var t = new TestDatabase();
        using var data = new MemoryStream([1, 2, 3, 4]);

        var name = await t.Photos.SaveAsync(data, "JPG");

        Assert.EndsWith(".jpg", name);
        Assert.True(t.Photos.Exists(name));
        Assert.Equal(4, new FileInfo(t.Photos.GetPath(name)).Length);

        t.Photos.Delete(name);
        Assert.False(t.Photos.Exists(name));
        t.Photos.Delete("../../etc/passwd"); // ignored, no throw
        Assert.Throws<ArgumentException>(() => t.Photos.GetPath("../x.jpg"));
    }

    [Fact]
    public async Task UnknownExtension_FallsBackToJpg()
    {
        await using var t = new TestDatabase();
        using var data = new MemoryStream([1]);
        Assert.EndsWith(".jpg", await t.Photos.SaveAsync(data, ".bmp"));
    }
}
