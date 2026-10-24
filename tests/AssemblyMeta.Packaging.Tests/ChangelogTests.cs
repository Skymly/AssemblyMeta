namespace AssemblyMeta.Packaging.Tests;

public class ChangelogTests
{
    [Fact]
    public void Changelog_records_1_0_0_without_claiming_a_publish()
    {
        var root = RepoRoot.Find();
        var path = Path.Combine(root, "CHANGELOG.md");
        Assert.True(File.Exists(path));
        var text = File.ReadAllText(path).Replace("\r\n", "\n");
        Assert.Contains("## 1.0.0-preview", text);
        Assert.Contains("## 1.0.0", text);
        Assert.Contains("AssemblyMeta.AssemblyInfo", text);
        Assert.Contains("AssemblyMeta.Constants", text);
        Assert.Contains("AssemblyMeta.Project", text);
        Assert.Contains("AssemblyMeta.Metadata", text);
        Assert.Contains("AssemblyMeta.Git", text);
        Assert.Contains("AssemblyMeta.Resources", text);
        Assert.Contains("AssemblyMeta.Strings", text);
        Assert.Contains("已于 2026-10-23 发布", text);
        Assert.Contains("尚未推送", text);
        Assert.DoesNotContain("NuGet 上还没有这些包", text);
        Assert.DoesNotContain("nuget push", text);
    }
}
