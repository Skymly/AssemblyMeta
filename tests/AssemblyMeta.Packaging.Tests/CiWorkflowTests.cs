namespace AssemblyMeta.Packaging.Tests;

public class CiWorkflowTests
{
    [Fact]
    public void Verify_workflow_builds_tests_and_packs_without_publishing()
    {
        var text = Read("verify.yml");

        Assert.Contains("pull_request:", text);
        Assert.Contains("push:", text);
        Assert.Contains("- main", text);
        Assert.Contains("contents: read", text);
        Assert.Contains("windows-latest", text);
        Assert.Contains("ubuntu-latest", text);
        Assert.Contains("macos-latest", text);
        Assert.Contains("matrix.os", text);
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
        Assert.DoesNotContain("id-token:", text);
        Assert.DoesNotContain("NuGet/login", text);
    }

    [Fact]
    public void Release_workflow_publishes_from_a_maintainer_tag()
    {
        var text = Read("release.yml");

        Assert.Contains("tags:", text);
        Assert.Contains("- \"v*\"", text);
        Assert.Contains("id-token: write", text);
        Assert.Contains("NuGet/login@v1", text);
        Assert.Contains("user: Skym", text);
        Assert.Contains("github.triggering_actor", text);
        Assert.Contains("Skymly", text);
        Assert.Contains("wys0610", text);
        Assert.Contains("dotnet nuget push", text);
        Assert.Contains("https://api.nuget.org/v3/index.json", text);
        Assert.DoesNotContain("environment:", text);
        Assert.DoesNotContain("secrets.", text);
        Assert.DoesNotContain("workflow_dispatch", text);
    }

    private static string Read(string name)
    {
        var root = RepoRoot.Find();
        var path = Path.Combine(root, ".github", "workflows", name);
        Assert.True(File.Exists(path));
        return File.ReadAllText(path).Replace("\r\n", "\n");
    }
}
