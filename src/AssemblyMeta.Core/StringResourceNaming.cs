using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace AssemblyMeta.Core;

/// <summary>
/// 把 resx 键映射为 AssemblyMeta.Strings 下的常量名。
/// 文件名 Strings 会从路径末尾去掉，避免生成 AssemblyMeta.Strings.Strings。
/// 键里的点号和下划线都表示嵌套。
/// </summary>
public static class StringResourceNaming
{
    public static bool TryCreate(string logicalPath, string key, string value, string comment, out ConstantRecord record)
    {
        record = new ConstantRecord("", "", "string", comment ?? "");
        var keyPath = NormalizeKey(key);
        if (keyPath.Length == 0)
        {
            return false;
        }

        var prefix = PrefixFromPath(logicalPath);
        var name = prefix.Length == 0 ? keyPath : prefix + "." + keyPath;
        record = new ConstantRecord(name, value ?? "", "string", comment ?? "");
        return true;
    }

    public static bool IsSatellitePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var name = Path.GetFileNameWithoutExtension(path);
        var dot = name.LastIndexOf('.');
        if (dot <= 0 || dot >= name.Length - 1)
        {
            return false;
        }

        var culture = name.Substring(dot + 1);
        try
        {
            var info = CultureInfo.GetCultureInfo(culture);
            return info.Name.Length > 0;
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }

    private static string PrefixFromPath(string? logicalPath)
    {
        var value = (logicalPath ?? "").Replace('\\', '/').Trim('/');
        var extension = Path.GetExtension(value);
        if (extension.Length > 0)
        {
            value = value.Substring(0, value.Length - extension.Length);
        }

        while (value.Equals("Strings", StringComparison.Ordinal) || value.EndsWith("/Strings", StringComparison.Ordinal))
        {
            var slash = value.LastIndexOf('/');
            value = slash < 0 ? "" : value.Substring(0, slash);
        }

        return value.Replace('/', '.');
    }

    private static string NormalizeKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return "";
        }

        var builder = new StringBuilder(key!.Length);
        var pendingDot = false;
        foreach (var ch in key)
        {
            if (ch == '.' || ch == '_')
            {
                pendingDot = builder.Length > 0;
                continue;
            }

            if (pendingDot)
            {
                builder.Append('.');
                pendingDot = false;
            }

            builder.Append(ch);
        }

        return builder.ToString();
    }
}
