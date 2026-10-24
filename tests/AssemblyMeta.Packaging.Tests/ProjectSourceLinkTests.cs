using System.Diagnostics;
using System.Text;

namespace AssemblyMeta.Packaging.Tests;

public class ProjectSourceLinkTests
{
    [Fact]
    public void PrepareProject_encodes_sourcelink_revision()
    {
        var root = RepoRoot.Find();
        var dir = Path.Combine(Path.GetTempPath(), "am-proj-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var buildTransitive = Path.Combine(dir, "pkg", "buildTransitive");
            var tasks = Path.Combine(dir, "pkg", "tasks", "netstandard2.0");
            Directory.CreateDirectory(buildTransitive);
            Directory.CreateDirectory(tasks);
            foreach (var dir_with_files in new[]
            {
                Path.Combine(root, "src", "AssemblyMeta.Project", "buildTransitive"),
                Path.Combine(root, "src", "build"),
            })
            {
                foreach (var file in Directory.GetFiles(dir_with_files))
                {
                    File.Copy(file, Path.Combine(buildTransitive, Path.GetFileName(file)));
                }
            }

            var tasksOut = Path.Combine(BuildTasks(root), "netstandard2.0");
            foreach (var dll in Directory.GetFiles(tasksOut, "*.dll"))
            {
                File.Copy(dll, Path.Combine(tasks, Path.GetFileName(dll)));
            }

            var projectDir = Path.Combine(dir, "proj");
            Directory.CreateDirectory(projectDir);
            Git(projectDir, "init");
            Git(projectDir, "-c", "user.email=am-tests@localhost", "-c", "user.name=am-tests", "commit", "--allow-empty", "-m", "init");
            var head = Git(projectDir, "rev-parse", "HEAD");

            var project = Path.Combine(projectDir, "App.csproj");
            File.WriteAllText(project, Project(buildTransitive));
            var result = Run(
                "dotnet",
                projectDir,
                "msbuild",
                project,
                "-nologo",
                "-v:minimal",
                "-p:ImportDirectoryBuildProps=false",
                "-p:ImportDirectoryBuildTargets=false",
                "/t:WriteProjectPayload");
            Assert.True(result.ExitCode == 0, result.Text);

            var revision = File.ReadAllText(Path.Combine(projectDir, "sourcerevision.txt")).Trim();
            Assert.Equal(head, revision);

            var payload = File.ReadAllText(Path.Combine(projectDir, "payload.b64")).Trim();
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            Assert.Contains("SourceRevisionId", decoded);
            Assert.Contains(head, decoded);
        }
        finally
        {
            DeleteTree(dir);
        }
    }

    private static void DeleteTree(string dir)
    {
        if (!Directory.Exists(dir))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        Directory.Delete(dir, true);
    }

    private static string BuildTasks(string root)
    {
        var project = Path.Combine(root, "src", "AssemblyMeta.Tasks", "AssemblyMeta.Tasks.csproj");
        var result = Run("dotnet", root, "build", project, "-c", "Release", "-nologo", "-v:minimal");
        Assert.True(result.ExitCode == 0, result.Text);
        var output = Path.Combine(root, "src", "AssemblyMeta.Tasks", "bin", "Release");
        Assert.True(Directory.Exists(output), output);
        return output;
    }

    private static string Git(string workingDirectory, params string[] args)
    {
        var result = Run("git", workingDirectory, args);
        Assert.True(result.ExitCode == 0, result.Text);
        return result.Text.Trim();
    }

    private static (int ExitCode, string Text) Run(string fileName, string workingDirectory, params string[] args)
    {
        var start = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var arg in args)
        {
            start.ArgumentList.Add(arg);
        }

        using var process = Process.Start(start) ?? throw new InvalidOperationException("无法启动 " + fileName + "。");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        return (process.ExitCode, stdout.GetAwaiter().GetResult() + Environment.NewLine + stderr.GetAwaiter().GetResult());
    }

    private static string Project(string buildTransitive)
    {
        var props = Path.Combine(buildTransitive, "AssemblyMeta.Project.props");
        var targets = Path.Combine(buildTransitive, "AssemblyMeta.Project.targets");
        return "<Project>\n"
            + "  <Import Project=\"Sdk.props\" Sdk=\"Microsoft.NET.Sdk\" />\n"
            + "  <Import Project=\"" + Xml(props) + "\" />\n"
            + "  <PropertyGroup>\n"
            + "    <TargetFramework>net10.0</TargetFramework>\n"
            + "    <EnableDefaultItems>false</EnableDefaultItems>\n"
            + "    <ImplicitUsings>disable</ImplicitUsings>\n"
            + "    <Nullable>disable</Nullable>\n"
            + "  </PropertyGroup>\n"
            + "  <ItemGroup>\n"
            + "    <ProjectProperty Include=\"SourceRevisionId\" />\n"
            + "  </ItemGroup>\n"
            + "  <Import Project=\"Sdk.targets\" Sdk=\"Microsoft.NET.Sdk\" />\n"
            + "  <Import Project=\"" + Xml(targets) + "\" />\n"
            + "  <Target Name=\"WriteProjectPayload\" DependsOnTargets=\"AssemblyMetaPrepareProject\">\n"
            + "    <WriteLinesToFile File=\"$(MSBuildProjectDirectory)/sourcerevision.txt\" Lines=\"$(SourceRevisionId)\" Overwrite=\"true\" />\n"
            + "    <WriteLinesToFile File=\"$(MSBuildProjectDirectory)/payload.b64\" Lines=\"$(AssemblyMeta_ProjectPayload)\" Overwrite=\"true\" />\n"
            + "  </Target>\n"
            + "</Project>\n";
    }

    private static string Xml(string value)
    {
        return value.Replace("&", "&amp;").Replace("\"", "&quot;");
    }
}
