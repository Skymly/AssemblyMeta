using AssemblyMeta.Core;
using AssemblyMeta.Git;
using AssemblyMeta.Metadata;
using AssemblyMeta.Project;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AssemblyMeta.Generator.Tests;

public class ProjectMetadataGitTests
{
    [Fact]
    public void Project_and_metadata_payloads_roundtrip_special_characters()
    {
        var records = new[]
        {
            new ConstantRecord("Foo", "a;b#c\r\n第二行 \"q\" \0", "", "注释"),
            new ConstantRecord("Http.Timeout", "30", "int", ""),
        };
        var project = PayloadCodec.EncodeProject("Demo.App", MemberVisibility.Public, records);
        var metadata = PayloadCodec.EncodeMetadata("  ", MemberVisibility.Internal, records);
        Assert.DoesNotContain("#", project);
        Assert.DoesNotContain("\n", project);
        Assert.DoesNotContain(";", project);

        Assert.True(PayloadCodec.TryDecodeProject(project, out var decodedProject));
        Assert.Equal("Demo.App", decodedProject.Namespace);
        Assert.Equal(MemberVisibility.Public, decodedProject.Visibility);
        Assert.Equal("a;b#c\r\n第二行 \"q\" \0", decodedProject.Constants[0].Value);
        Assert.Equal("string", decodedProject.Constants[1].EffectiveType);

        Assert.True(PayloadCodec.TryDecodeMetadata(metadata, out var decodedMetadata));
        Assert.Null(decodedMetadata.Namespace);
        Assert.Equal(2, decodedMetadata.Constants.Count);
        Assert.False(PayloadCodec.TryDecodeProject(metadata, out _));
        Assert.False(PayloadCodec.TryDecodeMetadata("@@@", out _));
    }

    [Fact]
    public void Git_payload_roundtrips_and_rejects_corrupt_input()
    {
        var encoded = PayloadCodec.EncodeGit(new GitModel
        {
            Namespace = "Demo",
            Visibility = MemberVisibility.Public,
            Commit = "e3be6392f9afe3c1dc3947123f384df570ed9686",
            Sha = "e3be6392f",
            Root = "C:/Code/Skymly/AssemblyMeta/",
            Url = "https://github.com/Skymly/AssemblyMeta.git",
            Branch = "pr15",
            Unavailable = true,
        });
        Assert.DoesNotContain("#", encoded);
        Assert.True(PayloadCodec.TryDecodeGit(encoded, out var model));
        Assert.Equal("pr15", model.Branch);
        Assert.True(model.Unavailable);
        Assert.Equal(MemberVisibility.Public, model.Visibility);
        Assert.False(PayloadCodec.TryDecodeGit("", out _));
    }

    [Fact]
    public void Project_generator_emits_nested_constants_and_cache_hits()
    {
        var payload = PayloadCodec.EncodeProject(null, MemberVisibility.Internal, new[]
        {
            new ConstantRecord("Company", "Skymly", "", "公司名称"),
            new ConstantRecord("Http.Timeout", "30", "", ""),
            new ConstantRecord("class", "keyword", "", ""),
            new ConstantRecord("Foo", "1", "", ""),
            new ConstantRecord("Foo.Bar", "2", "", ""),
        });
        var (diagnostics, source) = GeneratorTestHost.Run(new ProjectGenerator(), ProjectGenerator.PayloadKey, payload);
        Assert.NotNull(source);
        Assert.Contains("public static partial class Project", source);
        Assert.Contains("公司名称", source);
        Assert.Contains("项目属性 Http.Timeout。", source);
        Assert.Contains("public const string @class = \"keyword\";", source);
        Assert.Contains(diagnostics, d => d.Id == "AM0001");
        Assert.Equal("Skymly", GeneratorTestHost.ExecuteConstant(source!, "AssemblyMeta+Project", "Company"));
        Assert.Equal("30", GeneratorTestHost.ExecuteConstant(source!, "AssemblyMeta+Project+Http", "Timeout"));

        var changed = PayloadCodec.EncodeProject(null, MemberVisibility.Internal, new[]
        {
            new ConstantRecord("Company", "Other", "", ""),
        });
        GeneratorTestHost.AssertCached(new ProjectGenerator(), ProjectGenerator.PayloadKey, payload, changed);
    }

