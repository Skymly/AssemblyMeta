using System.Diagnostics;
using System.Text;

namespace AssemblyMeta.Packaging.Tests;

public class NamespaceResolutionTests
{
    [Fact]
    public void Every_package_imports_namespace_resolution()
    {
        var root = RepoRoot.Find();
        foreach (var name in new[]
        {
            "AssemblyMeta.AssemblyInfo",
            "AssemblyMeta.Constants",
            "AssemblyMeta.Project",
            "AssemblyMeta.Metadata",
            "AssemblyMeta.Git",
            "AssemblyMeta.Resources",
            "AssemblyMeta.Strings",
        })
        {
            var dir = Path.Combine(root, "src", name, "buildTransitive");
            var props = File.ReadAllText(Path.Combine(dir, name + ".props"));
            var targets = File.ReadAllText(Path.Combine(dir, name + ".targets"));
            Assert.Contains("AssemblyMeta.Namespace.props", props, StringComparison.Ordinal);
            Assert.Contains("AssemblyMeta.Namespace.targets", targets, StringComparison.Ordinal);
            Assert.Contains("AssemblyMetaResolveNamespace", targets, StringComparison.Ordinal);
        }

        var packaging = File.ReadAllText(Path.Combine(root, "src", "AssemblyMeta.Packaging.targets"));
        Assert.Contains("build\\AssemblyMeta.Namespace.props", packaging, StringComparison.Ordinal);
        Assert.Contains("build\\AssemblyMeta.Namespace.targets", packaging, StringComparison.Ordinal);
    }

    [Fact]
    public void Unset_uses_root_namespace_from_the_project_body()
    {
        var ns = Resolve("Case.csproj", "<RootNamespace>Demo.App</RootNamespace>");
        Assert.Equal("Demo.App", ns);
    }

    [Fact]
    public void Unset_uses_sdk_root_namespace_when_the_project_does_not_set_one()
    {
        var ns = Resolve("UnsetDefault.csproj", "");
        Assert.Equal("UnsetDefault", ns);
    }

    [Fact]
    public void Prepare_for_build_resolves_before_later_targets_read_the_property()
    {
        var ns = Resolve("Case.csproj", "<RootNamespace>Demo.App</RootNamespace>", "WriteAfterPrepare");
        Assert.Equal("Demo.App", ns);
    }

    [Fact]
    public void Explicit_empty_stays_global()
    {
        var ns = Resolve("Case.csproj", "<RootNamespace>Demo.App</RootNamespace><AssemblyMetaNamespace></AssemblyMetaNamespace>");
        Assert.Equal("", ns);
    }

    [Fact]
    public void Whitespace_is_explicit_blank()
    {
        var ns = Resolve("Case.csproj", "<RootNamespace>Demo.App</RootNamespace><AssemblyMetaNamespace>   </AssemblyMetaNamespace>");
        Assert.Equal("", ns);
    }

    [Fact]
    public void Explicit_value_wins_over_root_namespace()
    {
        var ns = Resolve("Case.csproj", "<RootNamespace>Demo.App</RootNamespace><AssemblyMetaNamespace>  Demo.Other  </AssemblyMetaNamespace>");
        Assert.Equal("Demo.Other", ns);
    }

    [Fact]
    public void Command_line_empty_stays_global()
    {
        var ns = Resolve(
            "Case.csproj",
            "<RootNamespace>Demo.App</RootNamespace>",
            extraArgs: new[] { "-p:AssemblyMetaNamespace=" });
        Assert.Equal("", ns);
    }

    [Fact]
    public void Command_line_value_wins()
    {
        var ns = Resolve(
            "Case.csproj",
            "<RootNamespace>Demo.App</RootNamespace>",
            extraArgs: new[] { "-p:AssemblyMetaNamespace=Cmd.Ns" });
        Assert.Equal("Cmd.Ns", ns);
    }

    [Fact]
    public void Directory_build_targets_empty_stays_global()
    {
        var ns = Resolve(
            "Case.csproj",
            "<RootNamespace>Demo.App</RootNamespace>",
            directoryBuildTargets: "<Project><PropertyGroup><AssemblyMetaNamespace></AssemblyMetaNamespace></PropertyGroup></Project>");
        Assert.Equal("", ns);
    }

    [Fact]
    public void Directory_build_targets_value_wins()
    {
        var ns = Resolve(
            "Case.csproj",
            "<RootNamespace>Demo.App</RootNamespace>",
            directoryBuildTargets: "<Project><PropertyGroup><AssemblyMetaNamespace>From.Targets</AssemblyMetaNamespace></PropertyGroup></Project>");
        Assert.Equal("From.Targets", ns);
    }

    [Fact]
    public void Directory_build_props_empty_is_treated_as_unset()
    {
        var ns = Resolve(
            "Case.csproj",
            "<RootNamespace>Demo.App</RootNamespace>",
            directoryBuildProps: "<Project><PropertyGroup><AssemblyMetaNamespace></AssemblyMetaNamespace></PropertyGroup></Project>");
        Assert.Equal("Demo.App", ns);
    }

