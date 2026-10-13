using System;

namespace AssemblyMeta.Core;

/// <summary>把 EmbeddedResource 映射为嵌套名和清单资源名。命名规则与 FileConstant 相同。</summary>
public static class ResourceNaming
{
    public static bool TryCreate(FileConstantNaming.Input input, string manifestName, out ResourceRecord record)
    {
        record = new ResourceRecord("", "", input.Comment ?? "");
        if (string.IsNullOrWhiteSpace(manifestName))
        {
            return false;
        }

        if (!FileConstantNaming.TryCreate(input, out var constant))
        {
            return false;
        }

        record = new ResourceRecord(constant.Name, manifestName.Trim(), input.Comment ?? "");
        return true;
    }

    public static bool IsResx(string? extension, string? logicalPath)
    {
        if (!string.IsNullOrEmpty(extension) && extension!.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return !string.IsNullOrEmpty(logicalPath) && logicalPath!.EndsWith(".resx", StringComparison.OrdinalIgnoreCase);
    }
}
