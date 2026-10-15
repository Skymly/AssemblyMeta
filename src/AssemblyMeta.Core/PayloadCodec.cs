using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AssemblyMeta.Core;

/// <summary>
/// 把任意字符串编码成 editorconfig 安全的 Base64 载荷。
/// Roslyn 的 GenerateMSBuildEditorConfig 不会转义属性值，换行、# 和分号都会截断。
/// </summary>
public static class PayloadCodec
{
    private static readonly byte[] AssemblyInfoMagic = { (byte)'A', (byte)'M', (byte)'A', (byte)'I' };
    private static readonly byte[] ConstantsMagic = { (byte)'A', (byte)'M', (byte)'C', (byte)'S' };
    private static readonly byte[] ProjectMagic = { (byte)'A', (byte)'M', (byte)'P', (byte)'R' };
    private static readonly byte[] MetadataMagic = { (byte)'A', (byte)'M', (byte)'M', (byte)'D' };
    private static readonly byte[] GitMagic = { (byte)'A', (byte)'M', (byte)'G', (byte)'T' };
    private static readonly byte[] ResourcesMagic = { (byte)'A', (byte)'M', (byte)'R', (byte)'S' };
    private static readonly byte[] StringsMagic = { (byte)'A', (byte)'M', (byte)'S', (byte)'T' };
    private const int Version = 1;

