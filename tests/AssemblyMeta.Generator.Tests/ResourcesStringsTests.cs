using System.Text;
using AssemblyMeta.Core;
using AssemblyMeta.Core.Emit;
using AssemblyMeta.Resources;
using AssemblyMeta.Strings;
using Microsoft.CodeAnalysis;

namespace AssemblyMeta.Generator.Tests;

public class ResourcesStringsTests
{
    [Fact]
    public void Resource_and_constant_payloads_roundtrip_and_do_not_cross_decode()
    {
        var resources = PayloadCodec.EncodeResources("Demo.App", MemberVisibility.Public, new[]
        {
            new ResourceRecord("Content.Docs.License", "Demo.App.Content.Docs.License.md", "a;b#c\r\n\"q\""),
        });
        var constants = PayloadCodec.EncodeConstants(null, MemberVisibility.Internal, new[]
        {
            new ConstantRecord("Hello", "你好; #\r\n", "string", "问候"),
        });
        Assert.DoesNotContain("#", resources);
        Assert.DoesNotContain("\n", constants);
        Assert.True(PayloadCodec.TryDecodeResources(resources, out var decodedResources));
        Assert.Equal("Demo.App", decodedResources.Namespace);
        Assert.Equal("a;b#c\r\n\"q\"", decodedResources.Resources[0].Comment);
        Assert.True(PayloadCodec.TryDecodeConstants(constants, out var decodedConstants));
        Assert.Null(decodedConstants.Namespace);
        Assert.Equal("你好; #\r\n", decodedConstants.Constants[0].Value);
        Assert.False(PayloadCodec.TryDecodeConstants(resources, out _));
        Assert.False(PayloadCodec.TryDecodeResources(constants, out _));
        Assert.False(PayloadCodec.TryDecodeResources("@@@", out _));
    }

    [Fact]
    public void Resx_reader_keeps_string_values_and_skips_binary()
    {
        const string xml = """
            <root>
              <data name="Hello" xml:space="preserve"><value>你好; #&#10;第二行</value><comment> 问候 </comment></data>
              <data name="Shopping_OutOfStock" xml:space="preserve"><value>缺货</value></data>
              <data name="Icon" type="System.Drawing.Icon, System.Drawing" mimetype="application/x-microsoft.net.object.binary.base64"><value>AAAA</value></data>
              <data name="Bag" type="System.Collections.Specialized.StringCollection, System"><value>AAAA</value></data>
              <data name="Label" type="System.String"><value>文本</value></data>
              <data name="Qualified" type="System.String, mscorlib"><value>限定</value></data>
            </root>
            """;
        Assert.True(ResxReader.TryRead(xml, out var entries, out var skipped, out var error), error);
        Assert.Equal(new[] { "Icon", "Bag" }, skipped);
        Assert.Equal("你好; #\n第二行", entries[0].Value);
        Assert.Equal("问候", entries[0].Comment);
        Assert.Equal("文本", entries[2].Value);
        Assert.Equal("限定", entries[3].Value);
        Assert.False(ResxReader.TryRead("<root>", out _, out _, out _));
    }

    [Fact]
    public void Strings_generator_reports_AM0007_and_AM0011_with_file_locations()
    {
        const string good = @"C:\repo\Messages.resx";
        const string broken = @"C:\repo\Broken.resx";
        var (diagnostics, _) = GeneratorTestHost.Run(
            new StringsGenerator(),
            new (string Path, string? Text)[]
            {
                (good, """
                    <root>
                      <data name="Hello" xml:space="preserve"><value>hi</value></data>
                      <data name="Icon" type="System.Drawing.Icon, System.Drawing"><value>AAAA</value></data>
                    </root>
                    """),
                (broken, "<root>"),
            },
            fileOptions: new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                [good] = ResxOptions(filename: "Messages"),
                [broken] = ResxOptions(filename: "Broken"),
            });

