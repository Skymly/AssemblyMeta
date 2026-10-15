using System.Diagnostics;
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

var cases = new (string Name, IIncrementalGenerator Generator, string Key, string Payload)[]
{
    ("AssemblyInfo", new AssemblyInfoGenerator(), AssemblyInfoGenerator.PayloadKey, PayloadCodec.EncodeAssemblyInfo(new AssemblyInfoModel
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
    })),
    ("Constants", new ConstantsGenerator(), ConstantsGenerator.PayloadKey, PayloadCodec.EncodeConstants(null, MemberVisibility.Internal, new[]
    {
        new ConstantRecord("Http.TimeoutSeconds", "30", "int", ""),
    }, Array.Empty<DiagnosticReport>())),
    ("Project", new ProjectGenerator(), ProjectGenerator.PayloadKey, PayloadCodec.EncodeProject(null, MemberVisibility.Internal, new[]
    {
        new ConstantRecord("AssemblyName", "Bench", "string", ""),
        new ConstantRecord("TargetFrameworkIdentifier", ".NETCoreApp", "string", ""),
    })),
    ("Metadata", new MetadataGenerator(), MetadataGenerator.PayloadKey, PayloadCodec.EncodeMetadata(null, MemberVisibility.Internal, new[]
    {
        new ConstantRecord("RepositoryUrl", "https://github.com/Skymly/AssemblyMeta", "string", ""),
    })),
    ("Git", new GitGenerator(), GitGenerator.PayloadKey, PayloadCodec.EncodeGit(new GitModel
    {
        Commit = "0123456789abcdef",
        Sha = "012345678",
        Root = "C:/Code/Skymly/AssemblyMeta/",
        Url = "https://github.com/Skymly/AssemblyMeta",
        Branch = "goal/v1",
    })),
    ("Resources", new ResourcesGenerator(), ResourcesGenerator.PayloadKey, PayloadCodec.EncodeResources(null, MemberVisibility.Internal, new[]
    {
        new ResourceRecord("Content.Docs.License", "Bench.Content.Docs.License.md", ""),
    }, Array.Empty<DiagnosticReport>())),
    ("Strings", new StringsGenerator(), StringsGenerator.PayloadKey, PayloadCodec.EncodeStrings(null, MemberVisibility.Internal, new[]
    {
        new ConstantRecord("Hello", "hello {0}", "string", ""),
    }, Array.Empty<DiagnosticReport>())),
};

const int warmup = 10;
const int samples = 50;
var sink = 0;
Console.WriteLine("name\tus\tbytes\tsourceChars");
foreach (var item in cases)
{
    string? source = null;
    var measured = Measure(() => source = Run(item.Generator, item.Key, item.Payload), warmup, samples);
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

static string? Run(IIncrementalGenerator generator, string payloadKey, string payload)
{
    var compilation = CSharpCompilation.Create(
        "AssemblyMeta.Bench.Input",
        new[] { CSharpSyntaxTree.ParseText("namespace Host; internal static class Marker {}") },
        new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
    driver = driver
        .WithUpdatedParseOptions(new CSharpParseOptions(LanguageVersion.CSharp12))
        .WithUpdatedAnalyzerConfigOptions(new BenchOptionsProvider(payloadKey, payload));
    driver = driver.RunGenerators(compilation);
    var generated = driver.GetRunResult().Results[0].GeneratedSources;
    return generated.Length == 0 ? null : generated[0].SourceText.ToString();
}

file sealed class BenchOptions : AnalyzerConfigOptions
{
    private readonly string _key;
    private readonly string _payload;

    public BenchOptions(string key, string payload)
    {
        _key = key;
        _payload = payload;
    }

    public override bool TryGetValue(string key, out string value)
    {
        if (string.Equals(key, _key, StringComparison.Ordinal))
        {
            value = _payload;
            return true;
        }

        value = "";
        return false;
    }
}

file sealed class BenchOptionsProvider : AnalyzerConfigOptionsProvider
{
    public BenchOptionsProvider(string key, string payload)
    {
        GlobalOptions = new BenchOptions(key, payload);
    }

    public override AnalyzerConfigOptions GlobalOptions { get; }

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;

    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;
}
