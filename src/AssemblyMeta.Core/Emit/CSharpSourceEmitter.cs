using AssemblyMeta.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace AssemblyMeta.Core.Emit;

/// <summary>把 AssemblyInfo / Constants 模型写成 C#。不引用 Roslyn，便于任务程序集复用同一套模型。</summary>
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
        var ns = ResolveNamespace(model.Namespace, diagnostics);
        var root = new Node();
        if (model.Constants is not null)
        {
            foreach (var constant in model.Constants)
            {
                AddConstant(root, constant, model.Visibility, csharpLanguageVersion, diagnostics);
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

    private static void AddConstant(Node root, ConstantRecord constant, MemberVisibility visibility, int languageVersion, List<DiagnosticReport> diagnostics)
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
            if (!IdentifierSanitizer.TrySanitize(segments[i], out var identifier))
            {
                diagnostics.Add(Error("AM0002", "常量名无效", "常量“" + name + "”包含无法映射为 C# 标识符的片段“" + segments[i] + "”，已忽略。"));
                return;
            }

            identifiers[i] = identifier;
        }

        var typeName = constant.EffectiveType;
        if (!IdentifierSanitizer.IsStringType(typeName) && string.IsNullOrWhiteSpace(constant.Value))
        {
            diagnostics.Add(Error("AM0003", "常量值无效", "常量“" + name + "”的类型不是 string，但 Value 为空，已忽略。"));
            return;
        }

        var node = root;
        for (var i = 0; i < identifiers.Length; i++)
        {
            var isLeaf = i == identifiers.Length - 1;
            if (!node.Children.TryGetValue(identifiers[i], out var child))
            {
                if (node.Leaf is not null)
                {
                    diagnostics.Add(Error("AM0001", "常量名重复", "常量“" + name + "”与已有成员“" + node.LeafPath + "”冲突，已忽略。"));
                    return;
                }

                child = new Node { Identifier = identifiers[i] };
                node.Children.Add(identifiers[i], child);
                node.ChildOrder.Add(identifiers[i]);
            }
            else if (isLeaf && (child.Leaf is not null || child.Children.Count > 0))
            {
                diagnostics.Add(Error("AM0001", "常量名重复", "常量“" + name + "”与已有成员冲突，已忽略。"));
                return;
            }
            else if (!isLeaf && child.Leaf is not null)
            {
                diagnostics.Add(Error("AM0001", "常量名重复", "常量“" + name + "”与已有成员“" + child.LeafPath + "”冲突，已忽略。"));
                return;
            }

            node = child;
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
            var child = node.Children[name];
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

    private sealed class Node
    {
        public string Identifier { get; set; } = "";

        public string? LeafPath { get; set; }

        public ConstantRecord? Leaf { get; set; }

        public string TypeName { get; set; } = "string";

        public string? Rhs { get; set; }

        public bool UseConst { get; set; }

        public string? Comment { get; set; }

        public Dictionary<string, Node> Children { get; } = new(StringComparer.Ordinal);

        public List<string> ChildOrder { get; } = new();
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
