using System.Text.Json;

namespace AutoInstaller;

public record Bookmark(string Title, string Url);

public static class BookmarkStore
{
    private static readonly string FilePath = Path.Combine(
        AppContext.BaseDirectory, "bookmarks.json");

    public static List<Bookmark> Load()
    {
        if (!File.Exists(FilePath)) return [];
        try
        {
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<List<Bookmark>>(json) ?? [];
        }
        catch { return []; }
    }

    public static void Save(List<Bookmark> bookmarks)
    {
        var json = JsonSerializer.Serialize(bookmarks,
            new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
    }
}
