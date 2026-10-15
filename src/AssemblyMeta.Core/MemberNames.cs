using System;
using System.Collections.Generic;

namespace AssemblyMeta.Core;

public enum MemberNameArea
{
    Constants,
    Resources,
    Strings,
}

public sealed class ConstantNameInput
{
    public string? Name { get; init; }

    public string? Value { get; init; }

    public string? Type { get; init; }

    public string? Comment { get; init; }
}

public sealed class FileConstantNameInput
{
    public string? RelativeDir { get; init; }

    public string? Filename { get; init; }

    public string? Extension { get; init; }

    public string? Link { get; init; }

    public string? Comment { get; init; }
}

public sealed class ResourceNameInput
{
    public string? Name { get; init; }

    public string? RelativeDir { get; init; }

    public string? Filename { get; init; }

    public string? Extension { get; init; }

    public string? Link { get; init; }

    public string? Comment { get; init; }

    public string? ManifestName { get; init; }

    public string? Spec { get; init; }
}

public sealed class StringNameInput
{
    public string? Name { get; init; }

    public bool OutsideWithoutLink { get; init; }

    public string? Spec { get; init; }

    public string? LogicalPath { get; init; }

    public string? Key { get; init; }

    public string? Value { get; init; }

    public string? Comment { get; init; }
}

public sealed class MemberNameRequest
{
    public MemberNameArea Area { get; init; }

    public IReadOnlyList<ConstantNameInput> Constants { get; init; } = Array.Empty<ConstantNameInput>();

    public IReadOnlyList<FileConstantNameInput> FileConstants { get; init; } = Array.Empty<FileConstantNameInput>();

    public IReadOnlyList<ResourceNameInput> Resources { get; init; } = Array.Empty<ResourceNameInput>();

    public IReadOnlyList<StringNameInput> Strings { get; init; } = Array.Empty<StringNameInput>();
}

public sealed class NamedMember
{
    public string Name { get; init; } = "";

    public string Value { get; init; } = "";

    public string Type { get; init; } = "string";

    public string Comment { get; init; } = "";

    public string? ManifestName { get; init; }
}

public sealed class MemberNameResult
{
    public IReadOnlyList<NamedMember> Members { get; init; } = Array.Empty<NamedMember>();

    public IReadOnlyList<DiagnosticReport> TaskDiagnostics { get; init; } = Array.Empty<DiagnosticReport>();

    public IReadOnlyList<DiagnosticReport> GeneratorDiagnostics { get; init; } = Array.Empty<DiagnosticReport>();
}

/// <summary>
/// 把已经读出的项收成消毒后的点号名，并带上诊断。不读文件，不接 MSBuild 或 Roslyn。
/// </summary>
public static class MemberNames
{
    public static MemberNameResult Name(MemberNameRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var members = new List<NamedMember>();
        var taskDiagnostics = new List<DiagnosticReport>();
        var generatorDiagnostics = new List<DiagnosticReport>();
        var root = new Node();
        switch (request.Area)
        {
            case MemberNameArea.Constants:
                NameConstants(request, root, members, generatorDiagnostics);
                break;
            case MemberNameArea.Resources:
                NameResources(request, root, members, taskDiagnostics, generatorDiagnostics);
                break;
            case MemberNameArea.Strings:
                NameStrings(request, root, members, taskDiagnostics, generatorDiagnostics);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(request));
        }

