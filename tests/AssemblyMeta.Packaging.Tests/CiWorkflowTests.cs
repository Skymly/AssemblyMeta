namespace AssemblyMeta.Packaging.Tests;

public class CiWorkflowTests
{
    [Fact]
    public void Workflow_builds_tests_and_packs_without_publishing()
    {
        var root = RepoRoot.Find();
        var dir = Path.Combine(root, ".github", "workflows");
        Assert.True(Directory.Exists(dir));
        var files = Directory.GetFiles(dir, "*.yml")
            .Concat(Directory.GetFiles(dir, "*.yaml"))
            .ToArray();
        var path = Assert.Single(files);
        var text = File.ReadAllText(path).Replace("\r\n", "\n");

        Assert.Contains("pull_request:", text);
        Assert.Contains("push:", text);
        Assert.Contains("- main", text);
        Assert.Contains("contents: read", text);
        Assert.Contains("windows-latest", text);
        Assert.Contains("global-json-file: global.json", text);
        Assert.Contains("git switch -C", text);
        Assert.Contains("Remove-Item Env:GITHUB_REF", text);
        Assert.Contains("./eng/verify.ps1", text);

        Assert.DoesNotContain("workflow_dispatch", text);
        Assert.DoesNotContain("secrets.", text);
        Assert.DoesNotContain("nuget push", text);
        Assert.DoesNotContain("gh release", text);
        Assert.DoesNotContain("upload-artifact", text);
        Assert.DoesNotContain("tags:", text);
        Assert.DoesNotContain("release:", text);
        Assert.DoesNotContain("write", text);
    }
}
