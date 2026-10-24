using System;
using System.Collections.Generic;
using AssemblyMeta.Core.Emit;

namespace AssemblyMeta.Core;

public enum SourceKind
{
    AssemblyInfo,
    Constants,
    Project,
    Metadata,
    Git,
    Resources,
    Strings,
}

/// <summary>把载荷字符串收成 C# 源码和诊断。不读文件，不按语言分派。</summary>
public static class SourceEmission
{
    public static EmitResult Emit(string payload, SourceKind kind, int csharpLanguageVersion)
    {
        if (payload is null)
        {
            throw new ArgumentNullException(nameof(payload));
        }

        if (!Enum.IsDefined(typeof(SourceKind), kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        var emitter = new CSharpSourceEmitter();
        switch (kind)
        {
            case SourceKind.AssemblyInfo:
                return PayloadCodec.TryDecodeAssemblyInfo(payload, out var assemblyInfo)
                    ? emitter.EmitAssemblyInfo(assemblyInfo, csharpLanguageVersion)
                    : Invalid(kind);
            case SourceKind.Constants:
                return PayloadCodec.TryDecodeConstants(payload, out var constants)
                    ? emitter.EmitConstants(constants, csharpLanguageVersion)
                    : Invalid(kind);
            case SourceKind.Project:
                return PayloadCodec.TryDecodeProject(payload, out var project)
                    ? emitter.EmitProject(project, csharpLanguageVersion)
                    : Invalid(kind);
            case SourceKind.Metadata:
                return PayloadCodec.TryDecodeMetadata(payload, out var metadata)
                    ? emitter.EmitMetadata(metadata, csharpLanguageVersion)
                    : Invalid(kind);
            case SourceKind.Git:
                return PayloadCodec.TryDecodeGit(payload, out var git)
                    ? emitter.EmitGit(git, csharpLanguageVersion)
                    : Invalid(kind);
            case SourceKind.Resources:
                return PayloadCodec.TryDecodeResources(payload, out var resources)
                    ? emitter.EmitResources(resources, csharpLanguageVersion)
                    : Invalid(kind);
            case SourceKind.Strings:
                return PayloadCodec.TryDecodeStrings(payload, out var strings)
                    ? emitter.EmitStrings(strings, csharpLanguageVersion)
                    : Invalid(kind);
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    private static EmitResult Invalid(SourceKind kind)
        => new()
        {
            Source = null,
            Diagnostics = new List<DiagnosticReport>
            {
                new(
                    "AM0004",
                    "Invalid AssemblyMeta payload / AssemblyMeta 载荷无效",
                    "Could not parse the " + kind + " compile-time payload. Clean obj and rebuild. / 无法解析 " + kind + " 编译期载荷。请清理 obj 后重新生成。",
                    DiagnosticSeverityKind.Error),
            },
        };
}