    public static string EncodeAssemblyInfo(AssemblyInfoModel model)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        return Encode(AssemblyInfoMagic, new[]
        {
            model.Namespace ?? "",
            VisibilityToken(model.Visibility),
            model.Configuration ?? "",
            model.Company ?? "",
            model.Title ?? "",
            model.Description ?? "",
            model.Product ?? "",
            model.Copyright ?? "",
            model.Version ?? "",
            model.InformationalVersion ?? "",
            model.FileVersion ?? "",
        });
    }

    public static bool TryDecodeAssemblyInfo(string? payload, out AssemblyInfoModel model)
    {
        model = new AssemblyInfoModel();
        if (!TryDecode(payload, AssemblyInfoMagic, out var fields) || fields.Count != 11)
        {
            return false;
        }

        if (!MemberVisibilityParser.TryParse(fields[1], out var visibility, out _))
        {
            visibility = MemberVisibility.Internal;
        }

        model = new AssemblyInfoModel
        {
            Namespace = NullIfEmpty(fields[0]),
            Visibility = visibility,
            Configuration = fields[2],
            Company = fields[3],
            Title = fields[4],
            Description = fields[5],
            Product = fields[6],
            Copyright = fields[7],
            Version = fields[8],
            InformationalVersion = fields[9],
            FileVersion = fields[10],
        };
        return true;
    }

    public static string EncodeConstants(string? ns, MemberVisibility visibility, IReadOnlyList<ConstantRecord> constants)
    {
        var fields = new List<string>
        {
            ns ?? "",
            VisibilityToken(visibility),
        };

        if (constants is not null)
        {
            foreach (var constant in constants)
            {
                fields.Add(constant.Name ?? "");
                fields.Add(constant.Value ?? "");
                fields.Add(constant.Type ?? "");
                fields.Add(constant.Comment ?? "");
            }
        }

        return Encode(ConstantsMagic, fields);
    }

    public static string EncodeConstants(string? ns, MemberVisibility visibility, IReadOnlyList<ConstantRecord> constants, IReadOnlyList<DiagnosticReport> namingDiagnostics)
    {
        var fields = new List<string>
        {
            ns ?? "",
            VisibilityToken(visibility),
        };
        AppendDiagnostics(fields, namingDiagnostics);
        if (constants is not null)
        {
            foreach (var constant in constants)
            {
                fields.Add(constant.Name ?? "");
                fields.Add(constant.Value ?? "");
                fields.Add(constant.Type ?? "");
                fields.Add(constant.Comment ?? "");
            }
        }

        return Encode(ConstantsMagic, fields, version: 2);
    }

    public static bool TryDecodeConstants(string? payload, out ConstantsModel model)
    {
        model = new ConstantsModel();
        if (!TryDecode(payload, ConstantsMagic, out var fields, out var version) || fields.Count < 2)
        {
            return false;
        }

        var index = 2;
        var namingDiagnostics = Array.Empty<DiagnosticReport>();
        if (version == 2)
        {
            if (!TryTakeDiagnostics(fields, ref index, out namingDiagnostics))
            {
                return false;
            }
        }

        if ((fields.Count - index) % 4 != 0)
        {
            return false;
        }

        if (!MemberVisibilityParser.TryParse(fields[1], out var visibility, out _))
        {
            visibility = MemberVisibility.Internal;
        }

        var count = (fields.Count - index) / 4;
        var constants = new ConstantRecord[count];
        for (var i = 0; i < count; i++)
        {
            var offset = index + (i * 4);
            constants[i] = new ConstantRecord(fields[offset], fields[offset + 1], fields[offset + 2], fields[offset + 3]);
        }

        model = new ConstantsModel
        {
            Namespace = NullIfEmpty(fields[0]),
            Visibility = visibility,
            Constants = constants,
            NamesAreFinal = version == 2,
            NamingDiagnostics = namingDiagnostics,
        };
        return true;
    }

    public static string EncodeProject(string? ns, MemberVisibility visibility, IReadOnlyList<ConstantRecord> constants)
        => EncodeNamed(ProjectMagic, ns, visibility, constants);

    public static bool TryDecodeProject(string? payload, out ConstantsModel model)
        => TryDecodeNamed(payload, ProjectMagic, out model);

    public static string EncodeMetadata(string? ns, MemberVisibility visibility, IReadOnlyList<ConstantRecord> constants)
        => EncodeNamed(MetadataMagic, ns, visibility, constants);

    public static bool TryDecodeMetadata(string? payload, out ConstantsModel model)
        => TryDecodeNamed(payload, MetadataMagic, out model);

    public static string EncodeGit(GitModel model)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        return Encode(GitMagic, new[]
        {
            model.Namespace ?? "",
            VisibilityToken(model.Visibility),
            model.Commit ?? "",
            model.Sha ?? "",
            model.Root ?? "",
            model.Url ?? "",
            model.Branch ?? "",
            model.Unavailable ? "1" : "0",
        });
    }

    public static bool TryDecodeGit(string? payload, out GitModel model)
    {
        model = new GitModel();
        if (!TryDecode(payload, GitMagic, out var fields) || fields.Count != 8)
        {
            return false;
        }

        if (!MemberVisibilityParser.TryParse(fields[1], out var visibility, out _))
        {
            visibility = MemberVisibility.Internal;
        }

        model = new GitModel
        {
            Namespace = NullIfEmpty(fields[0]),
            Visibility = visibility,
            Commit = fields[2],
            Sha = fields[3],
            Root = fields[4],
            Url = fields[5],
            Branch = fields[6],
            Unavailable = fields[7] == "1",
        };
        return true;
    }

    public static string EncodeResources(string? ns, MemberVisibility visibility, IReadOnlyList<ResourceRecord> resources)
    {
        var constants = new List<ConstantRecord>();
        if (resources is not null)
        {
            foreach (var resource in resources)
            {
                constants.Add(new ConstantRecord(resource.Name ?? "", resource.ManifestName ?? "", "string", resource.Comment ?? ""));
            }
        }

        return EncodeNamed(ResourcesMagic, ns, visibility, constants);
    }

    public static bool TryDecodeResources(string? payload, out ResourcesModel model)
    {
        model = new ResourcesModel();
        if (!TryDecodeNamed(payload, ResourcesMagic, out var constants))
        {
            return false;
        }

        var resources = new ResourceRecord[constants.Constants.Count];
        for (var i = 0; i < resources.Length; i++)
        {
            var constant = constants.Constants[i];
            resources[i] = new ResourceRecord(constant.Name, constant.Value, constant.Comment);
        }

        model = new ResourcesModel
        {
            Namespace = constants.Namespace,
            Visibility = constants.Visibility,
            Resources = resources,
            NamesAreFinal = constants.NamesAreFinal,
            NamingDiagnostics = constants.NamingDiagnostics,
        };
        return true;
    }

    public static string EncodeResources(string? ns, MemberVisibility visibility, IReadOnlyList<ResourceRecord> resources, IReadOnlyList<DiagnosticReport> namingDiagnostics)
    {
        var constants = new List<ConstantRecord>();
        if (resources is not null)
        {
            foreach (var resource in resources)
            {
                constants.Add(new ConstantRecord(resource.Name ?? "", resource.ManifestName ?? "", "string", resource.Comment ?? ""));
            }
        }

        return EncodeNamed(ResourcesMagic, ns, visibility, constants, namingDiagnostics);
    }

    public static string EncodeStrings(string? ns, MemberVisibility visibility, IReadOnlyList<ConstantRecord> constants)
        => EncodeNamed(StringsMagic, ns, visibility, constants);

    public static string EncodeStrings(string? ns, MemberVisibility visibility, IReadOnlyList<ConstantRecord> constants, IReadOnlyList<DiagnosticReport> namingDiagnostics)
        => EncodeNamed(StringsMagic, ns, visibility, constants, namingDiagnostics);

    public static bool TryDecodeStrings(string? payload, out ConstantsModel model)
        => TryDecodeNamed(payload, StringsMagic, out model);

    private static string EncodeNamed(byte[] magic, string? ns, MemberVisibility visibility, IReadOnlyList<ConstantRecord> constants)
    {
        var fields = new List<string>
        {
            ns ?? "",
            VisibilityToken(visibility),
        };

        if (constants is not null)
        {
            foreach (var constant in constants)
            {
                fields.Add(constant.Name ?? "");
                fields.Add(constant.Value ?? "");
                fields.Add(constant.Comment ?? "");
            }
        }

        return Encode(magic, fields);
    }

    private static string EncodeNamed(byte[] magic, string? ns, MemberVisibility visibility, IReadOnlyList<ConstantRecord> constants, IReadOnlyList<DiagnosticReport> namingDiagnostics)
    {
        var fields = new List<string>
        {
            ns ?? "",
            VisibilityToken(visibility),
        };
        AppendDiagnostics(fields, namingDiagnostics);
        if (constants is not null)
        {
            foreach (var constant in constants)
            {
                fields.Add(constant.Name ?? "");
                fields.Add(constant.Value ?? "");
                fields.Add(constant.Comment ?? "");
            }
        }

        return Encode(magic, fields, version: 2);
    }

    private static bool TryDecodeNamed(string? payload, byte[] magic, out ConstantsModel model)
    {
        model = new ConstantsModel();
        if (!TryDecode(payload, magic, out var fields, out var version) || fields.Count < 2)
        {
            return false;
        }

        var index = 2;
        var namingDiagnostics = Array.Empty<DiagnosticReport>();
        if (version == 2)
        {
            if (!TryTakeDiagnostics(fields, ref index, out namingDiagnostics))
            {
                return false;
            }
        }

        if ((fields.Count - index) % 3 != 0)
        {
            return false;
        }

        if (!MemberVisibilityParser.TryParse(fields[1], out var visibility, out _))
        {
            visibility = MemberVisibility.Internal;
        }

        var count = (fields.Count - index) / 3;
        var constants = new ConstantRecord[count];
        for (var i = 0; i < count; i++)
        {
            var offset = index + (i * 3);
            constants[i] = new ConstantRecord(fields[offset], fields[offset + 1], "string", fields[offset + 2]);
        }

        model = new ConstantsModel
        {
            Namespace = NullIfEmpty(fields[0]),
            Visibility = visibility,
            Constants = constants,
            NamesAreFinal = version == 2,
            NamingDiagnostics = namingDiagnostics,
        };
        return true;
    }

    private static string Encode(byte[] magic, IReadOnlyList<string> fields)
        => Encode(magic, fields, Version);

    private static string Encode(byte[] magic, IReadOnlyList<string> fields, int version)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(magic);
            writer.Write(version);
            writer.Write(fields.Count);
            foreach (var field in fields)
            {
                var bytes = Encoding.UTF8.GetBytes(field ?? "");
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }
        }

        return Convert.ToBase64String(stream.ToArray());
    }

    private static bool TryDecode(string? payload, byte[] magic, out List<string> fields)
        => TryDecode(payload, magic, out fields, out _);

    private static bool TryDecode(string? payload, byte[] magic, out List<string> fields, out int version)
    {
        fields = new List<string>();
        version = 0;
        if (string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(payload);
        }
        catch (FormatException)
        {
            return false;
        }

        using var stream = new MemoryStream(bytes, writable: false);
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: false);
        try
        {
            var actualMagic = reader.ReadBytes(magic.Length);
            if (actualMagic.Length != magic.Length)
            {
                return false;
            }

            for (var i = 0; i < magic.Length; i++)
            {
                if (actualMagic[i] != magic[i])
                {
                    return false;
                }
            }

            version = reader.ReadInt32();
            if (version != 1 && version != 2)
            {
                return false;
            }

            var count = reader.ReadInt32();
            if (count < 0 || count > 100_000)
            {
                return false;
            }

            for (var i = 0; i < count; i++)
            {
                var length = reader.ReadInt32();
                if (length < 0 || length > 16_000_000)
                {
                    return false;
                }

                var data = reader.ReadBytes(length);
                if (data.Length != length)
                {
                    return false;
                }

                fields.Add(Encoding.UTF8.GetString(data));
            }

            return stream.Position == stream.Length;
        }
        catch (EndOfStreamException)
        {
            return false;
        }
    }

    private static string VisibilityToken(MemberVisibility visibility)
        => visibility == MemberVisibility.Public ? "public" : "internal";

    private static string? NullIfEmpty(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void AppendDiagnostics(List<string> fields, IReadOnlyList<DiagnosticReport> diagnostics)
    {
        var items = diagnostics ?? Array.Empty<DiagnosticReport>();
        fields.Add(items.Count.ToString());
        foreach (var diagnostic in items)
        {
            fields.Add(diagnostic.Id ?? "");
            fields.Add(diagnostic.Title ?? "");
            fields.Add(diagnostic.Message ?? "");
            fields.Add(diagnostic.Severity == DiagnosticSeverityKind.Warning ? "warning" : "error");
        }
    }

    private static bool TryTakeDiagnostics(List<string> fields, ref int index, out DiagnosticReport[] diagnostics)
    {
        diagnostics = Array.Empty<DiagnosticReport>();
        if (index >= fields.Count || !int.TryParse(fields[index], out var count) || count < 0)
        {
            return false;
        }

        index++;
        if (index > fields.Count || (long)count * 4 > fields.Count - index)
        {
            return false;
        }

        diagnostics = new DiagnosticReport[count];
        for (var i = 0; i < count; i++)
        {
            var offset = index + (i * 4);
            var severity = fields[offset + 3] == "warning" ? DiagnosticSeverityKind.Warning : DiagnosticSeverityKind.Error;
            diagnostics[i] = new DiagnosticReport(fields[offset], fields[offset + 1], fields[offset + 2], severity);
        }

        index += count * 4;
        return true;
    }
}

/// <summary>解析 $(AssemblyMetaVisibility)。</summary>
public static class MemberVisibilityParser
{
    public static bool TryParse(string? value, out MemberVisibility visibility, out bool recognized)
    {
        var text = value?.Trim() ?? "";
        if (text.Length == 0 || text.Equals("internal", StringComparison.OrdinalIgnoreCase))
        {
            visibility = MemberVisibility.Internal;
            recognized = true;
            return true;
        }

        if (text.Equals("public", StringComparison.OrdinalIgnoreCase))
        {
            visibility = MemberVisibility.Public;
            recognized = true;
            return true;
        }

        visibility = MemberVisibility.Internal;
        recognized = false;
        return true;
    }
}
