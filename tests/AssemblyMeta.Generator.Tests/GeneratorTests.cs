using AssemblyMeta.AssemblyInfo;
using AssemblyMeta.Constants;
using AssemblyMeta.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AssemblyMeta.Generator.Tests;

public class GeneratorTests
{
    [Fact]
    public void AssemblyInfo_generator_emits_nine_constants()
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

        var (diagnostics, source) = GeneratorTestHost.Run(new AssemblyInfoGenerator(), AssemblyInfoGenerator.PayloadKey, payload);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(source);
        foreach (var name in new[]
        {
            "Configuration", "Company", "Title", "Description", "Product", "Copyright",
            "Version", "InformationalVersion", "FileVersion",
        })
        {
            Assert.Contains("string " + name, source);
        }

        Assert.Equal("1.2.3.0", GeneratorTestHost.ExecuteConstant(source!, "AssemblyMeta+Info", "Version"));
        Assert.Equal("带 # 和 ; 的描述", GeneratorTestHost.ExecuteConstant(source!, "AssemblyMeta+Info", "Description"));
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
        var payload = PayloadCodec.EncodeConstants(null, MemberVisibility.Internal, new[]
        {
            new ConstantRecord("Http.TimeoutSeconds", "30", "int", "请求超时（秒）"),
            new ConstantRecord("class", "keyword", "", ""),
            new ConstantRecord("Foo.Bar", "1", "int", ""),
            new ConstantRecord("Foo", "2", "int", ""),
            new ConstantRecord("Banner", "line1\nline2", "string", ""),
        });

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
        var payload = PayloadCodec.EncodeAssemblyInfo(new AssemblyInfoModel
        {
            Namespace = "1Bad",
            Version = "1.0.0.0",
        });
        var (diagnostics, source) = GeneratorTestHost.Run(new AssemblyInfoGenerator(), AssemblyInfoGenerator.PayloadKey, payload);
        Assert.Contains(diagnostics, d => d.Id == "AM0005");
        Assert.DoesNotContain("namespace ", source);
        Assert.Equal("1.0.0.0", GeneratorTestHost.ExecuteConstant(source!, "AssemblyMeta+Info", "Version"));
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
