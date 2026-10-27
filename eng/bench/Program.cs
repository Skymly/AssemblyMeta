using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using AssemblyMeta.AssemblyInfo;
using AssemblyMeta.Constants;
using AssemblyMeta.Core;
using AssemblyMeta.Git;
using AssemblyMeta.Metadata;
using AssemblyMeta.Project;
using AssemblyMeta.Resources;
using AssemblyMeta.Strings;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

var cases = new (string Name, Func<string?> Run)[]
{
    ("AssemblyInfo", () => Run(new AssemblyInfoGenerator(), AssemblyInfoGenerator.PayloadKey, PayloadCodec.EncodeAssemblyInfo(new AssemblyInfoModel
    {
        Configuration = "Release",
        Company = "Skymly",
        Title = "AssemblyMeta",
        Description = "bench",
        Product = "AssemblyMeta",
        Copyright = "2026",
        Version = "1.0.0.0",
        InformationalVersion = "1.0.0",
        FileVersion = "1.0.0.0",
    }))),
    ("Constants", () => Run(new ConstantsGenerator(), ConstantsGenerator.PayloadKey, PayloadCodec.EncodeConstants(null, MemberVisibility.Internal, new[]
    {
        new ConstantRecord("Http.TimeoutSeconds", "30", "int", ""),
    }, Array.Empty<DiagnosticReport>()))),
    ("Project", () => Run(new ProjectGenerator(), ProjectGenerator.PayloadKey, PayloadCodec.EncodeProject(null, MemberVisibility.Internal, new[]
    {
        new ConstantRecord("AssemblyName", "Bench", "string", ""),
        new ConstantRecord("TargetFrameworkIdentifier", ".NETCoreApp", "string", ""),
    }))),
    ("Metadata", () => Run(new MetadataGenerator(), MetadataGenerator.PayloadKey, PayloadCodec.EncodeMetadata(null, MemberVisibility.Internal, new[]
    {
        new ConstantRecord("RepositoryUrl", "https://github.com/Skymly/AssemblyMeta", "string", ""),
    }))),
    ("Git", () => Run(new GitGenerator(), GitGenerator.PayloadKey, PayloadCodec.EncodeGit(new GitModel
    {
        Commit = "0123456789abcdef",
        Sha = "012345678",
        Root = "C:/Code/Skymly/AssemblyMeta/",
        Url = "https://github.com/Skymly/AssemblyMeta",
        Branch = "goal/v1",
    }))),
    ("Resources", () => Run(new ResourcesGenerator(), ResourcesGenerator.PayloadKey, PayloadCodec.EncodeResources(null, MemberVisibility.Internal, new[]
    {
        new ResourceRecord("Content.Docs.License", "Bench.Content.Docs.License.md", ""),
    }, Array.Empty<DiagnosticReport>()))),
    ("Strings", () => RunStrings(new StringsGenerator())),
};

const int warmup = 10;
const int samples = 50;
var sink = 0;
Console.WriteLine("name\tus\tbytes\tsourceChars");
foreach (var item in cases)
{
    string? source = null;
    var measured = Measure(() => source = item.Run(), warmup, samples);
    sink += source?.Length ?? 0;
    Console.WriteLine($"{item.Name}\t{measured.Microseconds}\t{measured.Bytes}\t{source?.Length ?? 0}");
}

Console.WriteLine($"sink={sink}");
if (sink == 0)
{
    return 1;
}

return 0;

static (long Microseconds, long Bytes) Measure(Action action, int warmupCount, int sampleCount)
{
    for (var i = 0; i < warmupCount; i++)
    {
        action();
    }

    var times = new long[sampleCount];
    var bytes = new long[sampleCount];
    for (var i = 0; i < sampleCount; i++)
    {
        var before = GC.GetTotalAllocatedBytes(precise: true);
        var start = Stopwatch.GetTimestamp();
        action();
        var elapsed = Stopwatch.GetTimestamp() - start;
        var after = GC.GetTotalAllocatedBytes(precise: true);
        times[i] = elapsed * 1_000_000 / Stopwatch.Frequency;
        bytes[i] = after - before;
    }

    Array.Sort(times);
    Array.Sort(bytes);
    return (times[sampleCount / 2], bytes[sampleCount / 2]);
}

static CSharpCompilation BenchCompilation()
    => CSharpCompilation.Create(
        "AssemblyMeta.Bench.Input",
        new[] { CSharpSyntaxTree.ParseText("namespace Host; internal static class Marker {}") },
        new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

static string? Run(IIncrementalGenerator generator, string payloadKey, string payload)
{
    GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
    driver = driver
        .WithUpdatedParseOptions(new CSharpParseOptions(LanguageVersion.CSharp12))
        .WithUpdatedAnalyzerConfigOptions(new BenchOptionsProvider(new Dictionary<string, string> { [payloadKey] = payload }));
    driver = driver.RunGenerators(BenchCompilation());
    var generated = driver.GetRunResult().Results[0].GeneratedSources;
    return generated.Length == 0 ? null : generated[0].SourceText.ToString();
}

static string? RunStrings(IIncrementalGenerator generator)
{
    const string path = @"C:\repo\Strings.resx";
    var texts = ImmutableArray.Create<AdditionalText>(new BenchAdditionalText(
        path,
        "<root><data name=\"Hello\" xml:space=\"preserve\"><value>hello {0}</value></data></root>"));
    var fileOptions = new Dictionary<string, string>
    {
        ["build_metadata.AdditionalFiles.Filename"] = "Strings",
        ["build_metadata.AdditionalFiles.Extension"] = ".resx",
    };
    GeneratorDriver driver = CSharpGeneratorDriver.Create(
        new[] { generator.AsSourceGenerator() },
        additionalTexts: texts,
        parseOptions: new CSharpParseOptions(LanguageVersion.CSharp12),
        optionsProvider: new BenchOptionsProvider(new Dictionary<string, string>(), fileOptions));
    driver = driver.RunGenerators(BenchCompilation());
    var generated = driver.GetRunResult().Results[0].GeneratedSources;
    return generated.Length == 0 ? null : generated[0].SourceText.ToString();
}

file sealed class BenchAdditionalText : AdditionalText
{
    private readonly SourceText _text;

    public BenchAdditionalText(string path, string text)
    {
        Path = path;
        _text = SourceText.From(text);
    }

    public override string Path { get; }

    public override SourceText GetText(CancellationToken cancellationToken = default) => _text;
}

file sealed class BenchOptions : AnalyzerConfigOptions
{
    private readonly IReadOnlyDictionary<string, string> _values;

    public BenchOptions(IReadOnlyDictionary<string, string> values)
    {
        _values = values;
    }

    public override bool TryGetValue(string key, out string value)
        => _values.TryGetValue(key, out value!);
}

file sealed class BenchOptionsProvider : AnalyzerConfigOptionsProvider
{
    private readonly AnalyzerConfigOptions _fileOptions;

    public BenchOptionsProvider(IReadOnlyDictionary<string, string> global, IReadOnlyDictionary<string, string>? file = null)
    {
        GlobalOptions = new BenchOptions(global);
        _fileOptions = file is null ? GlobalOptions : new BenchOptions(file);
    }

    public override AnalyzerConfigOptions GlobalOptions { get; }

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;

    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => _fileOptions;
}
