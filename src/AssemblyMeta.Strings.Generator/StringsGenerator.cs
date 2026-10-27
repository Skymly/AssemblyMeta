using System;
using System.Text;
using System.Threading;
using AssemblyMeta.Core;
using AssemblyMeta.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace AssemblyMeta.Strings;

/// <summary>
/// resx 经 AdditionalFiles 直读，保存即刷新；标量（命名空间、可见性）经 build_property。
/// 卫星过滤与 AssemblyMetaStrings 排除在这里按 build_metadata.AdditionalFiles.* 逐项判定。
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class StringsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var files = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Select(static (pair, cancellation) => Read(pair.Left, pair.Right, cancellation))
            .Collect();

        var scalars = context.AnalyzerConfigOptionsProvider.Select(static (provider, _) =>
            new ValueTuple<string?, string?>(
                provider.GlobalOptions.TryGetValue("build_property.AssemblyMetaNamespace", out var ns) ? ns : null,
                provider.GlobalOptions.TryGetValue("build_property.AssemblyMetaVisibility", out var visibility) ? visibility : null));

        var language = context.ParseOptionsProvider.Select(static (options, _) =>
            options is CSharpParseOptions csharp
                ? (int)csharp.LanguageVersion
                : -1);

        context.RegisterSourceOutput(files.Combine(scalars).Combine(language), static (output, input) =>
        {
            var ((resxFiles, scalars), languageVersion) = input;
            if (languageVersion < 0)
            {
                return;
            }

            var result = StringsSource.Emit(scalars.Item1, scalars.Item2, resxFiles, languageVersion);
            foreach (var diagnostic in result.Diagnostics)
            {
                output.ReportDiagnostic(GeneratorHost.Create(diagnostic));
            }

            if (result.Source is not null)
            {
                output.AddSource("AssemblyMeta.Strings.g.cs", SourceText.From(result.Source, Encoding.UTF8));
            }
        });
    }

    private static ResxFileInput Read(AdditionalText file, AnalyzerConfigOptionsProvider provider, CancellationToken cancellation)
        => new(
            file.Path,
            file.GetText(cancellation)?.ToString(),
            Metadata(provider, file, "Link"),
            Metadata(provider, file, "RelativeDir"),
            Metadata(provider, file, "Filename"),
            Metadata(provider, file, "Extension"),
            Metadata(provider, file, "WithCulture"),
            Metadata(provider, file, "Culture"),
            Metadata(provider, file, "AssemblyMetaStrings"));

    private static string? Metadata(AnalyzerConfigOptionsProvider provider, AdditionalText file, string name)
        => provider.GetOptions(file).TryGetValue("build_metadata.AdditionalFiles." + name, out var value)
            ? value
            : null;
}