        return new MemberNameResult
        {
            Members = members,
            TaskDiagnostics = taskDiagnostics,
            GeneratorDiagnostics = generatorDiagnostics,
        };
    }

    private static void NameConstants(MemberNameRequest request, Node root, List<NamedMember> members, List<DiagnosticReport> generatorDiagnostics)
    {
        foreach (var item in request.Constants)
        {
            var comment = item.Comment ?? "";
            var type = string.IsNullOrWhiteSpace(item.Type) ? "string" : item.Type!.Trim();
            Accept(root, members, generatorDiagnostics, item.Name, item.Value ?? "", type, comment, null, AreaKind.Constants, "常量");
        }

        foreach (var item in request.FileConstants)
        {
            var created = FileConstantNaming.TryCreate(
                new FileConstantNaming.Input(
                    item.RelativeDir ?? "",
                    item.Filename ?? "",
                    item.Extension ?? "",
                    item.Link ?? "",
                    item.Comment ?? ""),
                out var record);
            if (!created)
            {
                continue;
            }

            Accept(root, members, generatorDiagnostics, record.Name, record.Value, "string", record.Comment, null, AreaKind.Constants, "常量");
        }
    }

    private static void NameResources(MemberNameRequest request, Node root, List<NamedMember> members, List<DiagnosticReport> taskDiagnostics, List<DiagnosticReport> generatorDiagnostics)
    {
        foreach (var item in request.Resources)
        {
            var spec = item.Spec ?? "";
            var manifest = (item.ManifestName ?? "").Trim();
            if (manifest.Length == 0)
            {
                taskDiagnostics.Add(Warning("AM0009", "嵌入资源已跳过", "嵌入资源“" + spec + "”没有清单名，已跳过。"));
                continue;
            }

            string raw;
            if (!string.IsNullOrEmpty(item.Name))
            {
                raw = item.Name!;
            }
            else
            {
                var created = ResourceNaming.TryCreate(
                    new FileConstantNaming.Input(
                        item.RelativeDir ?? "",
                        item.Filename ?? "",
                        item.Extension ?? "",
                        item.Link ?? "",
                        item.Comment ?? ""),
                    manifest,
                    out var record);
                if (!created)
                {
                    taskDiagnostics.Add(Warning("AM0010", "嵌入资源已跳过", "嵌入资源“" + spec + "”无法映射为标识符，已跳过。"));
                    continue;
                }

                raw = record.Name;
                manifest = record.ManifestName;
            }

            Accept(root, members, generatorDiagnostics, raw, manifest, "string", item.Comment ?? "", manifest, AreaKind.Resources, "资源");
        }
    }

    private static void NameStrings(MemberNameRequest request, Node root, List<NamedMember> members, List<DiagnosticReport> taskDiagnostics, List<DiagnosticReport> generatorDiagnostics)
    {
        foreach (var item in request.Strings)
        {
            var spec = item.Spec ?? "";
            if (item.OutsideWithoutLink)
            {
                taskDiagnostics.Add(Warning("AM0010", "字符串资源已跳过", "resx“" + spec + "”位于项目外且没有 Link，已跳过。"));
                continue;
            }

            string raw;
            var value = item.Value ?? "";
            var comment = item.Comment ?? "";
            if (!string.IsNullOrEmpty(item.Name))
            {
                raw = item.Name!;
            }
            else if (!StringResourceNaming.TryCreate(item.LogicalPath ?? "", item.Key ?? "", value, comment, out var record))
            {
                taskDiagnostics.Add(Warning("AM0010", "字符串资源已跳过", "已忽略 resx“" + spec + "”中的无效键“" + (item.Key ?? "") + "”。"));
                continue;
            }
            else
            {
                raw = record.Name;
                value = record.Value;
                comment = record.Comment;
            }

            if (string.IsNullOrWhiteSpace(comment))
            {
                comment = "字符串资源 " + raw + "。";
            }

            Accept(root, members, generatorDiagnostics, raw, value, "string", comment, null, AreaKind.Strings, "常量");
        }
    }

    private static void Accept(
        Node root,
        List<NamedMember> members,
        List<DiagnosticReport> generatorDiagnostics,
        string? rawName,
        string value,
        string type,
        string comment,
        string? manifest,
        AreaKind area,
        string duplicateKind)
    {
        if (!TrySegments(rawName, out var identifiers, out var invalidSegment))
        {
            var raw = rawName ?? "";
            var message = string.IsNullOrWhiteSpace(raw)
                ? (area == AreaKind.Resources ? "存在空的资源名或清单名，已忽略。" : "存在空的常量名，已忽略。")
                : (area == AreaKind.Resources
                    ? "资源“" + raw + "”包含无法映射为 C# 标识符的片段“" + invalidSegment + "”，已忽略。"
                    : "常量“" + raw + "”包含无法映射为 C# 标识符的片段“" + invalidSegment + "”，已忽略。");
            var title = area == AreaKind.Resources ? "资源名无效" : "常量名无效";
            generatorDiagnostics.Add(Error("AM0002", title, message));
            return;
        }

        var node = root;
        var parent = area == AreaKind.Resources ? "Resources" : area == AreaKind.Strings ? "Strings" : "Constants";
        for (var i = 0; i < identifiers.Length; i++)
        {
            var identifier = identifiers[i];
            var isLeaf = i == identifiers.Length - 1;
            if (area == AreaKind.Strings && parent.Length > 0 && string.Equals(Bare(identifier), parent, StringComparison.Ordinal))
            {
                var renamed = identifier + "_";
                generatorDiagnostics.Add(Warning(
                    "AM0008",
                    "名称与外层类型冲突",
                    "常量“" + rawName + "”的片段“" + Bare(identifier) + "”与外层类型同名，已改为“" + Bare(renamed) + "”。"));
                identifier = renamed;
                identifiers[i] = identifier;
            }
            else if (area == AreaKind.Resources)
            {
                identifier = DisambiguateResource(identifier, parent, i == 0, isLeaf, rawName ?? "", generatorDiagnostics);
                identifiers[i] = identifier;
            }

            var child = node.TryDescend(identifier, isLeaf, rawName ?? "", duplicateKind, generatorDiagnostics);
            if (child is null)
            {
                return;
            }

            node = child;
            parent = Bare(identifier);
        }

        node.HasLeaf = true;
        node.LeafPath = rawName;
        members.Add(new NamedMember
        {
            Name = string.Join(".", identifiers),
            Value = value,
            Type = type,
            Comment = comment,
            ManifestName = manifest,
        });
    }

    private static string DisambiguateResource(string identifier, string parent, bool isRootSegment, bool isLeaf, string path, List<DiagnosticReport> diagnostics)
    {
        var bare = Bare(identifier);
        string? reason = null;
        if (parent.Length > 0 && string.Equals(bare, parent, StringComparison.Ordinal))
        {
            reason = "与外层类型同名";
        }
        else if (isRootSegment && string.Equals(bare, "Manifest", StringComparison.Ordinal))
        {
            reason = "与生成的 Manifest 访问器同名";
        }
        else if (isLeaf && (bare == "Text" || bare == "GetBytes" || bare == "GetStream"))
        {
            reason = "与资源成员 Text、GetBytes 或 GetStream 同名";
        }

        if (reason is null)
        {
            return identifier;
        }

        var renamed = identifier + "_";
        diagnostics.Add(Warning("AM0008", "资源名已调整", "资源“" + path + "”的片段“" + bare + "”" + reason + "，已改为“" + Bare(renamed) + "”。"));
        return renamed;
    }

    private static bool TrySegments(string? dotted, out string[] identifiers, out string invalidSegment)
    {
        identifiers = Array.Empty<string>();
        invalidSegment = "";
        if (string.IsNullOrWhiteSpace(dotted))
        {
            return false;
        }

        var segments = dotted!.Split('.');
        identifiers = new string[segments.Length];
        for (var i = 0; i < segments.Length; i++)
        {
            if (!IdentifierSanitizer.TrySanitize(segments[i], out var identifier))
            {
                invalidSegment = segments[i];
                return false;
            }

            identifiers[i] = identifier;
        }

        return true;
    }

    private static string Bare(string identifier)
        => identifier.Length > 0 && identifier[0] == '@' ? identifier.Substring(1) : identifier;

    private static DiagnosticReport Error(string id, string title, string message)
        => new(id, title, message, DiagnosticSeverityKind.Error);

    private static DiagnosticReport Warning(string id, string title, string message)
        => new(id, title, message, DiagnosticSeverityKind.Warning);

    private enum AreaKind
    {
        Constants,
        Resources,
        Strings,
    }

    private sealed class Node
    {
        public string? LeafPath { get; set; }

        public bool HasLeaf { get; set; }

        public Dictionary<string, Node> Children { get; } = new(StringComparer.Ordinal);

        public Node? TryDescend(string identifier, bool isLeaf, string path, string kind, List<DiagnosticReport> diagnostics)
        {
            if (!Children.TryGetValue(identifier, out var existing))
            {
                if (HasLeaf)
                {
                    diagnostics.Add(Error("AM0001", kind + "名重复", kind + "“" + path + "”与已有成员“" + LeafPath + "”冲突，已忽略。"));
                    return null;
                }

                var created = new Node();
                Children.Add(identifier, created);
                return created;
            }

            if (isLeaf && (existing.HasLeaf || existing.Children.Count > 0))
            {
                diagnostics.Add(Error("AM0001", kind + "名重复", kind + "“" + path + "”与已有成员冲突，已忽略。"));
                return null;
            }

            if (!isLeaf && existing.HasLeaf)
            {
                diagnostics.Add(Error("AM0001", kind + "名重复", kind + "“" + path + "”与已有成员“" + existing.LeafPath + "”冲突，已忽略。"));
                return null;
            }

            return existing;
        }
    }
}
