using System;

namespace AssemblyMeta.Core;

/// <summary>要生成的源语言。当前只实现 C#，VB/F# 仅保留扩展点。</summary>
public enum SourceLanguage
{
    Unknown = 0,
    CSharp = 1,
    VisualBasic = 2,
    FSharp = 3,
}

/// <summary>生成类型的可见性。public 时成员改为 static readonly，避免调用方把常量值烤进程序集。</summary>
public enum MemberVisibility
{
    Internal = 0,
    Public = 1,
}

/// <summary>不依赖 Roslyn 的诊断严重级别。</summary>
public enum DiagnosticSeverityKind
{
    Hidden = 0,
    Info = 1,
    Warning = 2,
    Error = 3,
}

/// <summary>生成器要上报的诊断。消息已经格式化，不再包含占位符。</summary>
public readonly record struct DiagnosticReport(string Id, string Title, string Message, DiagnosticSeverityKind Severity);

/// <summary>一条尚未消毒的常量。Name 使用点号表示嵌套。</summary>
public sealed record ConstantRecord(string Name, string Value, string Type, string Comment)
{
    public string EffectiveType => string.IsNullOrWhiteSpace(Type) ? "string" : Type.Trim();
}

/// <summary>AssemblyInfo 生成模型。</summary>
public sealed class AssemblyInfoModel
{
    public string? Namespace { get; init; }

    public MemberVisibility Visibility { get; init; }

    public string Configuration { get; init; } = "";

    public string Company { get; init; } = "";

    public string Title { get; init; } = "";

    public string Description { get; init; } = "";

    public string Product { get; init; } = "";

    public string Copyright { get; init; } = "";

    public string Version { get; init; } = "";

    public string InformationalVersion { get; init; } = "";

    public string FileVersion { get; init; } = "";
}

/// <summary>Constants 生成模型。</summary>
public sealed class ConstantsModel
{
    public string? Namespace { get; init; }

    public MemberVisibility Visibility { get; init; }

    public IReadOnlyList<ConstantRecord> Constants { get; init; } = Array.Empty<ConstantRecord>();
}

/// <summary>Git 生成模型。设计时未查询时，五个字符串是占位符而不是空值。</summary>
public sealed class GitModel
{
    public string? Namespace { get; init; }

    public MemberVisibility Visibility { get; init; }

    public string Commit { get; init; } = "";

    public string Sha { get; init; } = "";

    public string Root { get; init; } = "";

    public string Url { get; init; } = "";

    public string Branch { get; init; } = "";

    /// <summary>真实构建已查询，但没有提交号。生成器应报 AM0006，仍输出成员。</summary>
    public bool Unavailable { get; init; }
}

/// <summary>一次生成结果。Source 为 null 表示不输出文件。</summary>
public sealed class EmitResult
{
    public string? Source { get; init; }

    public IReadOnlyList<DiagnosticReport> Diagnostics { get; init; } = Array.Empty<DiagnosticReport>();

    public static EmitResult Empty { get; } = new();
}
