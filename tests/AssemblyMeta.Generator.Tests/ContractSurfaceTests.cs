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

        Assert.DoesNotContain("Trademark", info.Source);

        var constants = SourceEmission.Emit(PayloadCodec.EncodeConstants(null, MemberVisibility.Public, new[]
        {
            new ConstantRecord("class", "keyword", "string", ""),
            new ConstantRecord("Http.Timeout", "30", "int", ""),
        }), SourceKind.Constants, 1100);
        Assert.NotNull(constants.Source);
        Assert.Contains("public static readonly string @class = \"keyword\";", constants.Source);
        var httpClass = constants.Source.IndexOf("public static partial class Http", StringComparison.Ordinal);
        Assert.True(httpClass >= 0);
        Assert.Contains("public static readonly int Timeout = 30;", constants.Source.Substring(httpClass));

        var project = SourceEmission.Emit(PayloadCodec.EncodeProject(null, MemberVisibility.Internal, new[]
        {
            new ConstantRecord("TargetFrameworkIdentifier", ".NETCoreApp", "string", ""),
        }), SourceKind.Project, 1100);
        Assert.Contains("public const string TargetFrameworkIdentifier = \".NETCoreApp\";", project.Source);

        var metadata = SourceEmission.Emit(PayloadCodec.EncodeMetadata(null, MemberVisibility.Internal, new[]
        {
            new ConstantRecord("Channel", "stable", "string", ""),
        }), SourceKind.Metadata, 1100);
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
        foreach (var name in new[] { "Commit", "Sha", "Root", "Url", "Branch" })
        {
            Assert.Contains($"public const string {name} =", git.Source);
        }

        Assert.Equal("abcdef", GeneratorTestHost.ExecuteConstant(git.Source!, "AssemblyMeta+Git", "Sha"));

        var resources = SourceEmission.Emit(PayloadCodec.EncodeResources(null, MemberVisibility.Internal, new[]
        {
            new ResourceRecord("Content.Docs.License", "Demo.Content.Docs.License.md", ""),
        }), SourceKind.Resources, 1100);
        Assert.Contains("public static string Text", resources.Source);
        Assert.Contains("public static byte[] GetBytes()", resources.Source);
        Assert.Contains("public static System.IO.Stream GetStream()", resources.Source);
        Assert.Contains("Demo.Content.Docs.License.md", resources.Source);

        var strings = SourceEmission.Emit(PayloadCodec.EncodeStrings(null, MemberVisibility.Internal, new[]
        {
            new ConstantRecord("Shop.Closed", "Closed today {0}", "string", ""),
        }), SourceKind.Strings, 1100);
        Assert.NotNull(strings.Source);
        var shopClass = strings.Source.IndexOf("public static partial class Shop", StringComparison.Ordinal);
        Assert.True(shopClass >= 0);
        Assert.Contains("public const string Closed = \"Closed today {0}\";", strings.Source.Substring(shopClass));
        Assert.DoesNotContain("Closed(", strings.Source);
    }
}
