using System.Text;
using AssemblyMeta.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace AssemblyMeta.Generators;

internal static class GeneratorHost
{
    public static void Register(
        IncrementalGeneratorInitializationContext context,
        string payloadKey,
        string fileName,
        SourceKind kind)
    {
        var payload = context.AnalyzerConfigOptionsProvider.Select((provider, _) =>
            provider.GlobalOptions.TryGetValue(payloadKey, out var value) ? value : null);

        var language = context.ParseOptionsProvider.Select(static (options, _) =>
            options is CSharpParseOptions csharp
                ? (int)csharp.LanguageVersion
                : -1);

        context.RegisterSourceOutput(payload.Combine(language), (output, pair) =>
        {
            var (encoded, languageVersion) = pair;
            if (encoded is null || languageVersion < 0)
            {
                return;
            }

            var result = SourceEmission.Emit(encoded, kind, languageVersion);
            foreach (var diagnostic in result.Diagnostics)
            {
                output.ReportDiagnostic(Create(diagnostic));
            }

            if (result.Source is not null)
            {
                output.AddSource(fileName, SourceText.From(result.Source, Encoding.UTF8));
            }
        });
    }

    private static Diagnostic Create(DiagnosticReport report)
    {
        var severity = report.Severity switch
        {
            DiagnosticSeverityKind.Warning => DiagnosticSeverity.Warning,
            DiagnosticSeverityKind.Info => DiagnosticSeverity.Info,
            DiagnosticSeverityKind.Hidden => DiagnosticSeverity.Hidden,
            _ => DiagnosticSeverity.Error,
        };
        var descriptor = new DiagnosticDescriptor(
            report.Id,
            report.Title,
            "{0}",
            "AssemblyMeta",
            severity,
            isEnabledByDefault: true);
        return Diagnostic.Create(descriptor, Location.None, report.Message);
    }
}
