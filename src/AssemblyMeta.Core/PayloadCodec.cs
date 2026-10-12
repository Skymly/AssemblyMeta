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

    public static bool TryDecodeConstants(string? payload, out ConstantsModel model)
    {
        model = new ConstantsModel();
        if (!TryDecode(payload, ConstantsMagic, out var fields) || fields.Count < 2 || (fields.Count - 2) % 4 != 0)
        {
            return false;
        }

        if (!MemberVisibilityParser.TryParse(fields[1], out var visibility, out _))
        {
            visibility = MemberVisibility.Internal;
        }

        var count = (fields.Count - 2) / 4;
        var constants = new ConstantRecord[count];
        for (var i = 0; i < count; i++)
        {
            var offset = 2 + (i * 4);
            constants[i] = new ConstantRecord(fields[offset], fields[offset + 1], fields[offset + 2], fields[offset + 3]);
        }

        model = new ConstantsModel
        {
            Namespace = NullIfEmpty(fields[0]),
            Visibility = visibility,
            Constants = constants,
        };
        return true;
    }

    private static string Encode(byte[] magic, IReadOnlyList<string> fields)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(magic);
            writer.Write(Version);
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
    {
        fields = new List<string>();
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

            var version = reader.ReadInt32();
            if (version != Version)
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
