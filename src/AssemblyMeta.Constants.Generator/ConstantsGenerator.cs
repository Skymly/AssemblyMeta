using System.Text;
using AssemblyMeta.Core;
using AssemblyMeta.Core.Emit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace AssemblyMeta.Constants;

/// <summary>
/// 从 build_property.AssemblyMeta_ConstantsPayload 生成 AssemblyMeta.Constants。
/// 不读取 AdditionalFiles，也不枚举 build_metadata，保证增量步骤只比较载荷字符串。
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class ConstantsGenerator : IIncrementalGenerator
{
    public const string PayloadKey = "build_property.AssemblyMeta_ConstantsPayload";

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

            if (!PayloadCodec.TryDecodeConstants(encoded, out var model))
            {
                output.ReportDiagnostic(Diagnostic.Create(Diagnostics.CorruptPayload, Location.None, "Constants"));
                return;
            }

            var emitter = EmitterRegistry.Shared.TryResolve(SourceLanguage.CSharp);
            if (emitter is null)
            {
                return;
            }

            var result = emitter.EmitConstants(model, languageVersion);
            foreach (var diagnostic in result.Diagnostics)
            {
                output.ReportDiagnostic(Diagnostics.Create(diagnostic));
            }

            if (result.Source is not null)
            {
                output.AddSource("AssemblyMeta.Constants.g.cs", SourceText.From(result.Source, Encoding.UTF8));
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
