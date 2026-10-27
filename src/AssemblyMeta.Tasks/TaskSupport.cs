using AssemblyMeta.Core;
using Microsoft.Build.Framework;

namespace AssemblyMeta.Tasks;

internal static class TaskSupport
{
    public static MemberVisibility ParseVisibility(Microsoft.Build.Utilities.Task task, string? visibility)
    {
        MemberVisibilityParser.TryParse(visibility, out var parsed, out var recognized);
        if (!recognized)
        {
            task.Log.LogWarning("Unrecognized AssemblyMetaVisibility “{0}”; fell back to internal. / 无法识别 AssemblyMetaVisibility“{0}”，已使用 internal。", visibility);
        }

        return parsed;
    }

    public static FileConstantNaming.Input ReadFileInput(ITaskItem item)
        => new(
            item.GetMetadata("RelativeDir"),
            item.GetMetadata("Filename"),
            item.GetMetadata("Extension"),
            item.GetMetadata("Link"),
            item.GetMetadata("Comment"));

    public static void LogCodeWarning(Microsoft.Build.Utilities.Task task, string code, string? file, string message, params object[] args)
    {
        task.Log.LogWarning(
            subcategory: "",
            warningCode: code,
            helpKeyword: "",
            file: file ?? "",
            lineNumber: 0,
            columnNumber: 0,
            endLineNumber: 0,
            endColumnNumber: 0,
            message: message,
            messageArgs: args);
    }

    public static bool IsTrue(string? value)
        => string.Equals(value?.Trim(), "true", System.StringComparison.OrdinalIgnoreCase);

    public static bool IsFalse(string? value)
        => string.Equals(value?.Trim(), "false", System.StringComparison.OrdinalIgnoreCase);
}
