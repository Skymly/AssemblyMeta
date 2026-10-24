using System.Diagnostics;
using System.IO.Compression;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Xml.Linq;

namespace AssemblyMeta.Packaging.Tests;

public class PackageLayoutTests
{
    private static readonly string[] SubpackageIds =
    {
        "AssemblyMeta.AssemblyInfo",
        "AssemblyMeta.Constants",
        "AssemblyMeta.Project",
        "AssemblyMeta.Metadata",
        "AssemblyMeta.Git",
        "AssemblyMeta.Resources",
        "AssemblyMeta.Strings",
    };

    private static readonly Guid SourceLinkKind = new("CC110556-A091-4D38-9FEC-25AB9A351A6A");

    [Fact]
    public void Eight_packages_have_the_recorded_layout()
    {
        var root = RepoRoot.Find();
        var packageVersion = ReadPackageVersion(root);
        var fileVersion = packageVersion.Split('-')[0] + ".0";
        var output = Path.Combine(Path.GetTempPath(), "am-pack-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        try
        {
            var packed = Pack(root, output);
            Assert.Equal(0, packed.ExitCode);
            Assert.DoesNotMatch(@"(?i)warning [A-Z]{2}\d+", packed.Text);
            Assert.DoesNotMatch(@"[1-9]\d* 个警告", packed.Text);
            Assert.DoesNotMatch(@"[1-9]\d* Warning", packed.Text);
            Assert.Empty(Directory.GetFiles(output, "*.snupkg"));

            var packages = Directory.GetFiles(output, "*.nupkg");
            Assert.Equal(8, packages.Length);
            var byId = new Dictionary<string, Package>(StringComparer.Ordinal);
            foreach (var path in packages)
            {
                var package = Package.Read(path);
                Assert.Equal(packageVersion, package.Version);
                Assert.EndsWith(package.Id + "." + package.Version + ".nupkg", Path.GetFileName(path), StringComparison.Ordinal);
                byId.Add(package.Id, package);
            }

            var meta = byId["AssemblyMeta"];
            Assert.Contains(meta.Entries, entry => entry.StartsWith("lib/netstandard2.0/", StringComparison.Ordinal));
            Assert.DoesNotContain(meta.Entries, entry => entry.StartsWith("analyzers/", StringComparison.Ordinal));
            Assert.DoesNotContain(meta.Entries, entry => entry.StartsWith("tasks/", StringComparison.Ordinal));
            Assert.DoesNotContain(meta.Entries, entry => entry.StartsWith("buildTransitive/", StringComparison.Ordinal));
            Assert.Equal(SubpackageIds.OrderBy(id => id, StringComparer.Ordinal), meta.Dependencies.OrderBy(id => id, StringComparer.Ordinal));

            foreach (var id in SubpackageIds)
            {
                var package = byId[id];
                Assert.Contains(package.Entries, entry => entry.StartsWith("analyzers/dotnet/roslyn4.11/cs/", StringComparison.Ordinal));
                Assert.DoesNotContain(package.Entries, entry => entry.StartsWith("analyzers/dotnet/cs/", StringComparison.Ordinal));
                Assert.Contains(package.Entries, entry => entry.StartsWith("tasks/netstandard2.0/", StringComparison.Ordinal));
                Assert.Contains(package.Entries, entry => entry.StartsWith("buildTransitive/", StringComparison.Ordinal));
                Assert.Contains("buildTransitive/AssemblyMeta.Namespace.props", package.Entries, StringComparer.Ordinal);
                Assert.Contains("buildTransitive/AssemblyMeta.Namespace.targets", package.Entries, StringComparer.Ordinal);

                var dlls = package.Entries.Where(entry => entry.EndsWith(".dll", StringComparison.Ordinal)).ToArray();
                Assert.NotEmpty(dlls);
                using var zip = ZipFile.OpenRead(package.Path);
                foreach (var dll in dlls)
                {
                    var extract = Path.Combine(output, package.Id + "." + dll.Replace('/', '_'));
                    zip.GetEntry(dll)!.ExtractToFile(extract, overwrite: true);
                    AssertVersionAndSymbols(extract, packageVersion, fileVersion);
                }
            }

            foreach (var package in byId.Values)
            {
                Assert.False(string.IsNullOrWhiteSpace(package.Description));
                Assert.False(string.IsNullOrWhiteSpace(package.Authors));
                Assert.Equal("MIT", package.License);
                Assert.Equal("README.md", package.Readme);
                Assert.Contains("README.md", package.Entries, StringComparer.Ordinal);
                Assert.Contains("LICENSE", package.Entries, StringComparer.Ordinal);
                Assert.Contains("github.com/Skymly/AssemblyMeta", package.RepositoryUrl, StringComparison.OrdinalIgnoreCase);
                Assert.False(string.IsNullOrWhiteSpace(package.Tags));
                Assert.True(string.IsNullOrEmpty(package.Icon));
            }
        }
        finally
        {
            if (Directory.Exists(output))
            {
                Directory.Delete(output, true);
            }
        }
    }

    private static string ReadPackageVersion(string root)
    {
        var doc = XDocument.Load(Path.Combine(root, "Directory.Build.props"));
        var version = doc.Root!.Elements("PropertyGroup").Elements("AssemblyMetaPackageVersion").Single().Value;
        Assert.False(string.IsNullOrWhiteSpace(version));
        return version;
    }

    private static void AssertVersionAndSymbols(string dllPath, string packageVersion, string fileVersion)
    {
        var info = FileVersionInfo.GetVersionInfo(dllPath);
        Assert.Equal(fileVersion, info.FileVersion);
        Assert.True(
            info.ProductVersion?.StartsWith(packageVersion, StringComparison.Ordinal) == true,
            dllPath + " ProductVersion=" + info.ProductVersion);

        using var stream = File.OpenRead(dllPath);
        using var pe = new PEReader(stream);
        Assert.Equal(fileVersion, pe.GetMetadataReader().GetAssemblyDefinition().Version.ToString());

        var embedded = Assert.Single(pe.ReadDebugDirectory().Where(entry => entry.Type == DebugDirectoryEntryType.EmbeddedPortablePdb));
        using var pdb = pe.ReadEmbeddedPortablePdbDebugDirectoryData(embedded);
        var pdbReader = pdb.GetMetadataReader();
        var sourceLink = pdbReader.CustomDebugInformation
            .Select(handle => pdbReader.GetCustomDebugInformation(handle))
            .Where(information => pdbReader.GetGuid(information.Kind) == SourceLinkKind)
            .Select(information => pdbReader.GetBlobBytes(information.Value))
            .ToArray();
        var json = Assert.Single(sourceLink);
        Assert.Contains("\"documents\"", Encoding.UTF8.GetString(json));
    }

    private static (int ExitCode, string Text) Pack(string root, string output)
    {
        var start = new ProcessStartInfo("dotnet", "pack \"" + Path.Combine(root, "AssemblyMeta.slnx") + "\" -c Release -o \"" + output + "\"")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var process = Process.Start(start) ?? throw new InvalidOperationException("无法启动 dotnet pack。");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        return (process.ExitCode, stdout.GetAwaiter().GetResult() + Environment.NewLine + stderr.GetAwaiter().GetResult());
    }

    private sealed class Package
    {
        public required string Path { get; init; }
        public required string Id { get; init; }
        public required string Version { get; init; }
        public required string Description { get; init; }
        public required string Authors { get; init; }
        public required string License { get; init; }
        public required string Readme { get; init; }
        public required string RepositoryUrl { get; init; }
        public required string Tags { get; init; }
        public required string Icon { get; init; }
        public required IReadOnlyList<string> Entries { get; init; }
        public required IReadOnlyList<string> Dependencies { get; init; }

        public static Package Read(string path)
        {
            using var zip = ZipFile.OpenRead(path);
            var entries = zip.Entries.Select(entry => entry.FullName.Replace('\\', '/')).ToArray();
            var nuspec = zip.Entries.Single(entry => entry.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));
            using var stream = nuspec.Open();
            var doc = XDocument.Load(stream);
            var ns = doc.Root!.Name.Namespace;
            var metadata = doc.Root.Element(ns + "metadata")!;
            var repository = metadata.Element(ns + "repository");
            var dependencies = metadata
                .Element(ns + "dependencies")?
                .Elements()
                .SelectMany(group => group.Name.LocalName == "dependency" ? new[] { group } : group.Elements(ns + "dependency"))
                .Select(item => (string?)item.Attribute("id") ?? "")
                .Where(id => id.Length > 0)
                .ToArray() ?? Array.Empty<string>();
            return new Package
            {
                Path = path,
                Id = Value(metadata, ns, "id"),
                Version = Value(metadata, ns, "version"),
                Description = Value(metadata, ns, "description"),
                Authors = Value(metadata, ns, "authors"),
                License = Value(metadata, ns, "license"),
                Readme = Value(metadata, ns, "readme"),
                RepositoryUrl = (string?)repository?.Attribute("url") ?? "",
                Tags = Value(metadata, ns, "tags"),
                Icon = Value(metadata, ns, "icon"),
                Entries = entries,
                Dependencies = dependencies,
            };
        }

        private static string Value(XElement metadata, XNamespace ns, string name)
        {
            return metadata.Element(ns + name)?.Value ?? "";
        }
    }
}
