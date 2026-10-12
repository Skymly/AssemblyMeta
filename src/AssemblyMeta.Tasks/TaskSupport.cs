using AssemblyMeta.Core;

namespace AssemblyMeta.Tasks;

internal static class TaskSupport
{
    public static MemberVisibility ParseVisibility(Microsoft.Build.Utilities.Task task, string? visibility)
    {
        MemberVisibilityParser.TryParse(visibility, out var parsed, out var recognized);
        if (!recognized)
        {
            task.Log.LogWarning("无法识别 AssemblyMetaVisibility“{0}”，已使用 internal。", visibility);
        }

        return parsed;
    }

    public static bool IsTrue(string? value)
        => string.Equals(value?.Trim(), "true", System.StringComparison.OrdinalIgnoreCase);
}
