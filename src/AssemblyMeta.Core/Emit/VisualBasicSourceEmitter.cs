using AssemblyMeta.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace AssemblyMeta.Core.Emit;

/// <summary>把同一组模型写成 Visual Basic。不引用 Roslyn，也不改 C# 发射。</summary>
internal sealed class VisualBasicSourceEmitter
{
    public EmitResult EmitAssemblyInfo(AssemblyInfoModel model)
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
            WriteNestedClassOpen(builder, indent, "Info", "Constants projected from the SDK-generated assembly attributes. / SDK 默认生成的程序集特性常量。");
            indent++;
            WriteStringMember(builder, indent, model.Visibility, "Configuration", model.Configuration, "Maps AssemblyConfigurationAttribute. / 对应 AssemblyConfigurationAttribute。");
            WriteStringMember(builder, indent, model.Visibility, "Company", model.Company, "Maps AssemblyCompanyAttribute. / 对应 AssemblyCompanyAttribute。");
            WriteStringMember(builder, indent, model.Visibility, "Title", model.Title, "Maps AssemblyTitleAttribute. / 对应 AssemblyTitleAttribute。");
            WriteStringMember(builder, indent, model.Visibility, "Description", model.Description, "Maps AssemblyDescriptionAttribute. / 对应 AssemblyDescriptionAttribute。");
            WriteStringMember(builder, indent, model.Visibility, "Product", model.Product, "Maps AssemblyProductAttribute. / 对应 AssemblyProductAttribute。");
            WriteStringMember(builder, indent, model.Visibility, "Copyright", model.Copyright, "Maps AssemblyCopyrightAttribute. / 对应 AssemblyCopyrightAttribute。");
            WriteStringMember(builder, indent, model.Visibility, "Version", model.Version, "Maps AssemblyVersionAttribute. Uses AssemblyVersion, not the NuGet Version. / 对应 AssemblyVersionAttribute。取 AssemblyVersion，不是 NuGet Version。");
            WriteStringMember(builder, indent, model.Visibility, "InformationalVersion", model.InformationalVersion, "Maps AssemblyInformationalVersionAttribute. / 对应 AssemblyInformationalVersionAttribute。");
            WriteStringMember(builder, indent, model.Visibility, "FileVersion", model.FileVersion, "Maps AssemblyFileVersionAttribute. / 对应 AssemblyFileVersionAttribute。");
            indent--;
            WriteClassClose(builder, indent);
            indent--;
            WriteModuleClose(builder, indent);
        }

        return new EmitResult { Source = builder.ToString(), Diagnostics = diagnostics };
    }

    public EmitResult EmitConstants(ConstantsModel model)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        return EmitArea(model, "AssemblyMeta.Constants", "Constants", "Compile-time constants generated from Constant / FileConstant MSBuild items. / 由 Constant / FileConstant MSBuild 项生成的编译期常量。", null);
    }

    public EmitResult EmitProject(ConstantsModel model)
        => EmitArea(model, "AssemblyMeta.Project", "Project", "Compile-time constants generated from ProjectProperty items. / 由 ProjectProperty 项生成的项目属性常量。", "Project property {0}. / 项目属性 {0}。");

    public EmitResult EmitMetadata(ConstantsModel model)
        => EmitArea(model, "AssemblyMeta.Metadata", "Metadata", "Compile-time constants generated from AssemblyMetadata items. / 由 AssemblyMetadata 项生成的程序集元数据常量。", "Assembly metadata {0}. / 程序集元数据 {0}。");

    public EmitResult EmitGit(GitModel model)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        var diagnostics = new List<DiagnosticReport>();
        if (model.Unavailable)
        {
            diagnostics.Add(Warning("AM0006", "Git information unavailable / Git 信息不可用", "Could not obtain the git commit hash. Check the project is inside a git repository and EnableSourceControlManagerQueries is true. / 未能取得 Git 提交号。确认项目位于 Git 仓库内，且 EnableSourceControlManagerQueries 为 true。"));
        }

        var ns = ResolveNamespace(model.Namespace, diagnostics);
        var builder = new StringBuilder();
        WriteHeader(builder, "AssemblyMeta.Git");
        using (var scope = new NamespaceScope(builder, ns))
        {
            var indent = scope.Indent;
            WriteRoot(builder, indent, model.Visibility);
            indent++;
            WriteNestedClassOpen(builder, indent, "Git", "Compile-time git information. Design-time builds do not query and produce [pending build]. / 编译期 Git 信息。设计时构建默认不查询，值为 [pending build]。");
            indent++;
            WriteStringMember(builder, indent, model.Visibility, "Commit", model.Commit, "Full hash of the current commit. / 当前提交的完整哈希。");
            WriteStringMember(builder, indent, model.Visibility, "Sha", model.Sha, "Uses RepositorySha verbatim when set; otherwise the first 9 characters of Commit, or the full length when shorter. / RepositorySha 已有值时原样采用。否则是 Commit 的前 9 位；不足 9 位时用全长。");
            WriteStringMember(builder, indent, model.Visibility, "Root", model.Root, "Repository root with forward slashes, ending in a slash when non-empty. / 仓库根目录，使用正斜杠，非空时以斜杠结尾。");
            WriteStringMember(builder, indent, model.Visibility, "Url", model.Url, "Repository URL. Only filled when PublishRepositoryUrl=true or RepositoryUrl is already set. / 仓库地址。仅当 PublishRepositoryUrl=true，或已经设置 RepositoryUrl 时填充。");
            WriteStringMember(builder, indent, model.Visibility, "Branch", model.Branch, "Current branch; pull requests produce \"pr\" plus the number. / 当前分支。拉取请求场景为 pr 加编号。");
            indent--;
            WriteClassClose(builder, indent);
            indent--;
            WriteModuleClose(builder, indent);
        }

        return new EmitResult { Source = builder.ToString(), Diagnostics = diagnostics };
    }

    public EmitResult EmitResources(ResourcesModel model)
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
        if (model.Resources is not null)
        {
            foreach (var resource in model.Resources)
            {
                AddResource(root, resource, diagnostics, model.NamesAreFinal);
            }
        }

        var builder = new StringBuilder();
        WriteHeader(builder, "AssemblyMeta.Resources");
        using (var scope = new NamespaceScope(builder, ns))
        {
            var indent = scope.Indent;
            WriteRoot(builder, indent, model.Visibility);
            indent++;
            WriteNestedClassOpen(builder, indent, "Resources", "Strongly-typed access to embedded resources. Text is read as UTF-8 and cached; GetBytes and GetStream reopen the resource. / 嵌入资源的强类型访问。Text 按 UTF-8 读取并缓存，GetBytes 与 GetStream 每次重新打开。");
            indent++;
            var wroteAny = EmitResourceChildren(builder, root, indent);
            if (wroteAny)
            {
                WriteManifest(builder, indent);
            }

            indent--;
            WriteClassClose(builder, indent);
            indent--;
            WriteModuleClose(builder, indent);
        }

        return new EmitResult { Source = builder.ToString(), Diagnostics = diagnostics };
    }

    public EmitResult EmitStrings(ConstantsModel model)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        return EmitArea(model, "AssemblyMeta.Strings", "Strings", "Compile-time constants generated from neutral .resx files; satellite cultures are not included. / 由中性 resx 生成的编译期字符串常量。卫星文化资源不参与生成。", "String resource {0}. / 字符串资源 {0}。");
    }

    private static EmitResult EmitArea(ConstantsModel model, string packageId, string className, string classSummary, string? defaultCommentFormat)
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
                var comment = constant.Comment;
                if (string.IsNullOrWhiteSpace(comment) && defaultCommentFormat is not null)
                {
                    comment = string.Format(defaultCommentFormat, constant.Name ?? "");
                }

                AddConstant(root, new ConstantRecord(constant.Name ?? "", constant.Value ?? "", constant.EffectiveType, comment ?? ""), model.Visibility, diagnostics, className, model.NamesAreFinal);
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
            EmitConstantChildren(builder, root, indent);
            indent--;
            WriteClassClose(builder, indent);
            indent--;
            WriteModuleClose(builder, indent);
        }

        return new EmitResult { Source = builder.ToString(), Diagnostics = diagnostics };
    }

    private static void AddConstant(Node root, ConstantRecord constant, MemberVisibility visibility, List<DiagnosticReport> diagnostics, string enclosingType, bool namesAreFinal)
    {
        var name = constant.Name ?? "";
        if (!TrySegments(name, namesAreFinal, "constant", "常量", diagnostics, out var identifiers))
        {
            return;
        }

        var typeName = constant.EffectiveType;
        if (!IdentifierSanitizer.IsStringType(typeName) && string.IsNullOrWhiteSpace(constant.Value))
        {
            diagnostics.Add(Error("AM0003", "Invalid constant value / 常量值无效", "Constant “" + name + "” has a non-string type but an empty Value and was ignored. / 常量“" + name + "”的类型不是 string，但 Value 为空，已忽略。"));
            return;
        }

        if (!VisualBasicIdentifiers.TryMapType(typeName, out var visualBasicType))
        {
            diagnostics.Add(Error("AM0013", "Type cannot be written in Visual Basic / 类型无法写入 Visual Basic", "Constant “" + name + "” has type “" + typeName + "”, which is outside the Visual Basic constant map, and was skipped. / 常量“" + name + "”的类型“" + typeName + "”不在 Visual Basic 常量映射表内，已跳过。"));
            return;
        }

        var needsRuntime = false;
        string rhs;
        if (IdentifierSanitizer.IsStringType(typeName))
        {
            rhs = VisualBasicIdentifiers.FormatString(constant.Value, out needsRuntime);
        }
        else if (!VisualBasicIdentifiers.TryFormatValue(typeName, constant.Value, out rhs))
        {
            diagnostics.Add(Error("AM0013", "Type cannot be written in Visual Basic / 类型无法写入 Visual Basic", "Constant “" + name + "” has value “" + (constant.Value ?? "") + "”, which cannot be written as a Visual Basic literal, and was skipped. / 常量“" + name + "”的值“" + (constant.Value ?? "") + "”无法写成 Visual Basic 字面量，已跳过。"));
            return;
        }

        if (ConflictsWithEnclosing(identifiers, enclosingType, name, "constant", "常量", diagnostics))
        {
            return;
        }

        if (!TryInsert(root, identifiers, name, "constant", "常量", diagnostics, out var node))
        {
            return;
        }

        node.HasLeaf = true;
        node.LeafPath = name;
        node.TypeName = visualBasicType;
        node.Rhs = rhs;
        node.UseConst = visibility != MemberVisibility.Public && IdentifierSanitizer.IsConstEligible(typeName) && !needsRuntime;
        node.Comment = constant.Comment;
    }

    private static void AddResource(Node root, ResourceRecord resource, List<DiagnosticReport> diagnostics, bool namesAreFinal)
    {
        var name = resource.Name ?? "";
        var manifest = resource.ManifestName ?? "";
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(manifest))
        {
            if (!namesAreFinal)
            {
                diagnostics.Add(Error("AM0002", "Invalid resource name / 资源名无效", "An empty resource or manifest name was ignored. / 存在空的资源名或清单名，已忽略。"));
            }

            return;
        }

        if (!TrySegments(name, namesAreFinal, "resource", "资源", diagnostics, out var identifiers))
        {
            return;
        }

        if (ConflictsWithEnclosing(identifiers, "Resources", name, "resource", "资源", diagnostics))
        {
            return;
        }

        if (ConflictsWithGeneratedResource(identifiers, name, diagnostics))
        {
            return;
        }

        if (!TryInsert(root, identifiers, name, "resource", "资源", diagnostics, out var node))
        {
            return;
        }

        node.HasLeaf = true;
        node.LeafPath = name;
        node.Resource = resource;
        node.Comment = resource.Comment;
    }

    private static bool TrySegments(string name, bool namesAreFinal, string kindEn, string kindZh, List<DiagnosticReport> diagnostics, out string[] identifiers)
    {
        identifiers = Array.Empty<string>();
        if (string.IsNullOrWhiteSpace(name))
        {
            if (!namesAreFinal)
            {
                diagnostics.Add(Error("AM0002", "Invalid " + kindEn + " name / " + kindZh + "名无效", "An empty " + kindEn + " name was ignored. / 存在空的" + kindZh + "名，已忽略。"));
            }

            return false;
        }

        var segments = name.Split('.');
        identifiers = new string[segments.Length];
        for (var i = 0; i < segments.Length; i++)
        {
            if (namesAreFinal)
            {
                if (!IdentifierSanitizer.IsValidIdentifier(segments[i]))
                {
                    return false;
                }

                identifiers[i] = segments[i];
                continue;
            }

            if (!IdentifierSanitizer.TrySanitize(segments[i], out var identifier))
            {
                diagnostics.Add(Error("AM0002", "Invalid " + kindEn + " name / " + kindZh + "名无效", "The " + kindEn + " “" + name + "” contains segment “" + segments[i] + "” that cannot be mapped to an identifier and was ignored. / " + kindZh + "“" + name + "”包含无法映射为标识符的片段“" + segments[i] + "”，已忽略。"));
                return false;
            }

            identifiers[i] = identifier;
        }

        return true;
    }

    private static bool ConflictsWithEnclosing(string[] identifiers, string enclosingType, string path, string kindEn, string kindZh, List<DiagnosticReport> diagnostics)
    {
        var parent = enclosingType;
        for (var i = 0; i < identifiers.Length; i++)
        {
            var bare = VisualBasicIdentifiers.Bare(identifiers[i]);
            if (parent.Length > 0 && string.Equals(bare, parent, StringComparison.OrdinalIgnoreCase))
            {
                diagnostics.Add(Error(
                    "AM0001",
                    "Duplicate " + kindEn + " name / " + kindZh + "名重复",
                    CaseMessage(kindEn, kindZh, path, parent)));
                return true;
            }

            parent = bare;
        }

        return false;
    }

    private static bool ConflictsWithGeneratedResource(string[] identifiers, string path, List<DiagnosticReport> diagnostics)
    {
        var root = VisualBasicIdentifiers.Bare(identifiers[0]);
        if (string.Equals(root, "Manifest", StringComparison.OrdinalIgnoreCase))
        {
            diagnostics.Add(Error("AM0001", "Duplicate resource name / 资源名重复", CaseMessage("resource", "资源", path, "Manifest")));
            return true;
        }

        var leaf = VisualBasicIdentifiers.Bare(identifiers[identifiers.Length - 1]);
        if (string.Equals(leaf, "Text", StringComparison.OrdinalIgnoreCase)
            || string.Equals(leaf, "GetBytes", StringComparison.OrdinalIgnoreCase)
            || string.Equals(leaf, "GetStream", StringComparison.OrdinalIgnoreCase))
        {
            var generated = string.Equals(leaf, "Text", StringComparison.OrdinalIgnoreCase)
                ? "Text"
                : string.Equals(leaf, "GetBytes", StringComparison.OrdinalIgnoreCase) ? "GetBytes" : "GetStream";
            diagnostics.Add(Error("AM0001", "Duplicate resource name / 资源名重复", CaseMessage("resource", "资源", path, generated)));
            return true;
        }

        return false;
    }

    private static bool TryInsert(Node root, string[] identifiers, string path, string kindEn, string kindZh, List<DiagnosticReport> diagnostics, out Node node)
    {
        node = root;
        for (var i = 0; i < identifiers.Length; i++)
        {
            var child = TryDescend(node, identifiers[i], i == identifiers.Length - 1, path, kindEn, kindZh, diagnostics);
            if (child is null)
            {
                return false;
            }

            node = child;
        }

        return true;
    }

    private static Node? TryDescend(Node parent, string identifier, bool isLeaf, string path, string kindEn, string kindZh, List<DiagnosticReport> diagnostics)
    {
        var bare = VisualBasicIdentifiers.Bare(identifier);
        var title = "Duplicate " + kindEn + " name / " + kindZh + "名重复";
        if (!parent.Children.TryGetValue(bare, out var existing))
        {
            if (parent.HasLeaf)
            {
                diagnostics.Add(Error("AM0001", title, CaseMessage(kindEn, kindZh, path, parent.LeafPath ?? parent.Bare)));
                return null;
            }

            var created = new Node
            {
                Bare = bare,
                Display = VisualBasicIdentifiers.Escape(identifier),
            };
            parent.Children.Add(bare, created);
            parent.ChildOrder.Add(bare);
            return created;
        }

        if (isLeaf && (existing.HasLeaf || existing.Children.Count > 0))
        {
            diagnostics.Add(Error("AM0001", title, CaseMessage(kindEn, kindZh, path, existing.LeafPath ?? existing.Bare)));
            return null;
        }

        if (!isLeaf && existing.HasLeaf)
        {
            diagnostics.Add(Error("AM0001", title, CaseMessage(kindEn, kindZh, path, existing.LeafPath ?? existing.Bare)));
            return null;
        }

        return existing;
    }

    private static string CaseMessage(string kindEn, string kindZh, string path, string other)
        => "The " + kindEn + " “" + path + "” differs only by case from “" + other + "” and was ignored. / " + kindZh + "“" + path + "”与“" + other + "”只差大小写，已忽略。";

    private static void EmitConstantChildren(StringBuilder builder, Node node, int indent)
    {
        foreach (var name in node.ChildOrder)
        {
            var child = node.Children[name];
            if (!HasContent(child))
            {
                continue;
            }

            if (child.HasLeaf && child.Children.Count == 0)
            {
                WriteConstant(builder, indent, child);
                continue;
            }

            WriteNestedClassOpen(builder, indent, child.Display, null);
            EmitConstantChildren(builder, child, indent + 1);
            WriteClassClose(builder, indent);
        }
    }

    private static bool EmitResourceChildren(StringBuilder builder, Node node, int indent)
    {
        var wrote = false;
        foreach (var name in node.ChildOrder)
        {
            var child = node.Children[name];
            if (!HasContent(child))
            {
                continue;
            }

            wrote = true;
            var summary = child.Resource is null ? null : ResourceSummary(child.Resource);
            WriteNestedClassOpen(builder, indent, child.Display, summary);
            if (child.Resource is not null && child.Children.Count == 0)
            {
                WriteResourceMembers(builder, indent + 1, child.Resource);
            }
            else
            {
                EmitResourceChildren(builder, child, indent + 1);
            }

            WriteClassClose(builder, indent);
        }

        return wrote;
    }

    private static bool HasContent(Node node)
    {
        if (node.HasLeaf)
        {
            return true;
        }

        foreach (var child in node.Children.Values)
        {
            if (HasContent(child))
            {
                return true;
            }
        }

        return false;
    }

    private static void WriteConstant(StringBuilder builder, int indent, Node node)
    {
        if (!string.IsNullOrWhiteSpace(node.Comment))
        {
            WriteSummary(builder, indent, node.Comment!);
        }

        var keyword = node.UseConst ? "Const" : "Shared ReadOnly";
        Line(builder, indent, "Public " + keyword + " " + node.Display + " As " + node.TypeName + " = " + (node.Rhs ?? "\"\""));
        builder.Append('\n');
    }

    private static void WriteStringMember(StringBuilder builder, int indent, MemberVisibility visibility, string name, string? value, string summary)
    {
        WriteSummary(builder, indent, summary);
        var rhs = VisualBasicIdentifiers.FormatString(value, out var needsRuntime);
        var useConst = visibility != MemberVisibility.Public && !needsRuntime;
        var keyword = useConst ? "Const" : "Shared ReadOnly";
        Line(builder, indent, "Public " + keyword + " " + VisualBasicIdentifiers.Escape(name) + " As String = " + rhs);
        builder.Append('\n');
    }

    private static void WriteResourceMembers(StringBuilder builder, int indent, ResourceRecord resource)
    {
        var literal = VisualBasicIdentifiers.FormatString(resource.ManifestName, out _);
        Line(builder, indent, "Private Shared _loaded As Boolean");
        Line(builder, indent, "Private Shared _text As String");
        builder.Append('\n');
        Line(builder, indent, "Public Shared ReadOnly Property Text As String");
        Line(builder, indent, "Get");
        Line(builder, indent + 1, "If Not _loaded Then");
        Line(builder, indent + 2, "_text = Manifest.ReadText(" + literal + ")");
        Line(builder, indent + 2, "_loaded = True");
        Line(builder, indent + 1, "End If");
        Line(builder, indent + 1, "Return _text");
        Line(builder, indent, "End Get");
        Line(builder, indent, "End Property");
        builder.Append('\n');
        Line(builder, indent, "Public Shared Function GetBytes() As Byte()");
        Line(builder, indent + 1, "Return Manifest.ReadBytes(" + literal + ")");
        Line(builder, indent, "End Function");
        builder.Append('\n');
        Line(builder, indent, "Public Shared Function GetStream() As System.IO.Stream");
        Line(builder, indent + 1, "Return Manifest.Open(" + literal + ")");
        Line(builder, indent, "End Function");
        builder.Append('\n');
    }

    private static void WriteManifest(StringBuilder builder, int indent)
    {
        Line(builder, indent, "Private Class Manifest");
        var inner = indent + 1;
        Line(builder, inner, "Private Shared ReadOnly Assembly As System.Reflection.Assembly = GetType(AssemblyMeta).Assembly");
        builder.Append('\n');
        Line(builder, inner, "Public Shared Function Open(name As String) As System.IO.Stream");
        Line(builder, inner + 1, "Dim stream = Assembly.GetManifestResourceStream(name)");
        Line(builder, inner + 1, "If stream Is Nothing Then");
        Line(builder, inner + 2, "Throw New System.InvalidOperationException(\"Embedded resource “\" & name & \"” was not found. / 找不到嵌入资源“\" & name & \"”。\")");
        Line(builder, inner + 1, "End If");
        Line(builder, inner + 1, "Return stream");
        Line(builder, inner, "End Function");
        builder.Append('\n');
        Line(builder, inner, "Public Shared Function ReadBytes(name As String) As Byte()");
        Line(builder, inner + 1, "Using stream As System.IO.Stream = Open(name)");
        Line(builder, inner + 2, "Using memory As New System.IO.MemoryStream()");
        Line(builder, inner + 3, "stream.CopyTo(memory)");
        Line(builder, inner + 3, "Return memory.ToArray()");
        Line(builder, inner + 2, "End Using");
        Line(builder, inner + 1, "End Using");
        Line(builder, inner, "End Function");
        builder.Append('\n');
        Line(builder, inner, "Public Shared Function ReadText(name As String) As String");
        Line(builder, inner + 1, "Using stream As System.IO.Stream = Open(name)");
        Line(builder, inner + 2, "Using reader As New System.IO.StreamReader(stream, System.Text.Encoding.UTF8, True)");
        Line(builder, inner + 3, "Return reader.ReadToEnd()");
        Line(builder, inner + 2, "End Using");
        Line(builder, inner + 1, "End Using");
        Line(builder, inner, "End Function");
        Line(builder, indent, "End Class");
        builder.Append('\n');
    }

    private static string ResourceSummary(ResourceRecord resource)
    {
        var manifest = "Manifest resource name " + (resource.ManifestName ?? "") + ". / 清单资源名 " + (resource.ManifestName ?? "") + "。";
        if (string.IsNullOrWhiteSpace(resource.Comment))
        {
            return manifest;
        }

        return resource.Comment.Trim() + " " + manifest;
    }

    private static string? ResolveNamespace(string? ns, List<DiagnosticReport> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(ns))
        {
            return null;
        }

        if (!VisualBasicIdentifiers.TryEscapeNamespace(ns!, out var escaped) || string.IsNullOrEmpty(escaped))
        {
            diagnostics.Add(Warning("AM0005", "Invalid namespace / 命名空间无效", "AssemblyMetaNamespace “" + ns + "” is not a valid namespace and fell back to the global namespace. / AssemblyMetaNamespace“" + ns + "”不是有效的命名空间，已回退到全局命名空间。"));
            return null;
        }

        return escaped;
    }

    private static void WriteHeader(StringBuilder builder, string packageId)
    {
        builder.Append("' <auto-generated/>\n");
        builder.Append("' Generated by ").Append(packageId).Append(" at compile time. Do not edit manually. / 由 ").Append(packageId).Append(" 在编译期生成，请勿手动修改。\n");
        builder.Append("Option Strict On\n");
        builder.Append("Option Explicit On\n\n");
    }

    private static void WriteRoot(StringBuilder builder, int indent, MemberVisibility visibility)
    {
        var keyword = visibility == MemberVisibility.Public ? "Public" : "Friend";
        Line(builder, indent, keyword + " Partial Module AssemblyMeta");
    }

    private static void WriteNestedClassOpen(StringBuilder builder, int indent, string identifier, string? summary)
    {
        if (!string.IsNullOrEmpty(summary))
        {
            WriteSummary(builder, indent, summary!);
        }

        Line(builder, indent, "Public Class " + identifier);
    }

    private static void WriteClassClose(StringBuilder builder, int indent)
    {
        Line(builder, indent, "End Class");
        builder.Append('\n');
    }

    private static void WriteModuleClose(StringBuilder builder, int indent)
    {
        Line(builder, indent, "End Module");
        builder.Append('\n');
    }

    private static void WriteSummary(StringBuilder builder, int indent, string summary)
    {
        Line(builder, indent, "''' <summary>");
        var normalized = summary.Replace("\r\n", "\n").Replace("\r", "\n");
        var lines = normalized.Split('\n');
        foreach (var line in lines)
        {
            Line(builder, indent, "''' " + XmlEscape(line));
        }

        Line(builder, indent, "''' </summary>");
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
        public string Bare { get; set; } = "";

        public string Display { get; set; } = "";

        public string? LeafPath { get; set; }

        public bool HasLeaf { get; set; }

        public string TypeName { get; set; } = "String";

        public string? Rhs { get; set; }

        public bool UseConst { get; set; }

        public string? Comment { get; set; }

        public ResourceRecord? Resource { get; set; }

        public Dictionary<string, Node> Children { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<string> ChildOrder { get; } = new();
    }

    private readonly struct NamespaceScope : IDisposable
    {
        private readonly StringBuilder _builder;
        private readonly bool _hasNamespace;

        public NamespaceScope(StringBuilder builder, string? ns)
        {
            _builder = builder;
            _hasNamespace = true;
            // VB prepends RootNamespace to every Namespace declaration. Global. keeps the
            // emitted namespace absolute, matching the C# emitter's explicit namespace.
            builder.Append("Namespace Global");
            if (!string.IsNullOrEmpty(ns))
            {
                builder.Append('.').Append(ns);
            }

            builder.Append("\n");
            Indent = 1;
        }

        public int Indent { get; }

        public void Dispose()
        {
            if (_hasNamespace)
            {
                _builder.Append("End Namespace\n");
            }
        }
    }
}