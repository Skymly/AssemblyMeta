using AssemblyMeta.AssemblyInfo;
using AssemblyMeta.Constants;
using AssemblyMeta.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AssemblyMeta.Generator.Tests;

public class GeneratorTests
{
    [Fact]
    public void AssemblyInfo_emission_lists_nine_constants()
    {
        var payload = PayloadCodec.EncodeAssemblyInfo(new AssemblyInfoModel
        {
            Configuration = "Debug",
            Company = "Skymly",
            Title = "标题",
            Description = "带 # 和 ; 的描述",
            Product = "产品",
            Copyright = "Copyright © 2026",
            Version = "1.2.3.0",
            InformationalVersion = "1.2.3-beta.4",
            FileVersion = "1.2.3.4",
        });

        var emitted = SourceEmission.Emit(payload, SourceKind.AssemblyInfo, 1100);
        Assert.DoesNotContain(emitted.Diagnostics, diagnostic => diagnostic.Id == "AM0004");
        Assert.NotNull(emitted.Source);
        foreach (var name in new[]
        {
            "Configuration", "Company", "Title", "Description", "Product", "Copyright",
            "Version", "InformationalVersion", "FileVersion",
        })
        {
            Assert.Contains("string " + name, emitted.Source);
        }

        Assert.Equal("1.2.3.0", GeneratorTestHost.ExecuteConstant(emitted.Source, "AssemblyMeta+Info", "Version"));
        Assert.Equal("带 # 和 ; 的描述", GeneratorTestHost.ExecuteConstant(emitted.Source, "AssemblyMeta+Info", "Description"));
    }

    [Fact]
    public void Missing_payload_emits_nothing()
    {
        var (_, source) = GeneratorTestHost.Run(new AssemblyInfoGenerator(), AssemblyInfoGenerator.PayloadKey, null);
        Assert.Null(source);
    }

    [Fact]
    public void Corrupt_payload_reports_AM0004()
    {
        var (diagnostics, source) = GeneratorTestHost.Run(new AssemblyInfoGenerator(), AssemblyInfoGenerator.PayloadKey, "not-base64");
        Assert.Contains(diagnostics, d => d.Id == "AM0004");
        Assert.Null(source);
    }

    [Fact]
    public void Constants_generator_emits_nested_typed_members_and_duplicate_diagnostic()
    {
        var named = MemberNames.Name(new MemberNameRequest
        {
            Area = MemberNameArea.Constants,
            Constants = new[]
            {
                new ConstantNameInput { Name = "Http.TimeoutSeconds", Value = "30", Type = "int", Comment = "请求超时（秒）" },
                new ConstantNameInput { Name = "class", Value = "keyword" },
                new ConstantNameInput { Name = "Foo.Bar", Value = "1", Type = "int" },
                new ConstantNameInput { Name = "Foo", Value = "2", Type = "int" },
                new ConstantNameInput { Name = "Banner", Value = "line1\nline2", Type = "string" },
            },
        });
        var records = new ConstantRecord[named.Members.Count];
        for (var i = 0; i < records.Length; i++)
        {
            var member = named.Members[i];
            records[i] = new ConstantRecord(member.Name, member.Value, member.Type, member.Comment);
        }

        var payload = PayloadCodec.EncodeConstants(null, MemberVisibility.Internal, records, named.GeneratorDiagnostics);

        var (diagnostics, source) = GeneratorTestHost.Run(
            new ConstantsGenerator(),
            ConstantsGenerator.PayloadKey,
            payload,
            LanguageVersion.CSharp12);
        Assert.NotNull(source);
        Assert.Contains("public static partial class Http", source);
        Assert.Contains("public const int TimeoutSeconds = 30;", source);
        Assert.Contains("public const string @class = \"keyword\";", source);
        Assert.Contains("\"\"\"", source);
        Assert.Contains(diagnostics, d => d.Id == "AM0001");
        Assert.Equal("30", GeneratorTestHost.ExecuteConstant(source!, "AssemblyMeta+Constants+Http", "TimeoutSeconds"));
        Assert.Equal("line1\nline2", GeneratorTestHost.ExecuteConstant(source!, "AssemblyMeta+Constants", "Banner", LanguageVersion.CSharp12));
    }

    [Fact]
    public void Invalid_namespace_falls_back_and_warns()
    {
        var emitted = SourceEmission.Emit(PayloadCodec.EncodeAssemblyInfo(new AssemblyInfoModel
        {
            Namespace = "1Bad",
            Version = "1.0.0.0",
        }), SourceKind.AssemblyInfo, 1100);
        Assert.Contains(emitted.Diagnostics, diagnostic => diagnostic.Id == "AM0005");
        Assert.DoesNotContain("namespace ", emitted.Source);
        Assert.Equal("1.0.0.0", GeneratorTestHost.ExecuteConstant(emitted.Source!, "AssemblyMeta+Info", "Version"));
    }

    [Fact]
    public void Unchanged_analyzer_options_reuse_incremental_output()
    {
        var payload = PayloadCodec.EncodeAssemblyInfo(new AssemblyInfoModel { Version = "3.0.0.0", Title = "缓存" });
        var values = new Dictionary<string, string>
        {
            [AssemblyInfoGenerator.PayloadKey] = payload,
        };
        var provider = new TestAnalyzerConfigOptionsProvider(values);
        var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp12);
        var compilation = CSharpCompilation.Create(
            "incremental",
            new[] { CSharpSyntaxTree.ParseText("class A {}") },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new[] { new AssemblyInfoGenerator().AsSourceGenerator() },
            additionalTexts: null,
            parseOptions: parseOptions,
            optionsProvider: provider,
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

        driver = driver.RunGenerators(compilation);
        var first = driver.GetRunResult().Results[0].GeneratedSources[0].SourceText.ToString();

        var updated = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("class B {}"));
        driver = driver.RunGenerators(updated);
        var secondResult = driver.GetRunResult();
        var second = secondResult.Results[0].GeneratedSources[0].SourceText.ToString();
        Assert.Equal(first, second);

        var reasons = secondResult.Results[0].TrackedSteps
            .SelectMany(pair => pair.Value)
            .SelectMany(step => step.Outputs)
            .Select(output => output.Reason)
            .ToArray();
        Assert.Contains(IncrementalStepRunReason.Cached, reasons);
        Assert.DoesNotContain(IncrementalStepRunReason.Modified, reasons);
    }
}
