using AssemblyMeta.Core;

namespace AssemblyMeta.Generator.Tests;

public class ContractSurfaceTests
{
    [Fact]
    public void Contract_surface_locks_named_shapes()
    {
        var info = SourceEmission.Emit(PayloadCodec.EncodeAssemblyInfo(new AssemblyInfoModel
        {
            Configuration = "Debug",
            Company = "Skymly",
            Title = "标题",
            Description = "",
            Product = "产品",
            Copyright = "",
            Version = "1.2.3.0",
            InformationalVersion = "1.2.3-beta.4",
            FileVersion = "1.2.3.4",
        }), SourceKind.AssemblyInfo, 1100);
        Assert.NotNull(info.Source);
        Assert.Contains("internal static partial class AssemblyMeta", info.Source);
        var infoClass = info.Source.IndexOf("public static partial class Info", StringComparison.Ordinal);
        Assert.True(infoClass > info.Source.IndexOf("internal static partial class AssemblyMeta", StringComparison.Ordinal));
        var infoBody = info.Source.Substring(infoClass);
        foreach (var name in new[]
        {
            "Configuration", "Company", "Title", "Description", "Product",
            "Copyright", "Version", "InformationalVersion", "FileVersion",
        })
        {
            Assert.Contains($"public const string {name} =", infoBody);
        }

        Assert.Contains("public const string Description = \"\";", infoBody);
        Assert.Contains("public const string Copyright = \"\";", infoBody);

        Assert.DoesNotContain("Trademark", info.Source);

        var constants = SourceEmission.Emit(PayloadCodec.EncodeConstants(null, MemberVisibility.Public, new[]
        {
            new ConstantRecord("class", "keyword", "string", ""),
            new ConstantRecord("a--b", "x", "string", ""),
            new ConstantRecord("1ab", "n", "string", ""),
            new ConstantRecord("Content.Docs.License", "Content/Docs/License.md", "string", ""),
            new ConstantRecord("Http.Timeout", "30", "int", ""),
        }), SourceKind.Constants, 1100);
        Assert.NotNull(constants.Source);
        Assert.Contains("public static partial class Constants", constants.Source);
        Assert.Contains("public static readonly string @class = \"keyword\";", constants.Source);
        Assert.Contains("public static readonly string a_b = \"x\";", constants.Source);
        Assert.Contains("public static readonly string _1ab = \"n\";", constants.Source);
        var httpClass = constants.Source.IndexOf("public static partial class Http", StringComparison.Ordinal);
        Assert.True(httpClass >= 0);
        Assert.Contains("public static readonly int Timeout = 30;", constants.Source.Substring(httpClass));
        var docsClass = constants.Source.IndexOf("public static partial class Docs", StringComparison.Ordinal);
        Assert.True(docsClass > constants.Source.IndexOf("public static partial class Content", StringComparison.Ordinal));
        Assert.Contains("public static readonly string License = \"Content/Docs/License.md\";", constants.Source.Substring(docsClass));

        var project = SourceEmission.Emit(PayloadCodec.EncodeProject(null, MemberVisibility.Internal, new[]
        {
            new ConstantRecord("TargetFrameworkIdentifier", ".NETCoreApp", "string", ""),
        }), SourceKind.Project, 1100);
        Assert.Contains("public static partial class Project", project.Source);
        Assert.Contains("public const string TargetFrameworkIdentifier = \".NETCoreApp\";", project.Source);

        var metadata = SourceEmission.Emit(PayloadCodec.EncodeMetadata(null, MemberVisibility.Internal, new[]
        {
            new ConstantRecord("Channel", "stable", "string", ""),
        }), SourceKind.Metadata, 1100);
        Assert.Contains("public static partial class Metadata", metadata.Source);
        Assert.Contains("public const string Channel = \"stable\";", metadata.Source);
        Assert.DoesNotContain("AssemblyMetadataAttribute", metadata.Source);

        var git = SourceEmission.Emit(PayloadCodec.EncodeGit(new GitModel
        {
            Commit = "abcdef",
            Sha = "abcdef",
            Root = "C:/repo/",
            Url = "",
            Branch = "main",
        }), SourceKind.Git, 1100);
        Assert.Contains("public static partial class Git", git.Source);
        foreach (var name in new[] { "Commit", "Sha", "Root", "Url", "Branch" })
        {
            Assert.Contains($"public const string {name} =", git.Source);
        }

        Assert.Contains("public const string Root = \"C:/repo/\";", git.Source);
        Assert.Contains("仓库根目录，使用正斜杠，非空时以斜杠结尾。", git.Source);
        Assert.Contains("public const string Url = \"\";", git.Source);
        Assert.Equal("abcdef", GeneratorTestHost.ExecuteConstant(git.Source!, "AssemblyMeta+Git", "Sha"));
        Assert.Contains("RepositorySha 已有值时原样采用。否则是 Commit 的前 9 位；不足 9 位时用全长。", git.Source);

        var pending = SourceEmission.Emit(PayloadCodec.EncodeGit(new GitModel
        {
            Commit = "[pending build]",
            Sha = "[pending build]",
            Root = "[pending build]",
            Url = "[pending build]",
            Branch = "[pending build]",
        }), SourceKind.Git, 1100);
        foreach (var name in new[] { "Commit", "Sha", "Root", "Url", "Branch" })
        {
            Assert.Contains($"public const string {name} = \"[pending build]\";", pending.Source);
        }

        var namedResources = MemberNames.Name(new MemberNameRequest
        {
            Area = MemberNameArea.Resources,
            Resources = new[]
            {
                new ResourceNameInput
                {
                    RelativeDir = @"Content\Docs\",
                    Filename = "License",
                    Extension = ".md",
                    ManifestName = "Demo.Content.Docs.License.md",
                },
                new ResourceNameInput { Name = "Resources.readme", ManifestName = "Demo.Resources.readme.txt" },
            },
        });
        var resourceRecords = new ResourceRecord[namedResources.Members.Count];
        for (var i = 0; i < resourceRecords.Length; i++)
        {
            var member = namedResources.Members[i];
            resourceRecords[i] = new ResourceRecord(member.Name, member.ManifestName ?? "", member.Comment);
        }

        var resources = SourceEmission.Emit(
            PayloadCodec.EncodeResources(null, MemberVisibility.Internal, resourceRecords, namedResources.GeneratorDiagnostics),
            SourceKind.Resources,
            1100);
        Assert.NotNull(resources.Source);
        Assert.Contains(resources.Diagnostics, diagnostic => diagnostic.Id == "AM0008");
        var resourceText = resources.Source.Replace("\r\n", "\n");
        Assert.Contains("public static partial class Resources\n", resourceText);
        Assert.Contains("public static partial class Resources_\n", resourceText);
        var resourceDocs = resourceText.IndexOf("public static partial class Docs", StringComparison.Ordinal);
        Assert.True(resourceDocs > resourceText.IndexOf("public static partial class Content", StringComparison.Ordinal));
        Assert.Contains("public static string Text\n", resourceText.Substring(resourceDocs));
        Assert.Contains("public static byte[] GetBytes()", resources.Source);
        Assert.Contains("public static System.IO.Stream GetStream()", resources.Source);
        Assert.Contains("Demo.Content.Docs.License.md", resources.Source);

        var strings = SourceEmission.Emit(PayloadCodec.EncodeStrings(null, MemberVisibility.Internal, new[]
        {
            new ConstantRecord("Shop.Closed", "Closed today {0}", "string", ""),
            new ConstantRecord("Errors.NotFound", "missing", "string", ""),
            new ConstantRecord("Hello", "Hello {who}", "string", ""),
        }), SourceKind.Strings, 1100);
        Assert.NotNull(strings.Source);
        var stringsClass = strings.Source.IndexOf("public static partial class Strings\n", StringComparison.Ordinal);
        Assert.True(stringsClass >= 0);
        var shopClass = strings.Source.IndexOf("public static partial class Shop", StringComparison.Ordinal);
        Assert.True(shopClass > stringsClass);
        Assert.True(shopClass >= 0);
        Assert.Contains("public const string Closed = \"Closed today {0}\";", strings.Source.Substring(shopClass));
        var errorsClass = strings.Source.IndexOf("public static partial class Errors", StringComparison.Ordinal);
        Assert.True(errorsClass > stringsClass);
        Assert.Contains("public const string NotFound = \"missing\";", strings.Source.Substring(errorsClass));
        Assert.Contains("public const string Hello = \"Hello {who}\";", strings.Source);
        Assert.DoesNotContain("Hello(", strings.Source);
        Assert.DoesNotContain("Closed(", strings.Source);
    }
}
