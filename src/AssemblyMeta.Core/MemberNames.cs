using System;
using System.Collections.Generic;

namespace AssemblyMeta.Core;

public enum MemberNameArea
{
    Constants,
    Resources,
    Strings,
}

public sealed class ConstantNameInput
{
    public string? Name { get; init; }

    public string? Value { get; init; }

    public string? Type { get; init; }

    public string? Comment { get; init; }
}

public sealed class FileConstantNameInput
{
    public string? RelativeDir { get; init; }

    public string? Filename { get; init; }

    public string? Extension { get; init; }

    public string? Link { get; init; }

    public string? Comment { get; init; }
}

public sealed class MemberNameRequest
{
    public MemberNameArea Area { get; init; }

    public IReadOnlyList<ConstantNameInput> Constants { get; init; } = Array.Empty<ConstantNameInput>();

    public IReadOnlyList<FileConstantNameInput> FileConstants { get; init; } = Array.Empty<FileConstantNameInput>();
}

public sealed class NamedMember
{
    public string Name { get; init; } = "";

    public string Value { get; init; } = "";

    public string Type { get; init; } = "string";

    public string Comment { get; init; } = "";

    public string? ManifestName { get; init; }
}

public sealed class MemberNameResult
{
    public IReadOnlyList<NamedMember> Members { get; init; } = Array.Empty<NamedMember>();

    public IReadOnlyList<DiagnosticReport> TaskDiagnostics { get; init; } = Array.Empty<DiagnosticReport>();

    public IReadOnlyList<DiagnosticReport> GeneratorDiagnostics { get; init; } = Array.Empty<DiagnosticReport>();
}

/// <summary>
/// 把已经读出的项收成消毒后的点号名，并带上诊断。不读文件，不接 MSBuild 或 Roslyn。
/// </summary>
public static class MemberNames
{
    public static MemberNameResult Name(MemberNameRequest request)
    {
        if (request.Area != MemberNameArea.Constants)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "这一刀只命名 Constants。");
        }

        var members = new List<NamedMember>();
        var generatorDiagnostics = new List<DiagnosticReport>();
        foreach (var item in request.Constants)
        {
            if (!TryDots(item.Name, out var name, out var invalidSegment))
            {
                var raw = item.Name ?? "";
                var message = string.IsNullOrWhiteSpace(raw)
                    ? "存在空的常量名，已忽略。"
                    : "常量“" + raw + "”包含无法映射为 C# 标识符的片段“" + invalidSegment + "”，已忽略。";
                generatorDiagnostics.Add(new DiagnosticReport("AM0002", "常量名无效", message, DiagnosticSeverityKind.Error));
                continue;
            }

            var type = string.IsNullOrWhiteSpace(item.Type) ? "string" : item.Type!.Trim();
            members.Add(new NamedMember
            {
                Name = name,
                Value = item.Value ?? "",
                Type = type,
                Comment = item.Comment ?? "",
            });
        }

        foreach (var item in request.FileConstants)
        {
            var created = FileConstantNaming.TryCreate(
                new FileConstantNaming.Input(
                    item.RelativeDir ?? "",
                    item.Filename ?? "",
                    item.Extension ?? "",
                    item.Link ?? "",
                    item.Comment ?? ""),
                out var record);
            if (!created)
            {
                continue;
            }

            if (!TryDots(record.Name, out var name, out var invalidSegment))
            {
                generatorDiagnostics.Add(new DiagnosticReport(
                    "AM0002",
                    "常量名无效",
                    "常量“" + record.Name + "”包含无法映射为 C# 标识符的片段“" + invalidSegment + "”，已忽略。",
                    DiagnosticSeverityKind.Error));
                continue;
            }

            members.Add(new NamedMember
            {
                Name = name,
                Value = record.Value,
                Type = "string",
                Comment = record.Comment,
            });
        }

        return new MemberNameResult
        {
            Members = members,
            GeneratorDiagnostics = generatorDiagnostics,
        };
    }

    private static bool TryDots(string? dotted, out string name, out string invalidSegment)
    {
        name = "";
        invalidSegment = "";
        if (string.IsNullOrWhiteSpace(dotted))
        {
            return false;
        }

        var segments = dotted!.Split('.');
        var identifiers = new string[segments.Length];
        for (var i = 0; i < segments.Length; i++)
        {
            if (!IdentifierSanitizer.TrySanitize(segments[i], out var identifier))
            {
                invalidSegment = segments[i];
                return false;
            }

            identifiers[i] = identifier;
        }

        name = string.Join(".", identifiers);
        return true;
    }
}
