using System.Text;
using AssemblyMeta.Core;
using AssemblyMeta.Core.Emit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace AssemblyMeta.Metadata;

/// <summary>
/// 从 build_property.AssemblyMeta_MetadataPayload 生成 AssemblyMeta.Metadata。
/// 输入只有分析器配置和语言版本，避免源码变化击穿增量缓存。
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class MetadataGenerator : IIncrementalGenerator
{
    public const string PayloadKey = "build_property.AssemblyMeta_MetadataPayload";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var payload = context.AnalyzerConfigOptionsProvider.Select(static (provider, _) =>
            provider.GlobalOptions.TryGetValue(PayloadKey, out var value) ? value : null);

        var language = context.ParseOptionsProvider.Select(static (options, _) =>
            options is CSharpParseOptions csharp
                ? ((int)csharp.LanguageVersion)
                : -1);

        context.RegisterSourceOutput(payload.Combine(language), static (output, pair) =>
        {
            var (encoded, languageVersion) = pair;
            if (encoded is null || languageVersion < 0)
            {
                return;
            }

            if (!PayloadCodec.TryDecodeMetadata(encoded, out var model))
            {
                output.ReportDiagnostic(Diagnostic.Create(Diagnostics.CorruptPayload, Location.None, "Metadata"));
                return;
            }

            var emitter = EmitterRegistry.Shared.TryResolve(SourceLanguage.CSharp);
            if (emitter is null)
            {
                return;
            }

            var result = emitter.EmitMetadata(model, languageVersion);
            foreach (var diagnostic in result.Diagnostics)
            {
                output.ReportDiagnostic(Diagnostics.Create(diagnostic));
            }

            if (result.Source is not null)
            {
                output.AddSource("AssemblyMeta.Metadata.g.cs", SourceText.From(result.Source, Encoding.UTF8));
            }
        });
    }
}

internal static class Diagnostics
{
    public static readonly DiagnosticDescriptor CorruptPayload = new(
        "AM0004",
        "AssemblyMeta 载荷无效",
        "无法解析 {0} 编译期载荷。请清理 obj 后重新生成。",
        "AssemblyMeta",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static Diagnostic Create(DiagnosticReport report)
    {
        var severity = report.Severity switch
        {
            DiagnosticSeverityKind.Warning => DiagnosticSeverity.Warning,
            DiagnosticSeverityKind.Info => DiagnosticSeverity.Info,
            DiagnosticSeverityKind.Hidden => DiagnosticSeverity.Hidden,
            _ => DiagnosticSeverity.Error,
        };
        var descriptor = new DiagnosticDescriptor(report.Id, report.Title, "{0}", "AssemblyMeta", severity, isEnabledByDefault: true);
        return Diagnostic.Create(descriptor, Location.None, report.Message);
    }
}