        var am0007 = Assert.Single(diagnostics, diagnostic => diagnostic.Id == "AM0007");
        Assert.Equal(DiagnosticSeverity.Warning, am0007.Severity);
        Assert.Equal(good, am0007.Location.GetLineSpan().Path);
        var am0011 = Assert.Single(diagnostics, diagnostic => diagnostic.Id == "AM0011");
        Assert.Equal(DiagnosticSeverity.Error, am0011.Severity);
        Assert.Equal(broken, am0011.Location.GetLineSpan().Path);
    }

    [Fact]
    public void Strings_generator_reports_AM0010_for_resx_outside_without_link()
    {
        const string path = @"C:\shared\Away.resx";
        var (diagnostics, source) = GeneratorTestHost.Run(
            new StringsGenerator(),
            new (string Path, string? Text)[] { (path, "<root><data name=\"Hello\"><value>x</value></data></root>") },
            fileOptions: new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                [path] = ResxOptions(relativeDir: @"C:\shared\", filename: "Away"),
            });

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "AM0010");
        Assert.DoesNotContain("Hello", source);
    }

    [Fact]
    public void String_names_drop_strings_file_prefix_and_split_underscores()
    {
        var named = MemberNames.Name(new MemberNameRequest
        {
            Area = MemberNameArea.Strings,
            Strings = new[]
            {
                new StringNameInput { LogicalPath = "Strings.resx", Key = "Shopping_NoShipping", Value = "不能把 {0} 送到 {1}。" },
                new StringNameInput { LogicalPath = "UI/Strings.resx", Key = "Hello", Value = "你好", Comment = "问候" },
                new StringNameInput { LogicalPath = "Errors.resx", Key = "NotFound", Value = "找不到 {0}" },
                new StringNameInput { LogicalPath = "Strings/Strings.resx", Key = "Hello", Value = "x" },
                new StringNameInput { LogicalPath = "Strings.resx", Key = "___", Spec = "Strings.resx" },
            },
        });
        Assert.Equal("Shopping.NoShipping", named.Members[0].Name);
        Assert.Equal("不能把 {0} 送到 {1}。", named.Members[0].Value);
        Assert.Equal("UI.Hello", named.Members[1].Name);
        Assert.Equal("问候", named.Members[1].Comment);
        Assert.Equal("Errors.NotFound", named.Members[2].Name);
        Assert.Equal("Hello", named.Members[3].Name);
        Assert.Equal(4, named.Members.Count);
        Assert.Contains(named.TaskDiagnostics, diagnostic => diagnostic.Id == "AM0010");
        Assert.True(StringResourceNaming.IsSatellitePath("Strings.fr.resx"));
        Assert.True(StringResourceNaming.IsSatellitePath(@"UI\Strings.zh-CN.resx"));
        Assert.False(StringResourceNaming.IsSatellitePath("Strings.resx"));
        Assert.False(StringResourceNaming.IsSatellitePath("Resources.Designer.resx"));
    }

    [Fact]
    public void Resources_generator_reads_manifest_and_renames_conflicts()
    {
        var named = MemberNames.Name(new MemberNameRequest
        {
            Area = MemberNameArea.Resources,
            Resources = new[]
            {
                new ResourceNameInput { Name = "Content.Docs.License", ManifestName = "Host.License.md", Comment = "许可证" },
                new ResourceNameInput { Name = "Resources.readme", ManifestName = "readme.txt" },
                new ResourceNameInput { Name = "Content.Text", ManifestName = "text.bin" },
                new ResourceNameInput { Name = "Manifest", ManifestName = "root.bin" },
            },
        });
        var records = new ResourceRecord[named.Members.Count];
        for (var i = 0; i < records.Length; i++)
        {
            var member = named.Members[i];
            records[i] = new ResourceRecord(member.Name, member.ManifestName ?? "", member.Comment);
        }

        var payload = PayloadCodec.EncodeResources(null, MemberVisibility.Internal, records, named.GeneratorDiagnostics);
        var (diagnostics, source) = GeneratorTestHost.Run(new ResourcesGenerator(), ResourcesGenerator.PayloadKey, payload);
        Assert.NotNull(source);
        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "AM0008");
        Assert.Contains("public static partial class Resources_", source);
        Assert.Contains("public static partial class readme", source);
        Assert.Contains("public static partial class Text_", source);
        Assert.Contains("public static partial class Manifest_", source);
        Assert.Contains("许可证", source);
        Assert.Contains("Host.License.md", source);
        Assert.Contains("private static class Manifest", source);

        var license = Encoding.UTF8.GetBytes("示例许可证文本。");
        Assert.Equal("示例许可证文本。", GeneratorTestHost.ExecuteMember(source!, "AssemblyMeta+Resources+Content+Docs+License", "Text", false, ("Host.License.md", license)));
        var bytes = (byte[])GeneratorTestHost.ExecuteMember(source!, "AssemblyMeta+Resources+Content+Docs+License", "GetBytes", true, ("Host.License.md", license));
        Assert.Equal(license, bytes);
        using var stream = (Stream)GeneratorTestHost.ExecuteMember(source!, "AssemblyMeta+Resources+Content+Docs+License", "GetStream", true, ("Host.License.md", license));
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        Assert.Equal(license, copy.ToArray());

        var changed = PayloadCodec.EncodeResources(null, MemberVisibility.Internal, new[]
        {
            new ResourceRecord("Content.Docs.License", "Other.md", ""),
        });
        GeneratorTestHost.AssertCached(new ResourcesGenerator(), ResourcesGenerator.PayloadKey, payload, changed);
    }

    [Fact]
    public void Empty_resources_emit_the_class_without_the_reader()
    {
        var emitted = SourceEmission.Emit(
            PayloadCodec.EncodeResources(null, MemberVisibility.Public, Array.Empty<ResourceRecord>()),
            SourceKind.Resources,
            1100);
        Assert.DoesNotContain(emitted.Diagnostics, diagnostic => diagnostic.Id == "AM0004");
        Assert.Contains("public static partial class AssemblyMeta", emitted.Source);
        Assert.Contains("public static partial class Resources", emitted.Source);
        Assert.DoesNotContain("class Manifest", emitted.Source);
    }

    [Fact]
    public void Strings_generator_emits_constants_and_cache_hits()
    {
        const string path = @"C:\repo\Strings.resx";
        const string xml = """
            <root>
              <data name="Hello" xml:space="preserve"><value>你好</value><comment>问候</comment></data>
              <data name="Shopping_OutOfStock"><value>缺货</value></data>
              <data name="Greeting"><value>你好，{0}</value></data>
              <data name="Strings"><value>self</value></data>
              <data name="Hello"><value>other</value></data>
            </root>
            """;
        var globals = new Dictionary<string, string> { ["build_property.AssemblyMetaNamespace"] = "Demo" };
        var fileOptions = new Dictionary<string, IReadOnlyDictionary<string, string>> { [path] = ResxOptions() };

        var (diagnostics, source) = GeneratorTestHost.Run(
            new StringsGenerator(),
            new[] { (path, (string?)xml) },
            globals,
            fileOptions);
        Assert.Contains("namespace Demo", source);
        Assert.Contains("问候", source);
        Assert.Contains("public const string Greeting = \"你好，{0}\";", source);
        Assert.Contains("public const string Strings_", source);
        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "AM0008");
        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "AM0001");
        Assert.Equal("你好", GeneratorTestHost.ExecuteConstant(source!, "Demo.AssemblyMeta+Strings", "Hello"));
        Assert.Equal("缺货", GeneratorTestHost.ExecuteConstant(source!, "Demo.AssemblyMeta+Strings+Shopping", "OutOfStock"));
        Assert.Equal("self", GeneratorTestHost.ExecuteConstant(source!, "Demo.AssemblyMeta+Strings", "Strings_"));

        var changed = new[] { (path, (string?)"<root><data name=\"Hello\"><value>其他</value></data></root>") };
        GeneratorTestHost.AssertCached(
            new StringsGenerator(),
            new[] { (path, (string?)xml) },
            changed,
            globals,
            fileOptions);
    }

    [Fact]
    public void Strings_generator_skips_satellite_and_excluded_files()
    {
        const string neutral = @"C:\repo\Strings.resx";
        const string satellite = @"C:\repo\Strings.fr.resx";
        const string cultured = @"C:\repo\Named.resx";
        const string excluded = @"C:\repo\Off.resx";
        const string xml = "<root><data name=\"Hello\"><value>x</value></data></root>";
        var (_, source) = GeneratorTestHost.Run(
            new StringsGenerator(),
            new (string Path, string? Text)[] { (neutral, xml), (satellite, xml), (cultured, xml), (excluded, xml) },
            fileOptions: new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                [neutral] = ResxOptions(),
                [satellite] = ResxOptions(filename: "Strings.fr"),
                [cultured] = ResxOptions(filename: "Named", withCulture: "true"),
                [excluded] = ResxOptions(filename: "Off", assemblyMetaStrings: "false"),
            });

        Assert.NotNull(source);
        Assert.Equal(1, source!.Split(new[] { "= \"x\";" }, StringSplitOptions.None).Length - 1);
    }

    [Fact]
    public void Strings_generator_warns_AM0012_on_unrecognized_visibility()
    {
        const string path = @"C:\repo\Strings.resx";
        var (diagnostics, source) = GeneratorTestHost.Run(
            new StringsGenerator(),
            new (string Path, string? Text)[] { (path, "<root><data name=\"Hello\"><value>x</value></data></root>") },
            new Dictionary<string, string> { ["build_property.AssemblyMetaVisibility"] = "bogus" },
            new Dictionary<string, IReadOnlyDictionary<string, string>> { [path] = ResxOptions() });

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "AM0012");
        Assert.Contains("public const string Hello", source);
    }

    [Fact]
    public void Strings_language_version_invalidates_emission()
    {
        const string path = @"C:\repo\Strings.resx";
        const string xml = "<root><data name=\"Note\"><value>line1&#10;line2</value></data></root>";
        GeneratorTestHost.AssertLanguageVersionInvalidates(
            new StringsGenerator(),
            new[] { (path, (string?)xml) },
            fileOptions: new Dictionary<string, IReadOnlyDictionary<string, string>> { [path] = ResxOptions() });
    }

    private static Dictionary<string, string> ResxOptions(
        string relativeDir = "",
        string filename = "Strings",
        string extension = ".resx",
        string? link = null,
        string? withCulture = null,
        string? assemblyMetaStrings = null)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["build_metadata.AdditionalFiles.RelativeDir"] = relativeDir,
            ["build_metadata.AdditionalFiles.Filename"] = filename,
            ["build_metadata.AdditionalFiles.Extension"] = extension,
        };
        if (link is not null)
        {
            values["build_metadata.AdditionalFiles.Link"] = link;
        }

        if (withCulture is not null)
        {
            values["build_metadata.AdditionalFiles.WithCulture"] = withCulture;
        }

        if (assemblyMetaStrings is not null)
        {
            values["build_metadata.AdditionalFiles.AssemblyMetaStrings"] = assemblyMetaStrings;
        }

        return values;
    }

    [Fact]
    public void Public_strings_are_static_readonly()
    {
        var emitted = new CSharpSourceEmitter().EmitStrings(new ConstantsModel
        {
            Visibility = MemberVisibility.Public,
            Constants = new[] { new ConstantRecord("Hello", "你好", "", "") },
        }, 1100);
        Assert.Contains("public static readonly string Hello = \"你好\";", emitted.Source);
        Assert.DoesNotContain("const string Hello", emitted.Source);
    }

    [Fact]
    public void Missing_payload_emits_nothing_for_resources()
    {
        var (missingDiagnostics, missing) = GeneratorTestHost.Run(new ResourcesGenerator(), ResourcesGenerator.PayloadKey, null);
        Assert.Null(missing);
        Assert.DoesNotContain(missingDiagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Strings_without_resx_emits_empty_class()
    {
        var (diagnostics, source) = GeneratorTestHost.Run(
            new StringsGenerator(),
            Array.Empty<(string Path, string? Text)>());
        Assert.NotNull(source);
        Assert.Contains("partial class Strings", source!);
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Invalid_namespace_warns_for_resources()
    {
        var source = SourceEmission.Emit(
            PayloadCodec.EncodeResources("1Bad", MemberVisibility.Internal, new[] { new ResourceRecord("Note", "Note.txt", "") }),
            SourceKind.Resources,
            1200);
        Assert.Contains(source.Diagnostics, diagnostic => diagnostic.Id == "AM0005");
        Assert.DoesNotContain("namespace ", source.Source);
        Assert.Contains("Note.txt", source.Source);
    }
}

