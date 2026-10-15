using AssemblyMeta.Core;

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
    public void Constants_names_escape_keywords_and_keep_values()
    {
        var result = MemberNames.Name(new MemberNameRequest
        {
            Area = MemberNameArea.Constants,
            Constants = new[]
            {
                new ConstantNameInput { Name = "class", Value = "keyword", Type = "string", Comment = "" },
            },
        });

        Assert.Empty(result.TaskDiagnostics);
        Assert.Empty(result.GeneratorDiagnostics);
        var member = Assert.Single(result.Members);
        Assert.Equal("@class", member.Name);
        Assert.Equal("keyword", member.Value);
        Assert.Equal("string", member.Type);
    }

    [Fact]
    public void File_constants_keep_forward_slashes_and_skip_outside_files()
    {
        var result = MemberNames.Name(new MemberNameRequest
        {
            Area = MemberNameArea.Constants,
            FileConstants = new[]
            {
                new FileConstantNameInput { RelativeDir = @"Content\Docs\", Filename = "License", Extension = ".md" },
                new FileConstantNameInput { RelativeDir = @"C:\outside\", Filename = "Readme", Extension = ".txt" },
                new FileConstantNameInput
                {
                    RelativeDir = @"C:\outside\",
                    Filename = "Readme",
                    Extension = ".txt",
                    Link = @"Included\Readme.txt",
                },
            },
        });

        Assert.Empty(result.TaskDiagnostics);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal(2, result.Members.Count);
        Assert.Equal("Content.Docs.License", result.Members[0].Name);
        Assert.Equal("Content/Docs/License.md", result.Members[0].Value);
        Assert.Equal("Included.Readme", result.Members[1].Name);
        Assert.Equal("Included/Readme.txt", result.Members[1].Value);
    }

    [Fact]
    public void Duplicate_constant_names_report_AM0001_and_keep_the_first()
    {
        var result = MemberNames.Name(new MemberNameRequest
        {
            Area = MemberNameArea.Constants,
            Constants = new[]
            {
                new ConstantNameInput { Name = "Foo.Bar", Value = "1", Type = "int" },
                new ConstantNameInput { Name = "Foo", Value = "2", Type = "int" },
            },
        });

        var member = Assert.Single(result.Members);
        Assert.Equal("Foo.Bar", member.Name);
        var diagnostic = Assert.Single(result.GeneratorDiagnostics);
        Assert.Equal("AM0001", diagnostic.Id);
        Assert.Empty(result.TaskDiagnostics);
    }

    [Fact]
    public void Resource_names_avoid_reserved_identifiers_and_skip_missing_manifests()
    {
        var result = MemberNames.Name(new MemberNameRequest
        {
            Area = MemberNameArea.Resources,
            Resources = new[]
            {
                new ResourceNameInput { Name = "Resources.readme", ManifestName = "readme.txt", Spec = "readme.txt" },
                new ResourceNameInput { Name = "Content.Text", ManifestName = "text.bin", Spec = "text.bin" },
                new ResourceNameInput { Name = "Manifest", ManifestName = "root.bin", Spec = "root.bin" },
                new ResourceNameInput { Spec = "missing.bin" },
            },
        });

        Assert.Equal("Resources_.readme", result.Members[0].Name);
        Assert.Equal("Content.Text_", result.Members[1].Name);
        Assert.Equal("Manifest_", result.Members[2].Name);
        Assert.Equal(3, result.Members.Count);
        Assert.Equal(3, result.GeneratorDiagnostics.Count);
        Assert.All(result.GeneratorDiagnostics, diagnostic => Assert.Equal("AM0008", diagnostic.Id));
        var task = Assert.Single(result.TaskDiagnostics);
        Assert.Equal("AM0009", task.Id);
    }

    [Fact]
    public void String_names_avoid_the_enclosing_type_and_skip_invalid_keys()
    {
        var result = MemberNames.Name(new MemberNameRequest
        {
            Area = MemberNameArea.Strings,
            Strings = new[]
            {
                new StringNameInput { Name = "Hello", Value = "你好", Comment = "问候" },
                new StringNameInput { Name = "Strings", Value = "self" },
                new StringNameInput { Name = "Hello", Value = "other" },
                new StringNameInput { Key = "___", Spec = "Strings.resx", LogicalPath = "Strings.resx" },
                new StringNameInput { OutsideWithoutLink = true, Spec = @"C:\outside\Strings.resx" },
            },
        });

        Assert.Equal("Hello", result.Members[0].Name);
        Assert.Equal("Strings_", result.Members[1].Name);
        Assert.Equal("字符串资源 Strings。", result.Members[1].Comment);
        Assert.Equal(2, result.Members.Count);
        Assert.Contains(result.GeneratorDiagnostics, diagnostic => diagnostic.Id == "AM0008");
        Assert.Contains(result.GeneratorDiagnostics, diagnostic => diagnostic.Id == "AM0001");
        Assert.Equal(2, result.TaskDiagnostics.Count);
        Assert.All(result.TaskDiagnostics, diagnostic => Assert.Equal("AM0010", diagnostic.Id));
    }

    [Fact]
    public void File_constant_names_sanitize_awkward_segments()
    {
        var result = MemberNames.Name(new MemberNameRequest
        {
            Area = MemberNameArea.Constants,
            FileConstants = new[]
            {
                new FileConstantNameInput
                {
                    RelativeDir = @"Content\Docs\",
                    Filename = "12. Readme (copy)",
                    Extension = ".txt",
                },
            },
        });

        var member = Assert.Single(result.Members);
        Assert.Equal("Content.Docs._12._Readme_copy_", member.Name);
        Assert.Equal("Content/Docs/12. Readme (copy).txt", member.Value);
        Assert.Empty(result.GeneratorDiagnostics);
    }

    [Fact]
    public void Constant_names_collapse_invalid_characters()
    {
        var result = MemberNames.Name(new MemberNameRequest
        {
            Area = MemberNameArea.Constants,
            Constants = new[]
            {
                new ConstantNameInput { Name = " Readme (copy)", Value = "a" },
                new ConstantNameInput { Name = "12", Value = "b" },
                new ConstantNameInput { Name = "...", Value = "c" },
            },
        });

        Assert.Equal("_Readme_copy_", result.Members[0].Name);
        Assert.Equal("_12", result.Members[1].Name);
        Assert.Equal(2, result.Members.Count);
        var diagnostic = Assert.Single(result.GeneratorDiagnostics);
        Assert.Equal("AM0002", diagnostic.Id);
    }

    [Fact]
    public void Final_constant_names_are_not_diagnosed_again_by_the_emitter()
    {
        var payload = PayloadCodec.EncodeConstants(
            null,
            MemberVisibility.Internal,
            new[]
            {
                new ConstantRecord("Foo", "1", "int", ""),
                new ConstantRecord("Foo", "2", "int", ""),
                new ConstantRecord("...", "3", "int", ""),
            },
            Array.Empty<DiagnosticReport>());
        var result = SourceEmission.Emit(payload, SourceKind.Constants, 1100);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Id == "AM0001" || diagnostic.Id == "AM0002");
        Assert.Contains("const int Foo = 1;", result.Source);
        Assert.DoesNotContain("= 2;", result.Source);
    }

    [Fact]
    public void Version1_constant_payload_still_escapes_keywords()
    {
        var payload = PayloadCodec.EncodeConstants(null, MemberVisibility.Internal, new[]
        {
            new ConstantRecord("class", "keyword", "string", ""),
        });
        Assert.True(PayloadCodec.TryDecodeConstants(payload, out var model));
        Assert.False(model.NamesAreFinal);
        var result = SourceEmission.Emit(payload, SourceKind.Constants, 1100);
        Assert.Contains("const string @class = \"keyword\";", result.Source);
    }

    [Fact]
    public void Empty_non_string_does_not_block_a_later_value()
    {
        var named = MemberNames.Name(new MemberNameRequest
        {
            Area = MemberNameArea.Constants,
            Constants = new[]
            {
                new ConstantNameInput { Name = "Timeout", Type = "int", Value = "" },
                new ConstantNameInput { Name = "Timeout", Type = "int", Value = "30" },
            },
        });

        Assert.Equal(2, named.Members.Count);
        Assert.DoesNotContain(named.GeneratorDiagnostics, diagnostic => diagnostic.Id == "AM0001");
        var records = new ConstantRecord[named.Members.Count];
        for (var i = 0; i < records.Length; i++)
        {
            var member = named.Members[i];
            records[i] = new ConstantRecord(member.Name, member.Value, member.Type, member.Comment);
        }

        var payload = PayloadCodec.EncodeConstants(null, MemberVisibility.Internal, records, named.GeneratorDiagnostics);
        var result = SourceEmission.Emit(payload, SourceKind.Constants, 1100);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "AM0003");
        Assert.Contains("const int Timeout = 30;", result.Source);
    }

    [Fact]
    public void Resource_manifest_names_are_trimmed()
    {
        var result = MemberNames.Name(new MemberNameRequest
        {
            Area = MemberNameArea.Resources,
            Resources = new[]
            {
                new ResourceNameInput
                {
                    RelativeDir = @"Content\",
                    Filename = "License",
                    Extension = ".md",
                    ManifestName = "  Host.License.md  ",
                },
            },
        });

        var member = Assert.Single(result.Members);
        Assert.Equal("Host.License.md", member.ManifestName);
        Assert.Equal("Host.License.md", member.Value);
        Assert.Empty(result.TaskDiagnostics);
    }

    [Fact]
    public void Multiline_string_uses_raw_literal_and_roundtrips()
    {
        var model = new AssemblyInfoModel
        {
            Description = "第一行\n第二行 \"引号\"",
            Version = "1.0.0.0",
        };
        var source = SourceEmission.Emit(PayloadCodec.EncodeAssemblyInfo(model), SourceKind.AssemblyInfo, 1200).Source!;
        Assert.Contains("\"\"\"", source);
        Assert.Equal(model.Description, GeneratorTestHost.ExecuteConstant(source, "AssemblyMeta+Info", "Description", Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12));
    }

    [Fact]
    public void Older_language_version_does_not_emit_raw_strings()
    {
        var source = SourceEmission.Emit(
            PayloadCodec.EncodeAssemblyInfo(new AssemblyInfoModel { Description = "第一行\n第二行" }),
            SourceKind.AssemblyInfo,
            1000).Source!;
        Assert.DoesNotContain("\"\"\"", source);
        Assert.Contains("\\n", source);
        Assert.Equal("第一行\n第二行", GeneratorTestHost.ExecuteConstant(source, "AssemblyMeta+Info", "Description", Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10));
    }

    [Fact]
    public void Public_visibility_uses_static_readonly_and_namespace()
    {
        var source = SourceEmission.Emit(
            PayloadCodec.EncodeAssemblyInfo(new AssemblyInfoModel { Namespace = "Demo.App", Visibility = MemberVisibility.Public, Version = "9.9.9.9" }),
            SourceKind.AssemblyInfo,
            1200).Source!;
        Assert.Contains("namespace Demo.App", source);
        Assert.Contains("public static partial class AssemblyMeta", source);
        Assert.Contains("public static readonly string Version", source);
        Assert.DoesNotContain("const string Version", source);
    }

    [Fact]
    public void Source_emission_decodes_constants_and_rejects_a_bad_payload()
    {
        var payload = PayloadCodec.EncodeConstants(null, MemberVisibility.Internal, new[]
        {
            new ConstantRecord("Answer", "42", "int", ""),
        });
        var emitted = SourceEmission.Emit(payload, SourceKind.Constants, 1100);
        Assert.NotNull(emitted.Source);
        Assert.Contains("const int Answer = 42;", emitted.Source);
        Assert.DoesNotContain(emitted.Diagnostics, diagnostic => diagnostic.Id == "AM0004");

        var rejected = SourceEmission.Emit("not-base64", SourceKind.Constants, 1100);
        Assert.Null(rejected.Source);
        var diagnostic = Assert.Single(rejected.Diagnostics);
        Assert.Equal("AM0004", diagnostic.Id);
        Assert.Equal("AssemblyMeta 载荷无效", diagnostic.Title);
        Assert.Equal("无法解析 Constants 编译期载荷。请清理 obj 后重新生成。", diagnostic.Message);
    }

    [Fact]
    public void Oversized_diagnostic_count_is_rejected()
    {
        var payload = CraftConstantsPayload("2147483647");
        Assert.False(PayloadCodec.TryDecodeConstants(payload, out _));
    }

    private static string CraftConstantsPayload(string diagnosticCount)
    {
        using var stream = new System.IO.MemoryStream();
        using (var writer = new System.IO.BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(new byte[] { (byte)'A', (byte)'M', (byte)'C', (byte)'S' });
            writer.Write(2);
            writer.Write(3);
            WriteField(writer, "");
            WriteField(writer, "internal");
            WriteField(writer, diagnosticCount);
        }

        return Convert.ToBase64String(stream.ToArray());
    }

    private static void WriteField(System.IO.BinaryWriter writer, string value)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }
}
