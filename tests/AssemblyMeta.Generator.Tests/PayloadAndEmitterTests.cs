using AssemblyMeta.Core;
using AssemblyMeta.Core.Emit;

namespace AssemblyMeta.Generator.Tests;

public class PayloadAndEmitterTests
{
    [Fact]
    public void AssemblyInfo_payload_roundtrips_special_characters()
    {
        var model = new AssemblyInfoModel
        {
            Namespace = "Demo.App",
            Visibility = MemberVisibility.Public,
            Configuration = "Debug",
            Company = "Skymly",
            Title = "标题",
            Description = "第一行\n第二行 \"引号\" ; # \0",
            Product = "产品",
            Copyright = "Copyright © 2026",
            Version = "1.2.3.0",
            InformationalVersion = "1.2.3-beta.4",
            FileVersion = "1.2.3.4",
        };

        var encoded = PayloadCodec.EncodeAssemblyInfo(model);
        Assert.DoesNotContain("\n", encoded);
        Assert.DoesNotContain("#", encoded);
        Assert.True(PayloadCodec.TryDecodeAssemblyInfo(encoded, out var decoded));
        Assert.Equal(model.Namespace, decoded.Namespace);
        Assert.Equal(MemberVisibility.Public, decoded.Visibility);
        Assert.Equal(model.Description, decoded.Description);
        Assert.Equal(model.Version, decoded.Version);
        Assert.Equal(model.Copyright, decoded.Copyright);
    }

    [Fact]
    public void Constants_payload_roundtrips_multiline_and_empty()
    {
        var records = new[]
        {
            new ConstantRecord("Http.TimeoutSeconds", "30", "int", "请求超时"),
            new ConstantRecord("Banner", "a;b#c\r\n第二行", "", "注释"),
        };
        var encoded = PayloadCodec.EncodeConstants("  ", MemberVisibility.Internal, records);
        Assert.True(PayloadCodec.TryDecodeConstants(encoded, out var model));
        Assert.Null(model.Namespace);
        Assert.Equal(2, model.Constants.Count);
        Assert.Equal("a;b#c\r\n第二行", model.Constants[1].Value);
        Assert.Equal("string", model.Constants[1].EffectiveType);
    }

    [Fact]
    public void Corrupt_payload_is_rejected()
    {
        Assert.False(PayloadCodec.TryDecodeAssemblyInfo("@@@", out _));
        Assert.False(PayloadCodec.TryDecodeConstants("", out _));
    }

    [Fact]
    public void Registry_reserves_vb_and_fsharp()
    {
        Assert.NotNull(EmitterRegistry.Shared.TryResolve(SourceLanguage.CSharp));
        Assert.Null(EmitterRegistry.Shared.TryResolve(SourceLanguage.VisualBasic));
        Assert.Null(EmitterRegistry.Shared.TryResolve(SourceLanguage.FSharp));
    }

    [Fact]
    public void FileConstant_uses_forward_slashes_and_link()
    {
        Assert.True(FileConstantNaming.TryCreate(
            new FileConstantNaming.Input(@"Content\Docs\", "License", ".md", "", ""),
            out var license));
        Assert.Equal("Content.Docs.License", license.Name);
        Assert.Equal("Content/Docs/License.md", license.Value);

        Assert.True(FileConstantNaming.TryCreate(
            new FileConstantNaming.Input(@"Content\Docs\", "12. Readme (copy)", ".txt", "", ""),
            out var readme));
        Assert.Equal("Content.Docs.12. Readme (copy)", readme.Name);

        Assert.True(FileConstantNaming.TryCreate(
            new FileConstantNaming.Input(@"C:\outside\", "Readme", ".txt", @"Included\Readme.txt", ""),
            out var linked));
        Assert.Equal("Included.Readme", linked.Name);
        Assert.Equal("Included/Readme.txt", linked.Value);

        Assert.False(FileConstantNaming.TryCreate(
            new FileConstantNaming.Input(@"C:\outside\", "Readme", ".txt", "", ""),
            out _));
    }

    [Fact]
    public void Sanitizer_collapses_invalid_characters_and_escapes_keywords()
    {
        Assert.True(IdentifierSanitizer.TrySanitize(" Readme (copy)", out var name));
        Assert.Equal("_Readme_copy_", name);
        Assert.True(IdentifierSanitizer.TrySanitize("12", out var digits));
        Assert.Equal("_12", digits);
        Assert.True(IdentifierSanitizer.TrySanitize("class", out var keyword));
        Assert.Equal("@class", keyword);
        Assert.False(IdentifierSanitizer.TrySanitize("...", out _));
    }

    [Fact]
    public void Multiline_string_uses_raw_literal_and_roundtrips()
    {
        var model = new AssemblyInfoModel
        {
            Description = "第一行\n第二行 \"引号\"",
            Version = "1.0.0.0",
        };
        var source = new CSharpSourceEmitter().EmitAssemblyInfo(model, 1200).Source!;
        Assert.Contains("\"\"\"", source);
        Assert.Equal(model.Description, GeneratorTestHost.ExecuteConstant(source, "AssemblyMeta+Info", "Description", Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12));
    }

    [Fact]
    public void Older_language_version_does_not_emit_raw_strings()
    {
        var source = new CSharpSourceEmitter().EmitAssemblyInfo(
            new AssemblyInfoModel { Description = "第一行\n第二行" },
            1000).Source!;
        Assert.DoesNotContain("\"\"\"", source);
        Assert.Contains("\\n", source);
        Assert.Equal("第一行\n第二行", GeneratorTestHost.ExecuteConstant(source, "AssemblyMeta+Info", "Description", Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10));
    }

    [Fact]
    public void Public_visibility_uses_static_readonly_and_namespace()
    {
        var source = new CSharpSourceEmitter().EmitAssemblyInfo(
            new AssemblyInfoModel { Namespace = "Demo.App", Visibility = MemberVisibility.Public, Version = "9.9.9.9" },
            1200).Source!;
        Assert.Contains("namespace Demo.App", source);
        Assert.Contains("public static partial class AssemblyMeta", source);
        Assert.Contains("public static readonly string Version", source);
        Assert.DoesNotContain("const string Version", source);
    }
}
