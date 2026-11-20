using System.Text;
using AssemblyMeta.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;

namespace AssemblyMeta.Generators;

internal static class VisualBasicGeneratorHost
{
    /// <summary>增量输入只有分析器全局选项里的载荷字符串。缺载荷时不调用发射入口。</summary>
    public static void Register(
        IncrementalGeneratorInitializationContext context,
        string payloadKey,
        string fileName,
        SourceKind kind)
    {
        var payload = context.AnalyzerConfigOptionsProvider.Select((provider, _) =>
            provider.GlobalOptions.TryGetValue(payloadKey, out var value) ? value : null);

        var language = context.ParseOptionsProvider.Select(static (options, _) =>
            options is VisualBasicParseOptions ? 1 : -1);

        context.RegisterSourceOutput(payload.Combine(language), (output, pair) =>
        {
            var (encoded, languageVersion) = pair;
            if (encoded is null || languageVersion < 0)
            {
                return;
            }

            var result = VisualBasicSourceEmission.Emit(encoded, kind);
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

    internal static Diagnostic Create(DiagnosticReport report)
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
        var location = string.IsNullOrEmpty(report.File)
            ? Location.None
            : Location.Create(report.File!, default, default);
        return Diagnostic.Create(descriptor, location, report.Message);
    }
}