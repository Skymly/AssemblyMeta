using AssemblyMeta.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace AssemblyMeta.Core.Emit;

/// <summary>把 AssemblyInfo / Constants / Project / Metadata / Git / Resources / Strings 模型写成 C#。不引用 Roslyn，便于任务程序集复用同一套模型。</summary>
public sealed class CSharpSourceEmitter : ISourceEmitter
{
    public SourceLanguage Language => SourceLanguage.CSharp;

    public EmitResult EmitAssemblyInfo(AssemblyInfoModel model, int csharpLanguageVersion)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        var diagnostics = new List<DiagnosticReport>();
        var ns = ResolveNamespace(model.Namespace, diagnostics);
        var builder = new StringBuilder();
        WriteHeader(builder, "AssemblyMeta.AssemblyInfo");
        using (var scope = new NamespaceScope(builder, ns))
        {
            var indent = scope.Indent;
            WriteRoot(builder, indent, model.Visibility);
            indent++;
            WriteNestedClassOpen(builder, indent, "Info", "SDK 默认生成的程序集特性常量。");
            indent++;
            WriteStringMember(builder, indent, model.Visibility, "Configuration", model.Configuration, "对应 AssemblyConfigurationAttribute。", csharpLanguageVersion);
            WriteStringMember(builder, indent, model.Visibility, "Company", model.Company, "对应 AssemblyCompanyAttribute。", csharpLanguageVersion);
            WriteStringMember(builder, indent, model.Visibility, "Title", model.Title, "对应 AssemblyTitleAttribute。", csharpLanguageVersion);
            WriteStringMember(builder, indent, model.Visibility, "Description", model.Description, "对应 AssemblyDescriptionAttribute。", csharpLanguageVersion);
            WriteStringMember(builder, indent, model.Visibility, "Product", model.Product, "对应 AssemblyProductAttribute。", csharpLanguageVersion);
            WriteStringMember(builder, indent, model.Visibility, "Copyright", model.Copyright, "对应 AssemblyCopyrightAttribute。", csharpLanguageVersion);
            WriteStringMember(builder, indent, model.Visibility, "Version", model.Version, "对应 AssemblyVersionAttribute。取 AssemblyVersion，不是 NuGet Version。", csharpLanguageVersion);
            WriteStringMember(builder, indent, model.Visibility, "InformationalVersion", model.InformationalVersion, "对应 AssemblyInformationalVersionAttribute。", csharpLanguageVersion);
            WriteStringMember(builder, indent, model.Visibility, "FileVersion", model.FileVersion, "对应 AssemblyFileVersionAttribute。", csharpLanguageVersion);
            indent--;
            WriteClose(builder, indent);
            indent--;
            WriteClose(builder, indent);
        }

