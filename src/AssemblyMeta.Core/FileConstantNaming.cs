using System;
using System.IO;

namespace AssemblyMeta.Core;

/// <summary>
/// 把 FileConstant 项映射为常量名和相对路径。
/// 值为相对项目的正斜杠路径；Link 优先于物理路径。项目外且没有 Link 的文件会被跳过。
/// </summary>
public static class FileConstantNaming
{
    public readonly record struct Input(string RelativeDir, string FileName, string Extension, string Link, string Comment);

    public static bool TryCreate(Input input, out ConstantRecord record)
    {
        record = new ConstantRecord("", "", "string", input.Comment ?? "");
        string logicalPath;
        if (!string.IsNullOrEmpty(input.Link))
        {
            logicalPath = Normalize(input.Link);
        }
        else if (IsRooted(input.RelativeDir))
        {
            return false;
        }
        else
        {
            logicalPath = Normalize((input.RelativeDir ?? "") + (input.FileName ?? "") + (input.Extension ?? ""));
        }

        if (logicalPath.Length == 0 || logicalPath.EndsWith("/", StringComparison.Ordinal))
        {
            return false;
        }

        var extension = Path.GetExtension(logicalPath);
        var withoutExtension = extension.Length == 0
            ? logicalPath
            : logicalPath.Substring(0, logicalPath.Length - extension.Length);
        if (withoutExtension.Length == 0)
        {
            return false;
        }

        var name = withoutExtension.Replace('/', '.');
        record = new ConstantRecord(name, logicalPath, "string", input.Comment ?? "");
        return true;
    }

    private static bool IsRooted(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        return Path.IsPathRooted(path) || path!.StartsWith("/", StringComparison.Ordinal) || path!.StartsWith("\\", StringComparison.Ordinal);
    }

    private static string Normalize(string path)
    {
        var value = path.Replace('\\', '/');
        while (value.StartsWith("./", StringComparison.Ordinal))
        {
            value = value.Substring(2);
        }

        if (value.StartsWith("/", StringComparison.Ordinal))
        {
            value = value.TrimStart('/');
        }

        return value;
    }
}
