using System;
using System.Collections.Generic;
using AssemblyMeta.Core.Emit;

namespace AssemblyMeta.Core;

/// <summary>一个 resx 源文件连同生成期需要的项元数据。Xml 为 null 视为无法读取。</summary>
public sealed record ResxFileInput(
    string Path,
    string? Xml,
    string? Link,
    string? RelativeDir,
    string? Filename,
    string? Extension,
    string? WithCulture,
    string? Culture,
    string? AssemblyMetaStrings);

/// <summary>把 resx 文件直接收成 Strings 的 C# 源码和诊断。不读文件，不接 MSBuild 或 Roslyn。</summary>
public static class StringsSource
{
    public static EmitResult Emit(string? ns, string? visibility, IReadOnlyList<ResxFileInput> files, int csharpLanguageVersion)
    {
        var diagnostics = new List<DiagnosticReport>();
        var inputs = new List<StringNameInput>();
        foreach (var file in files ?? Array.Empty<ResxFileInput>())
        {
            if (IsFalse(file.AssemblyMetaStrings))
            {
                continue;
            }

            if (!FileConstantNaming.TryCreate(
                    new FileConstantNaming.Input(
                        file.RelativeDir ?? "",
                        file.Filename ?? "",
                        file.Extension ?? "",
                        file.Link ?? "",
                        ""),
                    out var namedPath))
            {
                inputs.Add(new StringNameInput { OutsideWithoutLink = true, Spec = file.Path });
                continue;
            }

            var withCulture = file.WithCulture;
            if (IsTrue(withCulture) || (string.IsNullOrEmpty(withCulture) && StringResourceNaming.IsSatellitePath(namedPath.Value)))
            {
                continue;
            }

            if (file.Xml is null)
            {
                diagnostics.Add(new DiagnosticReport(
                    "AM0011",
                    "Cannot read resx / resx 无法读取",
                    "Cannot read resx " + file.Path + ". / 无法读取 resx " + file.Path + "。",
                    DiagnosticSeverityKind.Error) { File = file.Path });
                continue;
            }

            if (!ResxReader.TryRead(file.Xml, out var entries, out var skipped, out var error))
            {
                diagnostics.Add(new DiagnosticReport(
                    "AM0011",
                    "Cannot parse resx / resx 无法解析",
                    "Cannot parse resx " + file.Path + ": " + (error ?? "") + " / 无法解析 resx " + file.Path + "：" + (error ?? ""),
                    DiagnosticSeverityKind.Error) { File = file.Path });
                continue;
            }

            foreach (var skippedName in skipped)
            {
                diagnostics.Add(new DiagnosticReport(
                    "AM0007",
                    "Skipped non-string entry / 已跳过非字符串项",
                    "Skipped non-string entry “" + skippedName + "” in resx. / 已跳过 resx 中的非字符串项“" + skippedName + "”。",
                    DiagnosticSeverityKind.Warning) { File = file.Path });
            }

            foreach (var entry in entries)
            {
                inputs.Add(new StringNameInput
                {
                    LogicalPath = namedPath.Value,
                    Key = entry.Name,
                    Value = entry.Value,
                    Comment = entry.Comment,
                    Spec = file.Path,
                });
            }
        }

        var named = MemberNames.Name(new MemberNameRequest
        {
            Area = MemberNameArea.Strings,
            Strings = inputs,
        });
        diagnostics.AddRange(named.TaskDiagnostics);

        MemberVisibilityParser.TryParse(visibility, out var parsed, out var recognized);
        if (!recognized)
        {
            diagnostics.Add(new DiagnosticReport(
                "AM0012",
                "Unrecognized AssemblyMetaVisibility / AssemblyMetaVisibility 无法识别",
                "Unrecognized AssemblyMetaVisibility “" + visibility + "”; fell back to internal. / 无法识别 AssemblyMetaVisibility“" + visibility + "”，已使用 internal。",
                DiagnosticSeverityKind.Warning));
        }

        var records = new List<ConstantRecord>(named.Members.Count);
        foreach (var member in named.Members)
        {
            records.Add(new ConstantRecord(member.Name, member.Value, member.Type, member.Comment));
        }

        var emitted = new CSharpSourceEmitter().EmitStrings(
            new ConstantsModel
            {
                Namespace = ns,
                Visibility = parsed,
                Constants = records,
                NamesAreFinal = true,
                NamingDiagnostics = named.GeneratorDiagnostics,
            },
            csharpLanguageVersion);
        diagnostics.AddRange(emitted.Diagnostics);
        return new EmitResult { Source = emitted.Source, Diagnostics = diagnostics };
    }

    private static bool IsTrue(string? value)
        => string.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase);

    private static bool IsFalse(string? value)
        => string.Equals(value?.Trim(), "false", StringComparison.OrdinalIgnoreCase);
}
