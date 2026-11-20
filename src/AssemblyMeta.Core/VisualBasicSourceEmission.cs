using System;
using System.Collections.Generic;
using AssemblyMeta.Core.Emit;

namespace AssemblyMeta.Core;

/// <summary>把载荷字符串收成 Visual Basic 源码和诊断。不读文件，不按语言分派。C# 仍走 SourceEmission.Emit。</summary>
public static class VisualBasicSourceEmission
{
    public static EmitResult Emit(string payload, SourceKind kind)
    {
        if (payload is null)
        {
            throw new ArgumentNullException(nameof(payload));
        }

        if (!Enum.IsDefined(typeof(SourceKind), kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        var emitter = new VisualBasicSourceEmitter();
        switch (kind)
        {
            case SourceKind.AssemblyInfo:
                return PayloadCodec.TryDecodeAssemblyInfo(payload, out var assemblyInfo)
                    ? emitter.EmitAssemblyInfo(assemblyInfo)
                    : Invalid(kind);
            case SourceKind.Constants:
                return PayloadCodec.TryDecodeConstants(payload, out var constants)
                    ? emitter.EmitConstants(constants)
                    : Invalid(kind);
            case SourceKind.Project:
                return PayloadCodec.TryDecodeProject(payload, out var project)
                    ? emitter.EmitProject(project)
                    : Invalid(kind);
            case SourceKind.Metadata:
                return PayloadCodec.TryDecodeMetadata(payload, out var metadata)
                    ? emitter.EmitMetadata(metadata)
                    : Invalid(kind);
            case SourceKind.Git:
                return PayloadCodec.TryDecodeGit(payload, out var git)
                    ? emitter.EmitGit(git)
                    : Invalid(kind);
            case SourceKind.Resources:
                return PayloadCodec.TryDecodeResources(payload, out var resources)
                    ? emitter.EmitResources(resources)
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