    [Fact]
    public void Metadata_generator_emits_constants_and_cache_hits()
    {
        var payload = PayloadCodec.EncodeMetadata(null, MemberVisibility.Internal, new[]
        {
            new ConstantRecord("DemoKey", "a;b#c", "", ""),
            new ConstantRecord("Area.Name", "nested", "", "区域"),
        });
        var (diagnostics, source) = GeneratorTestHost.Run(new MetadataGenerator(), MetadataGenerator.PayloadKey, payload);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains("public static partial class Metadata", source);
        Assert.DoesNotContain("AssemblyMetadataAttribute", source);
        Assert.Equal("a;b#c", GeneratorTestHost.ExecuteConstant(source!, "AssemblyMeta+Metadata", "DemoKey"));
        Assert.Equal("nested", GeneratorTestHost.ExecuteConstant(source!, "AssemblyMeta+Metadata+Area", "Name"));

        var changed = PayloadCodec.EncodeMetadata(null, MemberVisibility.Internal, new[]
        {
            new ConstantRecord("DemoKey", "other", "", ""),
        });
        GeneratorTestHost.AssertCached(new MetadataGenerator(), MetadataGenerator.PayloadKey, payload, changed);
    }

    [Fact]
    public void Git_generator_emits_members_pending_placeholder_and_cache_hits()
    {
        var payload = PayloadCodec.EncodeGit(new GitModel
        {
            Commit = "e3be6392f9afe3c1dc3947123f384df570ed9686",
            Sha = "e3be6392f",
            Root = "C:/Code/Skymly/AssemblyMeta/",
            Url = "https://github.com/Skymly/AssemblyMeta.git",
            Branch = "main",
        });
        var (diagnostics, source) = GeneratorTestHost.Run(new GitGenerator(), GitGenerator.PayloadKey, payload);
        Assert.DoesNotContain(diagnostics, d => d.Id == "AM0006");
        Assert.Equal("e3be6392f9afe3c1dc3947123f384df570ed9686", GeneratorTestHost.ExecuteConstant(source!, "AssemblyMeta+Git", "Commit"));
        Assert.Equal("e3be6392f", GeneratorTestHost.ExecuteConstant(source!, "AssemblyMeta+Git", "Sha"));
        Assert.Equal("main", GeneratorTestHost.ExecuteConstant(source!, "AssemblyMeta+Git", "Branch"));

        var pending = PayloadCodec.EncodeGit(new GitModel
        {
            Commit = GitInformation.PendingValue,
            Sha = GitInformation.PendingValue,
            Root = GitInformation.PendingValue,
            Url = GitInformation.PendingValue,
            Branch = GitInformation.PendingValue,
        });
        var (pendingDiagnostics, pendingSource) = GeneratorTestHost.Run(new GitGenerator(), GitGenerator.PayloadKey, pending);
        Assert.DoesNotContain(pendingDiagnostics, d => d.Id == "AM0006");
        Assert.Equal(GitInformation.PendingValue, GeneratorTestHost.ExecuteConstant(pendingSource!, "AssemblyMeta+Git", "Root"));

        var unavailable = PayloadCodec.EncodeGit(new GitModel { Unavailable = true });
        var (unavailableDiagnostics, unavailableSource) = GeneratorTestHost.Run(new GitGenerator(), GitGenerator.PayloadKey, unavailable);
        Assert.Contains(unavailableDiagnostics, d => d.Id == "AM0006" && d.Severity == DiagnosticSeverity.Warning);
        Assert.NotNull(unavailableSource);

        GeneratorTestHost.AssertCached(new GitGenerator(), GitGenerator.PayloadKey, payload, pending);
    }

    [Theory]
    [InlineData("Project")]
    [InlineData("Metadata")]
    [InlineData("Git")]
    public void Missing_or_corrupt_payload_is_ignored_or_reported(string area)
    {
        IIncrementalGenerator generator = area switch
        {
            "Project" => new ProjectGenerator(),
            "Metadata" => new MetadataGenerator(),
            _ => new GitGenerator(),
        };
        var key = area switch
        {
            "Project" => ProjectGenerator.PayloadKey,
            "Metadata" => MetadataGenerator.PayloadKey,
            _ => GitGenerator.PayloadKey,
        };
        var (missingDiagnostics, missing) = GeneratorTestHost.Run(generator, key, null);
        Assert.Null(missing);
        Assert.DoesNotContain(missingDiagnostics, d => d.Severity == DiagnosticSeverity.Error);

        var (corrupt, source) = GeneratorTestHost.Run(generator, key, "not-base64");
        Assert.Contains(corrupt, d => d.Id == "AM0004");
        Assert.Null(source);
    }

    [Fact]
    public void Public_git_uses_static_readonly_and_invalid_namespace_warns()
    {
        var source = SourceEmission.Emit(PayloadCodec.EncodeGit(new GitModel
        {
            Namespace = "1Bad",
            Visibility = MemberVisibility.Public,
            Commit = "abc",
            Sha = "abc",
            Branch = "main",
        }), SourceKind.Git, 1200);
        Assert.Contains(source.Diagnostics, d => d.Id == "AM0005");
        Assert.Contains("public static readonly string Commit", source.Source);
        Assert.DoesNotContain("namespace ", source.Source);
    }
}
