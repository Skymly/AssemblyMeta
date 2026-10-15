namespace AssemblyMeta.Packaging.Tests;

public class ChangelogTests
{
    [Fact]
    public void Changelog_records_1_0_0_without_claiming_a_publish()
    {
        var root = FindRepoRoot();
        var path = Path.Combine(root, "CHANGELOG.md");
        Assert.True(File.Exists(path));
        var text = File.ReadAllText(path).Replace("\r\n", "\n");
        Assert.Contains("## 1.0.0", text);
        Assert.Contains("AssemblyMeta.AssemblyInfo", text);
        Assert.Contains("AssemblyMeta.Constants", text);
        Assert.Contains("AssemblyMeta.Project", text);
        Assert.Contains("AssemblyMeta.Metadata", text);
        Assert.Contains("AssemblyMeta.Git", text);
        Assert.Contains("AssemblyMeta.Resources", text);
        Assert.Contains("AssemblyMeta.Strings", text);
        Assert.DoesNotContain("nuget push", text);
        Assert.DoesNotContain("已发布", text);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AssemblyMeta.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("找不到 AssemblyMeta.slnx。");
    }
}
