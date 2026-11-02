using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using AssemblyMeta.Constants.VisualBasic;
using AssemblyMeta.Core;
using AssemblyMeta.Resources.VisualBasic;
using AssemblyMeta.Strings.VisualBasic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;

namespace AssemblyMeta.Generator.Tests;

public class VisualBasicEmissionTests
{
    [Fact]
    public void Same_constants_resx_and_resource_values_match_csharp()
    {
        var constants = PayloadCodec.EncodeConstants(
            "Sample",
            MemberVisibility.Internal,
            new ConstantRecord[]
            {
                new("Banner", "line1\r\nline2", "string", ""),
                new("Weird", "a;b#c \"q\"", "string", ""),
                new("Http.TimeoutSeconds", "30", "int", "请求超时（秒）"),
                new("Ratio", "1.5f", "float", ""),
                new("Enabled", "true", "bool", ""),
            },
            Array.Empty<DiagnosticReport>());
        var csharpConstants = SourceEmission.Emit(constants, SourceKind.Constants, 1200);
        var visualBasicConstants = VisualBasicSourceEmission.Emit(constants, SourceKind.Constants);
        Assert.Contains("static partial class AssemblyMeta", csharpConstants.Source);
        Assert.DoesNotContain("Partial Module", csharpConstants.Source);
        Assert.Contains("Friend Partial Module AssemblyMeta", visualBasicConstants.Source);
        Assert.Contains("Option Strict On", visualBasicConstants.Source);
        Assert.DoesNotContain("Imports ", visualBasicConstants.Source);
        Assert.DoesNotContain(visualBasicConstants.Diagnostics, item => item.Severity == DiagnosticSeverityKind.Error);

        var csharpValues = ReadConstants(CompileCSharp(csharpConstants.Source!, "CSharpConstants"));
        var visualBasicValues = ReadConstants(CompileVisualBasic(visualBasicConstants.Source!, "VisualBasicConstants"));
        Assert.Equal("line1\nline2", csharpValues["Banner"]);
        Assert.Equal(csharpValues["Banner"], visualBasicValues["Banner"]);
        Assert.Equal(csharpValues["Weird"], visualBasicValues["Weird"]);
        Assert.Equal(csharpValues["TimeoutSeconds"], visualBasicValues["TimeoutSeconds"]);
        Assert.Equal(csharpValues["Ratio"], visualBasicValues["Ratio"]);
        Assert.Equal(csharpValues["Enabled"], visualBasicValues["Enabled"]);

        const string resx = """
            <?xml version="1.0" encoding="utf-8"?>
            <root>
              <data name="Hello" xml:space="preserve"><value>你好</value></data>
              <data name="Greeting" xml:space="preserve"><value>你好，{0}</value></data>
            </root>
            """;
        var files = new[]
        {
            new ResxFileInput("Strings.resx", resx, null, "", "Strings", ".resx", "false", "", null),
            new ResxFileInput("Strings.fr.resx", "<root><data name=\"Skip\" xml:space=\"preserve\"><value>no</value></data></root>", null, "", "Strings.fr", ".resx", "", "fr", null),
        };
        var csharpStrings = StringsSource.Emit("Sample", "internal", files, 1200);
        var visualBasicStrings = StringsSource.EmitVisualBasic("Sample", "internal", files);
        Assert.DoesNotContain("Skip", visualBasicStrings.Source);
        var csharpHello = ReadConstants(CompileCSharp(csharpStrings.Source!, "CSharpStrings"))["Hello"];
        var visualBasicHello = ReadConstants(CompileVisualBasic(visualBasicStrings.Source!, "VisualBasicStrings"))["Hello"];
        Assert.Equal("你好", csharpHello);
        Assert.Equal(csharpHello, visualBasicHello);
        Assert.Equal(
            ReadConstants(CompileCSharp(csharpStrings.Source!, "CSharpGreeting"))["Greeting"],
            ReadConstants(CompileVisualBasic(visualBasicStrings.Source!, "VisualBasicGreeting"))["Greeting"]);

        var payload = PayloadCodec.EncodeResources(
            "Sample",
            MemberVisibility.Internal,
            new[] { new ResourceRecord("Content.Note", "Sample.Note.txt", "") },
            Array.Empty<DiagnosticReport>());
        var bytes = Encoding.UTF8.GetBytes("仅 Resources 包。\n");
        var csharpResource = SourceEmission.Emit(payload, SourceKind.Resources, 1200);
        var visualBasicResource = VisualBasicSourceEmission.Emit(payload, SourceKind.Resources);
        Assert.Contains("Function GetBytes", visualBasicResource.Source);
        Assert.Contains("Function GetStream", visualBasicResource.Source);
        Assert.Contains("Property Text", visualBasicResource.Source);
        Assert.DoesNotContain("Imports ", visualBasicResource.Source);
        var csharpText = ReadResourceText(CompileCSharp(csharpResource.Source!, "CSharpResources", "Sample.Note.txt", bytes));
        var visualBasicText = ReadResourceText(CompileVisualBasic(visualBasicResource.Source!, "VisualBasicResources", "Sample.Note.txt", bytes));
        Assert.Equal("仅 Resources 包。\n", csharpText);
        Assert.Equal(csharpText, visualBasicText);
    }