        return new EmitResult { Source = builder.ToString(), Diagnostics = diagnostics };
    }

    public EmitResult EmitConstants(ConstantsModel model, int csharpLanguageVersion)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        var diagnostics = new List<DiagnosticReport>();
        if (model.NamesAreFinal)
        {
            diagnostics.AddRange(model.NamingDiagnostics);
        }

        var ns = ResolveNamespace(model.Namespace, diagnostics);
        var root = new Node();
        if (model.Constants is not null)
        {
            foreach (var constant in model.Constants)
            {
                AddConstant(root, constant, model.Visibility, csharpLanguageVersion, diagnostics, "Constants", avoidEnclosingName: false, namesAreFinal: true);
            }
        }

        var builder = new StringBuilder();
        WriteHeader(builder, "AssemblyMeta.Constants");
        using (var scope = new NamespaceScope(builder, ns))
        {
            var indent = scope.Indent;
            WriteRoot(builder, indent, model.Visibility);
            indent++;
            WriteNestedClassOpen(builder, indent, "Constants", "由 Constant / FileConstant MSBuild 项生成的编译期常量。");
            indent++;
            EmitChildren(builder, root, indent, model.Visibility);
            indent--;
            WriteClose(builder, indent);
            indent--;
            WriteClose(builder, indent);
        }

        return new EmitResult { Source = builder.ToString(), Diagnostics = diagnostics };
    }

    public EmitResult EmitProject(ConstantsModel model, int csharpLanguageVersion)
        => EmitArea(model, csharpLanguageVersion, "AssemblyMeta.Project", "Project", "由 ProjectProperty 项生成的项目属性常量。", "项目属性");

    public EmitResult EmitMetadata(ConstantsModel model, int csharpLanguageVersion)
        => EmitArea(model, csharpLanguageVersion, "AssemblyMeta.Metadata", "Metadata", "由 AssemblyMetadata 项生成的程序集元数据常量。", "程序集元数据");

    public EmitResult EmitGit(GitModel model, int csharpLanguageVersion)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        var diagnostics = new List<DiagnosticReport>();
        if (model.Unavailable)
        {
            diagnostics.Add(Warning("AM0006", "Git 信息不可用", "未能取得 Git 提交号。确认项目位于 Git 仓库内，且 EnableSourceControlManagerQueries 为 true。"));
        }

        var ns = ResolveNamespace(model.Namespace, diagnostics);
        var builder = new StringBuilder();
        WriteHeader(builder, "AssemblyMeta.Git");
        using (var scope = new NamespaceScope(builder, ns))
        {
            var indent = scope.Indent;
            WriteRoot(builder, indent, model.Visibility);
            indent++;
            WriteNestedClassOpen(builder, indent, "Git", "编译期 Git 信息。设计时构建默认不查询，值为 [pending build]。");
            indent++;
            WriteStringMember(builder, indent, model.Visibility, "Commit", model.Commit, "当前提交的完整哈希。", csharpLanguageVersion);
            WriteStringMember(builder, indent, model.Visibility, "Sha", model.Sha, "Commit 的前 9 位。不足 9 位时与 Commit 相同。", csharpLanguageVersion);
            WriteStringMember(builder, indent, model.Visibility, "Root", model.Root, "仓库根目录，使用正斜杠，并以斜杠结尾。", csharpLanguageVersion);
            WriteStringMember(builder, indent, model.Visibility, "Url", model.Url, "仓库地址。仅当 PublishRepositoryUrl=true，或已经设置 RepositoryUrl 时填充。", csharpLanguageVersion);
            WriteStringMember(builder, indent, model.Visibility, "Branch", model.Branch, "当前分支。拉取请求场景为 pr 加编号。", csharpLanguageVersion);
            indent--;
            WriteClose(builder, indent);
            indent--;
            WriteClose(builder, indent);
        }

        return new EmitResult { Source = builder.ToString(), Diagnostics = diagnostics };
    }

    public EmitResult EmitResources(ResourcesModel model, int csharpLanguageVersion)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        var diagnostics = new List<DiagnosticReport>();
        if (model.NamesAreFinal)
        {
            diagnostics.AddRange(model.NamingDiagnostics);
        }

        var ns = ResolveNamespace(model.Namespace, diagnostics);
        var root = new NameNode<ResourceRecord>();
        if (model.Resources is not null)
        {
            foreach (var resource in model.Resources)
            {
                AddResource(root, resource, diagnostics, namesAreFinal: true);
            }
        }

        var builder = new StringBuilder();
        WriteHeader(builder, "AssemblyMeta.Resources");
        using (var scope = new NamespaceScope(builder, ns))
        {
            var indent = scope.Indent;
            WriteRoot(builder, indent, model.Visibility);
            indent++;
            WriteNestedClassOpen(builder, indent, "Resources", "嵌入资源的强类型访问。Text 按 UTF-8 读取并缓存，GetBytes 与 GetStream 每次重新打开。");
            indent++;
            EmitResourceChildren(builder, root, indent);
            if (root.ChildOrder.Count > 0)
            {
                WriteManifest(builder, indent);
            }

            indent--;
            WriteClose(builder, indent);
            indent--;
            WriteClose(builder, indent);
        }

        return new EmitResult { Source = builder.ToString(), Diagnostics = diagnostics };
    }

    public EmitResult EmitStrings(ConstantsModel model, int csharpLanguageVersion)
        => EmitArea(model, csharpLanguageVersion, "AssemblyMeta.Strings", "Strings", "由中性 resx 生成的编译期字符串常量。卫星文化资源不参与生成。", "字符串资源", avoidEnclosingName: false, namesAreFinal: true);

    private EmitResult EmitArea(ConstantsModel model, int languageVersion, string packageId, string className, string classSummary, string defaultCommentPrefix, bool avoidEnclosingName = false, bool namesAreFinal = false)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        var diagnostics = new List<DiagnosticReport>();
        if (namesAreFinal)
        {
            diagnostics.AddRange(model.NamingDiagnostics);
        }

        var ns = ResolveNamespace(model.Namespace, diagnostics);
        var root = new Node();
        if (model.Constants is not null)
        {
            foreach (var constant in model.Constants)
            {
                var comment = constant.Comment;
                if (string.IsNullOrWhiteSpace(comment))
                {
                    comment = defaultCommentPrefix + " " + (constant.Name ?? "") + "。";
                }

                AddConstant(root, new ConstantRecord(constant.Name ?? "", constant.Value ?? "", "string", comment ?? ""), model.Visibility, languageVersion, diagnostics, className, avoidEnclosingName, namesAreFinal);
            }
        }

        var builder = new StringBuilder();
        WriteHeader(builder, packageId);
        using (var scope = new NamespaceScope(builder, ns))
        {
            var indent = scope.Indent;
            WriteRoot(builder, indent, model.Visibility);
            indent++;
            WriteNestedClassOpen(builder, indent, className, classSummary);
            indent++;
            EmitChildren(builder, root, indent, model.Visibility);
            indent--;
            WriteClose(builder, indent);
            indent--;
            WriteClose(builder, indent);
        }

        return new EmitResult { Source = builder.ToString(), Diagnostics = diagnostics };
    }

    private static void AddConstant(Node root, ConstantRecord constant, MemberVisibility visibility, int languageVersion, List<DiagnosticReport> diagnostics, string enclosingType, bool avoidEnclosingName, bool namesAreFinal = false)
    {
        var name = constant.Name ?? "";
        if (string.IsNullOrWhiteSpace(name))
        {
            diagnostics.Add(Error("AM0002", "常量名无效", "存在空的常量名，已忽略。"));
            return;
        }

        var segments = name.Split('.');
        var identifiers = new string[segments.Length];
        for (var i = 0; i < segments.Length; i++)
        {
            if (namesAreFinal)
            {
                if (!IdentifierSanitizer.IsValidIdentifier(segments[i]))
                {
                    diagnostics.Add(Error("AM0002", "常量名无效", "常量“" + name + "”包含无法映射为 C# 标识符的片段“" + segments[i] + "”，已忽略。"));
                    return;
                }

                identifiers[i] = segments[i];
            }
            else if (!IdentifierSanitizer.TrySanitize(segments[i], out var identifier))
            {
                diagnostics.Add(Error("AM0002", "常量名无效", "常量“" + name + "”包含无法映射为 C# 标识符的片段“" + segments[i] + "”，已忽略。"));
                return;
            }
            else
            {
                identifiers[i] = identifier;
            }
        }

        var typeName = constant.EffectiveType;
        if (!IdentifierSanitizer.IsStringType(typeName) && string.IsNullOrWhiteSpace(constant.Value))
        {
            diagnostics.Add(Error("AM0003", "常量值无效", "常量“" + name + "”的类型不是 string，但 Value 为空，已忽略。"));
            return;
        }

        var node = root;
        var parent = enclosingType ?? "";
        for (var i = 0; i < identifiers.Length; i++)
        {
            var identifier = identifiers[i];
            if (!namesAreFinal && avoidEnclosingName && parent.Length > 0 && string.Equals(Bare(identifier), parent, StringComparison.Ordinal))
            {
                var renamed = identifier + "_";
                diagnostics.Add(Warning(
                    "AM0008",
                    "名称与外层类型冲突",
                    "常量“" + name + "”的片段“" + Bare(identifier) + "”与外层类型同名，已改为“" + Bare(renamed) + "”。"));
                identifier = renamed;
            }

            var isLeaf = i == identifiers.Length - 1;
            var child = node.TryDescend(identifier, isLeaf, name, "常量", diagnostics);
            if (child is null)
            {
                return;
            }

            node = (Node)child;
            parent = Bare(identifier);
        }

        node.Leaf = constant;
        node.LeafPath = name;
        node.TypeName = typeName;
        node.Rhs = IdentifierSanitizer.IsStringType(typeName)
            ? CSharpLiteral.String(constant.Value ?? "", languageVersion)
            : constant.Value.Trim();
        node.UseConst = visibility != MemberVisibility.Public && IdentifierSanitizer.IsConstEligible(typeName);
        node.Comment = constant.Comment;
    }

    private static void EmitChildren(StringBuilder builder, Node node, int indent, MemberVisibility visibility)
    {
        foreach (var name in node.ChildOrder)
        {
            var child = (Node)node.Children[name];
            if (child.Leaf is not null && child.Children.Count == 0)
            {
                WriteMember(builder, indent, child);
                continue;
            }

            WriteNestedClassOpen(builder, indent, child.Identifier, null);
            if (child.Leaf is not null)
            {
                // 既是叶子又有子节点的情况已在添加时拒绝。
            }

            EmitChildren(builder, child, indent + 1, visibility);
            WriteClose(builder, indent);
        }
    }

    private static void WriteMember(StringBuilder builder, int indent, Node node)
    {
        if (!string.IsNullOrWhiteSpace(node.Comment))
        {
            WriteSummary(builder, indent, node.Comment!);
        }

        var keyword = node.UseConst ? "const" : "static readonly";
        var rhs = node.Rhs ?? "\"\"";
        if (rhs.IndexOf('\n') >= 0)
        {
            Line(builder, indent, "public " + keyword + " " + node.TypeName + " " + node.Identifier + " = " + rhs + ";");
        }
        else
        {
            Line(builder, indent, "public " + keyword + " " + node.TypeName + " " + node.Identifier + " = " + rhs + ";");
        }
    }

    private static void WriteStringMember(StringBuilder builder, int indent, MemberVisibility visibility, string name, string? value, string summary, int languageVersion)
    {
        WriteSummary(builder, indent, summary!);
        var useConst = visibility != MemberVisibility.Public;
        var keyword = useConst ? "const" : "static readonly";
        var rhs = CSharpLiteral.String(value ?? "", languageVersion);
        Line(builder, indent, "public " + keyword + " string " + name + " = " + rhs + ";");
        builder.Append('\n');
    }

    private static string? ResolveNamespace(string? ns, List<DiagnosticReport> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(ns))
        {
            return null;
        }

        if (!IdentifierSanitizer.TryEscapeNamespace(ns!, out var escaped) || string.IsNullOrEmpty(escaped))
        {
            diagnostics.Add(Warning("AM0005", "命名空间无效", "AssemblyMetaNamespace“" + ns + "”不是有效的命名空间，已回退到全局命名空间。"));
            return null;
        }

        return escaped;
    }

    private static void WriteHeader(StringBuilder builder, string packageId)
    {
        builder.Append("// <auto-generated/>\n");
        builder.Append("// 由 ").Append(packageId).Append(" 在编译期生成。请勿手动修改。\n");
        builder.Append("#nullable enable\n");
        builder.Append("#pragma warning disable\n\n");
    }

    private static void WriteRoot(StringBuilder builder, int indent, MemberVisibility visibility)
    {
        var keyword = visibility == MemberVisibility.Public ? "public" : "internal";
        Line(builder, indent, keyword + " static partial class AssemblyMeta");
        Line(builder, indent, "{");
    }

    private static void WriteNestedClassOpen(StringBuilder builder, int indent, string identifier, string? summary)
    {
        if (!string.IsNullOrEmpty(summary))
        {
            WriteSummary(builder, indent, summary!);
        }

        Line(builder, indent, "public static partial class " + identifier);
        Line(builder, indent, "{");
    }

    private static void WriteClose(StringBuilder builder, int indent)
    {
        Line(builder, indent, "}");
        builder.Append('\n');
    }

    private static void WriteSummary(StringBuilder builder, int indent, string summary)
    {
        Line(builder, indent, "/// <summary>");
        var normalized = summary.Replace("\r\n", "\n").Replace("\r", "\n");
        var lines = normalized.Split('\n');
        foreach (var line in lines)
        {
            Line(builder, indent, "/// " + XmlEscape(line));
        }

        Line(builder, indent, "/// </summary>");
    }

    private static void AddResource(NameNode<ResourceRecord> root, ResourceRecord resource, List<DiagnosticReport> diagnostics, bool namesAreFinal = false)
    {
        var name = resource.Name ?? "";
        var manifest = resource.ManifestName ?? "";
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(manifest))
        {
            diagnostics.Add(Error("AM0002", "资源名无效", "存在空的资源名或清单名，已忽略。"));
            return;
        }

        var segments = name.Split('.');
        var identifiers = new string[segments.Length];
        for (var i = 0; i < segments.Length; i++)
        {
            if (namesAreFinal)
            {
                if (!IdentifierSanitizer.IsValidIdentifier(segments[i]))
                {
                    diagnostics.Add(Error("AM0002", "资源名无效", "资源“" + name + "”包含无法映射为 C# 标识符的片段“" + segments[i] + "”，已忽略。"));
                    return;
                }

                identifiers[i] = segments[i];
            }
            else if (!IdentifierSanitizer.TrySanitize(segments[i], out var identifier))
            {
                diagnostics.Add(Error("AM0002", "资源名无效", "资源“" + name + "”包含无法映射为 C# 标识符的片段“" + segments[i] + "”，已忽略。"));
                return;
            }
            else
            {
                identifiers[i] = identifier;
            }
        }

        var node = root;
        var parent = "Resources";
        for (var i = 0; i < identifiers.Length; i++)
        {
            var isLeaf = i == identifiers.Length - 1;
            var identifier = namesAreFinal
                ? identifiers[i]
                : DisambiguateResource(identifiers[i], parent, i == 0, isLeaf, name, diagnostics);
            var child = node.TryDescend(identifier, isLeaf, name, "资源", diagnostics);
            if (child is null)
            {
                return;
            }

            node = child;
            parent = Bare(identifier);
        }

        node.Leaf = resource;
        node.LeafPath = name;
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

    private static void EmitResourceChildren(StringBuilder builder, NameNode<ResourceRecord> node, int indent)
    {
        foreach (var name in node.ChildOrder)
        {
            var child = node.Children[name];
            var summary = child.Leaf is null ? null : ResourceSummary(child.Leaf);
            WriteNestedClassOpen(builder, indent, child.Identifier, summary);
            if (child.Leaf is not null && child.Children.Count == 0)
            {
                WriteResourceMembers(builder, indent + 1, child.Leaf);
            }
            else
            {
                EmitResourceChildren(builder, child, indent + 1);
            }

            WriteClose(builder, indent);
        }
    }

    private static string ResourceSummary(ResourceRecord resource)
    {
        var manifest = "清单资源名 " + (resource.ManifestName ?? "") + "。";
        if (string.IsNullOrWhiteSpace(resource.Comment))
        {
            return manifest;
        }

        return resource.Comment.Trim() + " " + manifest;
    }

    private static void WriteResourceMembers(StringBuilder builder, int indent, ResourceRecord resource)
    {
        var literal = CSharpLiteral.EscapedString(resource.ManifestName ?? "");
        Line(builder, indent, "private static string _text;");
        Line(builder, indent, "public static string Text");
        Line(builder, indent, "{");
        Line(builder, indent + 1, "get");
        Line(builder, indent + 1, "{");
        Line(builder, indent + 2, "var value = _text;");
        Line(builder, indent + 2, "if (value == null)");
        Line(builder, indent + 2, "{");
        Line(builder, indent + 3, "value = Manifest.ReadText(" + literal + ");");
        Line(builder, indent + 3, "_text = value;");
        Line(builder, indent + 2, "}");
        Line(builder, indent + 2, "return value;");
        Line(builder, indent + 1, "}");
        Line(builder, indent, "}");
        builder.Append('\n');
        Line(builder, indent, "public static byte[] GetBytes() => Manifest.ReadBytes(" + literal + ");");
        builder.Append('\n');
        Line(builder, indent, "public static System.IO.Stream GetStream() => Manifest.Open(" + literal + ");");
        builder.Append('\n');
    }

    private static void WriteManifest(StringBuilder builder, int indent)
    {
        Line(builder, indent, "private static class Manifest");
        Line(builder, indent, "{");
        var inner = indent + 1;
        Line(builder, inner, "private static readonly System.Reflection.Assembly Assembly = typeof(AssemblyMeta).Assembly;");
        builder.Append('\n');
        Line(builder, inner, "public static System.IO.Stream Open(string name)");
        Line(builder, inner, "{");
        Line(builder, inner + 1, "var stream = Assembly.GetManifestResourceStream(name);");
        Line(builder, inner + 1, "if (stream == null)");
        Line(builder, inner + 1, "{");
        Line(builder, inner + 2, "throw new System.InvalidOperationException(\"找不到嵌入资源“\" + name + \"”。\");");
        Line(builder, inner + 1, "}");
        Line(builder, inner + 1, "return stream;");
        Line(builder, inner, "}");
        builder.Append('\n');
        Line(builder, inner, "public static byte[] ReadBytes(string name)");
        Line(builder, inner, "{");
        Line(builder, inner + 1, "using (var stream = Open(name))");
        Line(builder, inner + 1, "using (var memory = new System.IO.MemoryStream())");
        Line(builder, inner + 1, "{");
        Line(builder, inner + 2, "stream.CopyTo(memory);");
        Line(builder, inner + 2, "return memory.ToArray();");
        Line(builder, inner + 1, "}");
        Line(builder, inner, "}");
        builder.Append('\n');
        Line(builder, inner, "public static string ReadText(string name)");
        Line(builder, inner, "{");
        Line(builder, inner + 1, "using (var stream = Open(name))");
        Line(builder, inner + 1, "using (var reader = new System.IO.StreamReader(stream, System.Text.Encoding.UTF8, true))");
        Line(builder, inner + 1, "{");
        Line(builder, inner + 2, "return reader.ReadToEnd();");
        Line(builder, inner + 1, "}");
        Line(builder, inner, "}");
        Line(builder, indent, "}");
        builder.Append('\n');
    }

    private static string Bare(string identifier)
        => identifier.StartsWith("@", StringComparison.Ordinal) ? identifier.Substring(1) : identifier;

    private static string XmlEscape(string value)
        => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private static void Line(StringBuilder builder, int indent, string text)
    {
        if (indent > 0)
        {
            builder.Append(' ', indent * 4);
        }

        builder.Append(text);
        builder.Append('\n');
    }

    private static DiagnosticReport Error(string id, string title, string message)
        => new(id, title, message, DiagnosticSeverityKind.Error);

    private static DiagnosticReport Warning(string id, string title, string message)
        => new(id, title, message, DiagnosticSeverityKind.Warning);

    /// <summary>常量树和资源树共用的分段插入。冲突时报告 AM0001 并返回 null。</summary>
    private class NameNode<TLeaf> where TLeaf : class
    {
        public string Identifier { get; set; } = "";

        public string? LeafPath { get; set; }

        public TLeaf? Leaf { get; set; }

        public Dictionary<string, NameNode<TLeaf>> Children { get; } = new(StringComparer.Ordinal);

        public List<string> ChildOrder { get; } = new();

        public NameNode<TLeaf>? TryDescend(string identifier, bool isLeaf, string path, string kind, List<DiagnosticReport> diagnostics)
        {
            if (!Children.TryGetValue(identifier, out var existing))
            {
                if (Leaf is not null)
                {
                    diagnostics.Add(Error("AM0001", kind + "名重复", kind + "“" + path + "”与已有成员“" + LeafPath + "”冲突，已忽略。"));
                    return null;
                }

                var created = CreateChild(identifier);
                Children.Add(identifier, created);
                ChildOrder.Add(identifier);
                return created;
            }

            if (isLeaf && (existing.Leaf is not null || existing.Children.Count > 0))
            {
                diagnostics.Add(Error("AM0001", kind + "名重复", kind + "“" + path + "”与已有成员冲突，已忽略。"));
                return null;
            }

            if (!isLeaf && existing.Leaf is not null)
            {
                diagnostics.Add(Error("AM0001", kind + "名重复", kind + "“" + path + "”与已有成员“" + existing.LeafPath + "”冲突，已忽略。"));
                return null;
            }

            return existing;
        }

        protected virtual NameNode<TLeaf> CreateChild(string identifier)
            => new() { Identifier = identifier };
    }

    private sealed class Node : NameNode<ConstantRecord>
    {
        public string TypeName { get; set; } = "string";

        public string? Rhs { get; set; }

        public bool UseConst { get; set; }

        public string? Comment { get; set; }

        protected override NameNode<ConstantRecord> CreateChild(string identifier)
            => new Node { Identifier = identifier };
    }

    private readonly struct NamespaceScope : IDisposable
    {
        private readonly StringBuilder _builder;
        private readonly bool _hasNamespace;

        public NamespaceScope(StringBuilder builder, string? ns)
        {
            _builder = builder;
            _hasNamespace = !string.IsNullOrEmpty(ns);
            Indent = 0;
            if (_hasNamespace)
            {
                builder.Append("namespace ").Append(ns).Append("\n{\n");
                Indent = 1;
            }
        }

        public int Indent { get; }

        public void Dispose()
        {
            if (_hasNamespace)
            {
                _builder.Append("}\n");
            }
        }
    }
}
