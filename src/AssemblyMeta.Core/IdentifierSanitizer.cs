using System;
using System.Collections.Generic;
using System.Text;

namespace AssemblyMeta.Core;

/// <summary>把任意片段收成合法的 C# 标识符。连续非法字符折叠成一个下划线。</summary>
public static class IdentifierSanitizer
{
    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class",
        "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event",
        "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if",
        "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new",
        "null", "object", "operator", "out", "override", "params", "private", "protected", "public",
        "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static",
        "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong",
        "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while",
        "add", "alias", "and", "ascending", "args", "async", "await", "by", "descending", "dynamic",
        "equals", "file", "from", "get", "global", "group", "init", "into", "join", "let", "managed",
        "nameof", "nint", "not", "notnull", "nuint", "on", "or", "orderby", "partial", "record",
        "remove", "required", "scoped", "select", "set", "unmanaged", "value", "var", "when", "where",
        "with", "yield",
    };

    public static bool IsKeyword(string identifier)
        => Keywords.Contains(identifier);

    public static string EscapeKeyword(string identifier)
        => Keywords.Contains(identifier) ? "@" + identifier : identifier;

    public static bool IsValidIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        var value = name[0] == '@' ? name.Substring(1) : name;
        if (value.Length == 0 || !(char.IsLetter(value[0]) || value[0] == '_'))
        {
            return false;
        }

        for (var i = 1; i < value.Length; i++)
        {
            if (!(char.IsLetterOrDigit(value[i]) || value[i] == '_'))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>消毒单个路径片段。无法生成标识符时返回 false。</summary>
    public static bool TrySanitize(string segment, out string identifier)
    {
        identifier = "";
        if (segment is null)
        {
            return false;
        }

        var builder = new StringBuilder(segment.Length + 1);
        var pendingUnderscore = false;
        foreach (var ch in segment)
        {
            if (ch == '_' || char.IsLetter(ch))
            {
                if (pendingUnderscore)
                {
                    builder.Append('_');
                    pendingUnderscore = false;
                }

                builder.Append(ch);
            }
            else if (char.IsDigit(ch))
            {
                if (builder.Length == 0)
                {
                    builder.Append('_');
                }
                else if (pendingUnderscore)
                {
                    builder.Append('_');
                }

                pendingUnderscore = false;
                builder.Append(ch);
            }
            else
            {
                pendingUnderscore = true;
            }
        }

        if (pendingUnderscore && builder.Length > 0)
        {
            builder.Append('_');
        }

        if (builder.Length == 0)
        {
            return false;
        }

        identifier = EscapeKeyword(builder.ToString());
        return true;
    }

    public static bool TryEscapeNamespace(string ns, out string escaped)
    {
        escaped = "";
        if (string.IsNullOrWhiteSpace(ns))
        {
            return true;
        }

        var parts = ns.Trim().Split('.');
        if (parts.Length == 0)
        {
            return false;
        }

        var escapedParts = new string[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i].Trim();
            if (!IsValidIdentifier(part) || part[0] == '@')
            {
                return false;
            }

            escapedParts[i] = EscapeKeyword(part);
        }

        escaped = string.Join(".", escapedParts);
        return true;
    }

    public static bool IsStringType(string typeName)
    {
        var trimmed = typeName.Trim();
        return trimmed.Equals("string", StringComparison.Ordinal)
            || trimmed.Equals("System.String", StringComparison.Ordinal)
            || trimmed.Equals("global::System.String", StringComparison.Ordinal);
    }

    public static bool IsConstEligible(string typeName)
    {
        switch (typeName.Trim())
        {
            case "bool":
            case "byte":
            case "sbyte":
            case "char":
            case "decimal":
            case "double":
            case "float":
            case "int":
            case "long":
            case "short":
            case "string":
            case "uint":
            case "ulong":
            case "ushort":
            case "System.Boolean":
            case "System.Byte":
            case "System.SByte":
            case "System.Char":
            case "System.Decimal":
            case "System.Double":
            case "System.Single":
            case "System.Int16":
            case "System.Int32":
            case "System.Int64":
            case "System.UInt16":
            case "System.UInt32":
            case "System.UInt64":
            case "System.String":
                return true;
            default:
                return false;
        }
    }
}