    [Fact]
    public void Case_only_names_and_unmapped_types_are_diagnostics_not_illegal_source()
    {
        var payload = PayloadCodec.EncodeConstants(
            "Sample",
            MemberVisibility.Internal,
            new ConstantRecord[]
            {
                new("Foo", "one", "string", ""),
                new("foo", "two", "string", ""),
                new("When", "kept", "string", ""),
                new("Dim", "keyword", "string", ""),
                new("Later", "2026-11-02", "DateTime", ""),
                new("Count", "1 + 2", "int", ""),
            },
            Array.Empty<DiagnosticReport>());
        var result = VisualBasicSourceEmission.Emit(payload, SourceKind.Constants);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "AM0001");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "AM0013" && diagnostic.Message.Contains("DateTime"));
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "AM0013" && diagnostic.Message.Contains("1 + 2"));
        Assert.DoesNotContain("DateTime", result.Source);
        Assert.DoesNotContain("1 + 2", result.Source);
        Assert.Contains("Public Const Foo As String = \"one\"", result.Source);
        Assert.DoesNotContain("foo", result.Source, StringComparison.Ordinal);
        Assert.Contains("[Dim]", result.Source);
        Assert.Contains("[When]", result.Source);
        CompileVisualBasic(result.Source!, "VisualBasicRejected");

        var resource = PayloadCodec.EncodeResources(
            "Sample",
            MemberVisibility.Internal,
            new[] { new ResourceRecord("text", "Sample.text", "") },
            Array.Empty<DiagnosticReport>());
        var resourceResult = VisualBasicSourceEmission.Emit(resource, SourceKind.Resources);
        Assert.Contains(resourceResult.Diagnostics, diagnostic => diagnostic.Id == "AM0001");
        Assert.DoesNotContain("Class text", resourceResult.Source);
        Assert.DoesNotContain("Class [text]", resourceResult.Source);
        CompileVisualBasic(resourceResult.Source!, "VisualBasicResourceRejected");
    }

    [Fact]
    public void Visual_basic_generators_emit_vb_and_csharp_emit_stays_csharp()
    {
        var payload = PayloadCodec.EncodeConstants("Sample", MemberVisibility.Internal, new[] { new ConstantRecord("Banner", "line1\nline2", "string", "") }, Array.Empty<DiagnosticReport>());
        var visualBasic = RunVisualBasic(new ConstantsGenerator(), ConstantsGenerator.PayloadKey, payload);
        Assert.EndsWith(".g.vb", visualBasic.HintName, StringComparison.Ordinal);
        Assert.Contains("Partial Module AssemblyMeta", visualBasic.Source);
        CompileVisualBasic(visualBasic.Source, "GeneratorConstants");

        var csharp = GeneratorTestHost.Run(new AssemblyMeta.Constants.ConstantsGenerator(), AssemblyMeta.Constants.ConstantsGenerator.PayloadKey, payload);
        Assert.Contains("static partial class AssemblyMeta", csharp.Source);
        Assert.DoesNotContain(".g.vb", csharp.Source);

        var resx = """
            <root><data name="Hello" xml:space="preserve"><value>你好</value></data></root>
            """;
        const string stringsPath = @"C:\repo\Strings.resx";
        var strings = RunVisualBasicStrings(
            new StringsGenerator(),
            new[] { (stringsPath, (string?)resx) },
            new Dictionary<string, string> { ["build_property.AssemblyMetaNamespace"] = "Sample" },
            new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                [stringsPath] = new Dictionary<string, string>
                {
                    ["build_metadata.AdditionalFiles.RelativeDir"] = "",
                    ["build_metadata.AdditionalFiles.Filename"] = "Strings",
                    ["build_metadata.AdditionalFiles.Extension"] = ".resx",
                },
            });
        Assert.Equal("AssemblyMeta.Strings.g.vb", strings.HintName);
        Assert.Contains("你好", strings.Source);
        CompileVisualBasic(strings.Source, "GeneratorStrings");
    }

    [Fact]
    public void CSharp_generator_projects_do_not_reference_visual_basic()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var projects = Directory.GetFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories)
            .Where(path => path.Contains(".Generator", StringComparison.Ordinal) && !path.Contains("VisualBasic", StringComparison.Ordinal));
        Assert.NotEmpty(projects);
        foreach (var project in projects)
        {
            var text = File.ReadAllText(project);
            Assert.DoesNotContain("Microsoft.CodeAnalysis.VisualBasic", text);
        }

        var emission = File.ReadAllText(Path.Combine(root, "src", "AssemblyMeta.Core", "SourceEmission.cs"));
        Assert.Contains("new CSharpSourceEmitter()", emission);
        Assert.DoesNotContain("VisualBasicSourceEmitter", emission);
    }

    private static Dictionary<string, object?> ReadConstants(Assembly assembly)
    {
        var module = assembly.GetTypes().Single(type => type.Name == "AssemblyMeta");
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        Collect(module, values);
        return values;
    }

    private static void Collect(Type type, Dictionary<string, object?> values)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        foreach (var field in type.GetFields(flags))
        {
            if (field.IsLiteral || field.IsInitOnly)
            {
                values[field.Name] = field.GetValue(null);
            }
        }

        foreach (var nested in type.GetNestedTypes(flags))
        {
            Collect(nested, values);
        }
    }

    private static string ReadResourceText(Assembly assembly)
    {
        var module = assembly.GetTypes().Single(type => type.Name == "AssemblyMeta");
        var resources = module.GetNestedType("Resources", BindingFlags.Public | BindingFlags.NonPublic)!;
        var note = resources.GetNestedType("Content", BindingFlags.Public | BindingFlags.NonPublic)!
            .GetNestedType("Note", BindingFlags.Public | BindingFlags.NonPublic)!;
        return (string)note.GetProperty("Text", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
    }

    private static Assembly CompileCSharp(string source, string assemblyName, string? resourceName = null, byte[]? resourceBytes = null)
    {
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12));
        var compilation = CSharpCompilation.Create(
            assemblyName,
            new[] { tree },
            References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        return Emit(compilation, assemblyName, resourceName, resourceBytes);
    }

    private static Assembly CompileVisualBasic(string source, string assemblyName, string? resourceName = null, byte[]? resourceBytes = null)
    {
        var options = new VisualBasicCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optionStrict: OptionStrict.On,
                optionExplicit: true,
                optionInfer: true)
            .WithGlobalImports(GlobalImport.Parse("Microsoft.VisualBasic"));
        var tree = VisualBasicSyntaxTree.ParseText(source);
        var compilation = VisualBasicCompilation.Create(assemblyName, new[] { tree }, References(), options);
        return Emit(compilation, assemblyName, resourceName, resourceBytes);
    }

    private static Assembly Emit(Compilation compilation, string assemblyName, string? resourceName, byte[]? resourceBytes)
    {
        using var pe = new MemoryStream();
        var resources = resourceName is null
            ? null
            : new[] { new ResourceDescription(resourceName, () => new MemoryStream(resourceBytes!), isPublic: true) };
        var result = compilation.Emit(pe, manifestResources: resources);
        if (!result.Success)
        {
            var errors = string.Join("\n", result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
            throw new Xunit.Sdk.XunitException(assemblyName + " failed to compile:\n" + errors + "\n" + compilation.SyntaxTrees.First().ToString());
        }

        pe.Position = 0;
        return Assembly.Load(pe.ToArray());
    }

    private static MetadataReference[] References()
    {
        var directory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        return new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(directory, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(directory, "Microsoft.VisualBasic.dll")),
            MetadataReference.CreateFromFile(Path.Combine(directory, "Microsoft.VisualBasic.Core.dll")),
        };
    }

    private static (string HintName, string Source) RunVisualBasic(IIncrementalGenerator generator, string payloadKey, string payload)
    {
        var compilation = VisualBasicCompilation.Create(
            "input",
            new[] { VisualBasicSyntaxTree.ParseText("Friend Module Marker\nEnd Module\n") },
            References(),
            new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator.AsSourceGenerator()));
        driver = driver.WithUpdatedParseOptions(VisualBasicParseOptions.Default).WithUpdatedAnalyzerConfigOptions(new TestAnalyzerConfigOptionsProvider(new Dictionary<string, string> { [payloadKey] = payload }));
        driver = driver.RunGenerators(compilation);
        var generated = driver.GetRunResult().Results[0].GeneratedSources;
        Assert.Single(generated);
        return (generated[0].HintName, generated[0].SourceText.ToString());
    }

    private static (string HintName, string Source) RunVisualBasicStrings(
        IIncrementalGenerator generator,
        IReadOnlyList<(string Path, string? Text)> files,
        IReadOnlyDictionary<string, string> global,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> fileOptions)
    {
        var additional = files.Select(file => (AdditionalText)new InMemoryAdditionalText(file.Path, file.Text)).ToImmutableArray();
        var compilation = VisualBasicCompilation.Create(
            "input",
            new[] { VisualBasicSyntaxTree.ParseText("Friend Module Marker\nEnd Module\n") },
            References(),
            new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator.AsSourceGenerator()));
        driver = driver
            .WithUpdatedParseOptions(VisualBasicParseOptions.Default)
            .WithUpdatedAnalyzerConfigOptions(new TestAnalyzerConfigOptionsProvider(global, fileOptions))
            .ReplaceAdditionalTexts(additional);
        driver = driver.RunGenerators(compilation);
        var generated = driver.GetRunResult().Results[0].GeneratedSources;
        Assert.Single(generated);
        return (generated[0].HintName, generated[0].SourceText.ToString());
    }
}