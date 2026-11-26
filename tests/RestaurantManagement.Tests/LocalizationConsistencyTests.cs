using System.Text.Json;
using Xunit;

namespace RestaurantManagement.Tests;

public class LocalizationConsistencyTests
{
    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "RestaurantManagementSystem.sln")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate RestaurantManagementSystem.sln");
    }

    [Fact]
    public void AllKeysInEnJson_MustExistAndBeNonEmptyInMrJson_AndViceVersa()
    {
        var root = FindRepoRoot();
        var i18nDir = Path.Combine(root, "src", "RestaurantManagement.Web", "wwwroot", "i18n");
        var enPath = Path.Combine(i18nDir, "en.json");
        var mrPath = Path.Combine(i18nDir, "mr.json");

        Assert.True(File.Exists(enPath), $"Missing {enPath}");
        Assert.True(File.Exists(mrPath), $"Missing {mrPath}");

        var enDict = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(enPath)) ?? new();
        var mrDict = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(mrPath)) ?? new();

        var missingInMr = enDict.Keys.Where(k => !mrDict.TryGetValue(k, out var val) || string.IsNullOrWhiteSpace(val)).ToList();
        var missingInEn = mrDict.Keys.Where(k => !enDict.TryGetValue(k, out var val) || string.IsNullOrWhiteSpace(val)).ToList();

        Assert.True(missingInMr.Count == 0, $"Keys missing or empty in mr.json: {string.Join(", ", missingInMr)}");
        Assert.True(missingInEn.Count == 0, $"Keys missing or empty in en.json: {string.Join(", ", missingInEn)}");
    }
}
