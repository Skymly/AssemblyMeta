using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using AssemblyMeta.AssemblyInfo;
using AssemblyMeta.Constants;
using AssemblyMeta.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace AssemblyMeta.Generator.Tests;

internal sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
{
    private readonly Dictionary<string, string> _values;

    public TestAnalyzerConfigOptions(IReadOnlyDictionary<string, string> values)
    {
        _values = new Dictionary<string, string>(values, StringComparer.Ordinal);
    }

    public override bool TryGetValue(string key, out string value)
        => _values.TryGetValue(key, out value!);
}

internal sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
{
    public TestAnalyzerConfigOptionsProvider(IReadOnlyDictionary<string, string> global)
    {
        GlobalOptions = new TestAnalyzerConfigOptions(global);
    }

    public override AnalyzerConfigOptions GlobalOptions { get; }

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;

    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;
}

internal static class GeneratorTestHost
{
    private static readonly MetadataReference[] References = CreateReferences();

    public static (ImmutableArray<Diagnostic> Diagnostics, string? Source) Run(
        IIncrementalGenerator generator,
        string payloadKey,
        string? payload,
        LanguageVersion languageVersion = LanguageVersion.CSharp12)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        if (payload is not null)
        {
            values[payloadKey] = payload;
        }

        var compilation = CSharpCompilation.Create(
            "AssemblyMeta.GeneratorTests.Input",
            new[] { CSharpSyntaxTree.ParseText("namespace Host; internal static class Marker {}") },
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver
            .WithUpdatedParseOptions(new CSharpParseOptions(languageVersion))
            .WithUpdatedAnalyzerConfigOptions(new TestAnalyzerConfigOptionsProvider(values));
        driver = driver.RunGenerators(compilation);
        var result = driver.GetRunResult();
        var generated = result.Results[0].GeneratedSources;
        return (result.Diagnostics, generated.Length == 0 ? null : generated[0].SourceText.ToString());
    }

    public static string ExecuteConstant(string source, string typeName, string memberName, LanguageVersion languageVersion = LanguageVersion.CSharp12)
    {
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(languageVersion));
        var compilation = CSharpCompilation.Create(
            "AssemblyMeta.Generated.Probe",
            new[] { tree },
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var pe = new MemoryStream();
        var emit = compilation.Emit(pe);
        if (!emit.Success)
        {
            var errors = string.Join(Environment.NewLine, emit.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
            throw new InvalidOperationException(errors + Environment.NewLine + source);
        }

        var assembly = Assembly.Load(pe.ToArray());
        var type = assembly.GetType(typeName, throwOnError: true)!;
        var field = type.GetField(memberName, BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
        if (field is null)
        {
            throw new MissingFieldException(typeName, memberName);
        }

        return field.GetValue(null)?.ToString() ?? "";
    }

    public static object ExecuteMember(string source, string typeName, string memberName, bool method, params (string Name, byte[] Content)[] manifestResources)
    {
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp12));
        var compilation = CSharpCompilation.Create(
            "AssemblyMeta.Generated.Probe." + Guid.NewGuid().ToString("N"),
            new[] { tree },
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var descriptions = manifestResources.Select(resource => new ResourceDescription(
            resource.Name,
            () => new MemoryStream(resource.Content, writable: false),
            isPublic: true)).ToArray();
        using var pe = new MemoryStream();
        var emit = compilation.Emit(pe, manifestResources: descriptions);
        if (!emit.Success)
        {
            var errors = string.Join(Environment.NewLine, emit.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
            throw new InvalidOperationException(errors + Environment.NewLine + source);
        }

        var assembly = Assembly.Load(pe.ToArray());
        var type = assembly.GetType(typeName, throwOnError: true)!;
        if (method)
        {
            var found = type.GetMethod(memberName, BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
            if (found is null)
            {
                throw new MissingMethodException(typeName, memberName);
            }

            return found.Invoke(null, null)!;
        }

        var property = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
        if (property is null)
        {
            throw new MissingMemberException(typeName, memberName);
        }

        return property.GetValue(null)!;
    }

    public static void AssertCached(IIncrementalGenerator generator, string payloadKey, string payload, string changedPayload)
    {
        var values = new Dictionary<string, string>
        {
            [payloadKey] = payload,
        };
        var provider = new TestAnalyzerConfigOptionsProvider(values);
        var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp12);
        var compilation = CSharpCompilation.Create(
            "incremental",
            new[] { CSharpSyntaxTree.ParseText("class A {}") },
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new[] { generator.AsSourceGenerator() },
            additionalTexts: null,
            parseOptions: parseOptions,
            optionsProvider: provider,
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

        driver = driver.RunGenerators(compilation);
        var first = driver.GetRunResult().Results[0].GeneratedSources[0].SourceText.ToString();

        var updated = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("class B {}"));
        driver = driver.RunGenerators(updated);
        var secondResult = driver.GetRunResult();
        Assert.Equal(first, secondResult.Results[0].GeneratedSources[0].SourceText.ToString());

        var reasons = secondResult.Results[0].TrackedSteps
            .SelectMany(pair => pair.Value)
            .SelectMany(step => step.Outputs)
            .Select(output => output.Reason)
            .ToArray();
        Assert.Contains(IncrementalStepRunReason.Cached, reasons);
        Assert.DoesNotContain(IncrementalStepRunReason.Modified, reasons);

        driver = driver.WithUpdatedAnalyzerConfigOptions(new TestAnalyzerConfigOptionsProvider(new Dictionary<string, string>
        {
            [payloadKey] = changedPayload,
        }));
        driver = driver.RunGenerators(updated);
        var third = driver.GetRunResult();
        var changedReasons = third.Results[0].TrackedSteps
            .SelectMany(pair => pair.Value)
            .SelectMany(step => step.Outputs)
            .Select(output => output.Reason)
            .ToArray();
        Assert.Contains(IncrementalStepRunReason.Modified, changedReasons);
        Assert.NotEqual(first, third.Results[0].GeneratedSources[0].SourceText.ToString());
    }

    private static MetadataReference[] CreateReferences()
    {
        var directory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        return new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(directory, "System.Runtime.dll")),
        };
    }
}