    private static string Resolve(
        string projectFileName,
        string innerProperties,
        string target = "WriteNamespace",
        string? directoryBuildProps = null,
        string? directoryBuildTargets = null,
        IReadOnlyList<string>? extraArgs = null)
    {
        var root = RepoRoot.Find();
        var props = Path.Combine(root, "src", "build", "AssemblyMeta.Namespace.props");
        var targets = Path.Combine(root, "src", "build", "AssemblyMeta.Namespace.targets");
        var dir = Path.Combine(Path.GetTempPath(), "am-ns-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            if (directoryBuildProps is not null)
            {
                File.WriteAllText(Path.Combine(dir, "Directory.Build.props"), directoryBuildProps);
            }

            if (directoryBuildTargets is not null)
            {
                File.WriteAllText(Path.Combine(dir, "Directory.Build.targets"), directoryBuildTargets);
            }

            var project = Path.Combine(dir, projectFileName);
            File.WriteAllText(project, Project(props, targets, innerProperties));
            var args = new List<string>
            {
                "msbuild",
                project,
                "-nologo",
                "-v:minimal",
                "/t:" + target,
            };
            if (directoryBuildProps is null)
            {
                args.Add("-p:ImportDirectoryBuildProps=false");
            }

            if (directoryBuildTargets is null)
            {
                args.Add("-p:ImportDirectoryBuildTargets=false");
            }

            if (extraArgs is not null)
            {
                args.AddRange(extraArgs);
            }

            var start = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = dir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            foreach (var arg in args)
            {
                start.ArgumentList.Add(arg);
            }

            using var process = Process.Start(start) ?? throw new InvalidOperationException("无法启动 dotnet。");
            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();
            var resolvedPath = Path.Combine(dir, "resolved.txt");
            var resolved = File.Exists(resolvedPath) ? ReadResolved(resolvedPath) : "<missing>";
            Assert.True(process.ExitCode == 0, stdout + Environment.NewLine + stderr + Environment.NewLine + resolved);
            Assert.DoesNotContain("MSB4011", stdout + stderr, StringComparison.Ordinal);
            return resolved;
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }

    private static string Project(string props, string targets, string innerProperties)
    {
        return "<Project>\n"
            + "  <Import Project=\"Sdk.props\" Sdk=\"Microsoft.NET.Sdk\" />\n"
            + "  <Import Project=\"" + Xml(props) + "\" Condition=\"'$(_AssemblyMetaNamespacePropsImported)' != 'true'\" />\n"
            + "  <Import Project=\"" + Xml(props) + "\" Condition=\"'$(_AssemblyMetaNamespacePropsImported)' != 'true'\" />\n"
            + "  <PropertyGroup>\n"
            + "    <TargetFramework>net10.0</TargetFramework>\n"
            + "    <EnableDefaultItems>false</EnableDefaultItems>\n"
            + "    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>\n"
            + "    <DisableImplicitFrameworkDefines>true</DisableImplicitFrameworkDefines>\n"
            + "    <ImplicitUsings>disable</ImplicitUsings>\n"
            + "    <Nullable>disable</Nullable>\n"
            + "    " + innerProperties + "\n"
            + "  </PropertyGroup>\n"
            + "  <Import Project=\"Sdk.targets\" Sdk=\"Microsoft.NET.Sdk\" />\n"
            + "  <Import Project=\"" + Xml(targets) + "\" Condition=\"'$(_AssemblyMetaNamespaceTargetsImported)' != 'true'\" />\n"
            + "  <Import Project=\"" + Xml(targets) + "\" Condition=\"'$(_AssemblyMetaNamespaceTargetsImported)' != 'true'\" />\n"
            + "  <Target Name=\"WriteNamespace\" DependsOnTargets=\"AssemblyMetaResolveNamespace\">\n"
            + "    <WriteLinesToFile File=\"$(MSBuildProjectDirectory)\\resolved.txt\" Lines=\"NS=$(AssemblyMetaNamespace)END\" Overwrite=\"true\" />\n"
            + "  </Target>\n"
            + "  <Target Name=\"WriteAfterPrepare\" DependsOnTargets=\"PrepareForBuild\">\n"
            + "    <WriteLinesToFile File=\"$(MSBuildProjectDirectory)\\resolved.txt\" Lines=\"NS=$(AssemblyMetaNamespace)END\" Overwrite=\"true\" />\n"
            + "  </Target>\n"
            + "</Project>\n";
    }

    private static string ReadResolved(string path)
    {
        var bytes = File.ReadAllBytes(path);
        string text;
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            text = Encoding.Unicode.GetString(bytes);
        }
        else if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            text = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        }
        else
        {
            text = Encoding.UTF8.GetString(bytes);
        }

        const string prefix = "NS=";
        const string suffix = "END";
        var start = text.IndexOf(prefix, StringComparison.Ordinal);
        var end = text.LastIndexOf(suffix, StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start, text);
        return text.Substring(start + prefix.Length, end - (start + prefix.Length)).Trim();
    }

    private static string Xml(string value)
    {
        return value.Replace("&", "&amp;").Replace("\"", "&quot;");
    }
}
