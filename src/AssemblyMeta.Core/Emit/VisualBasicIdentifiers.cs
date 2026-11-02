using System;
using AssemblyMeta.Core;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AssemblyMeta.Core.Emit;

/// <summary>VB 标识符、类型映射和字面量。不引用 Roslyn。大小写比较用序号规则。</summary>
internal static class VisualBasicIdentifiers
{
    private static readonly HashSet<string> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "AddHandler", "AddressOf", "Alias", "And", "AndAlso", "As", "Boolean", "ByRef", "Byte", "ByVal",
        "Call", "Case", "Catch", "CBool", "CByte", "CChar", "CDate", "CDbl", "CDec", "Char", "CInt",
        "Class", "CLng", "CObj", "Const", "Continue", "CSByte", "CShort", "CSng", "CStr", "CType",
        "CUInt", "CULng", "CUShort", "Date", "Decimal", "Declare", "Default", "Delegate", "Dim",
        "DirectCast", "Do", "Double", "Each", "Else", "ElseIf", "End", "EndIf", "Enum", "Erase",
        "Error", "Event", "Exit", "False", "Finally", "For", "Friend", "Function", "Get", "GetType",
        "GetXMLNamespace", "Global", "GoSub", "GoTo", "Handles", "If", "Implements", "Imports", "In",
        "Inherits", "Integer", "Interface", "Is", "IsNot", "Let", "Lib", "Like", "Long", "Loop", "Me",
        "Mod", "Module", "MustInherit", "MustOverride", "MyBase", "MyClass", "NameOf", "Namespace",
        "Narrowing", "New", "Next", "Not", "Nothing", "NotInheritable", "NotOverridable", "Object",
        "Of", "On", "Operator", "Option", "Optional", "Or", "OrElse", "Out", "Overloads",
        "Overridable", "Overrides", "ParamArray", "Partial", "Private", "Property", "Protected",
        "Public", "RaiseEvent", "ReadOnly", "ReDim", "REM", "RemoveHandler", "Resume", "Return",
        "SByte", "Select", "Set", "Shadows", "Shared", "Short", "Single", "Static", "Step", "Stop",
        "String", "Structure", "Sub", "SyncLock", "Then", "Throw", "To", "True", "Try", "TryCast",
        "TypeOf", "UInteger", "ULong", "UShort", "Using", "Variant", "Wend", "When", "While",
        "Widening", "With", "WithEvents", "WriteOnly", "Xor",
    };

    public static string Bare(string identifier)
        => identifier.Length > 0 && identifier[0] == '@' ? identifier.Substring(1) : identifier;

    public static bool IsKeyword(string identifier)
        => Keywords.Contains(Bare(identifier));

    public static string Escape(string identifier)
    {
        var bare = Bare(identifier);
        return IsKeyword(bare) ? "[" + bare + "]" : bare;
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
            if (!IdentifierSanitizer.IsValidIdentifier(part) || part[0] == '@')
            {
                return false;
            }

            escapedParts[i] = Escape(part);
        }

        escaped = string.Join(".", escapedParts);
        return true;
    }

    /// <summary>只映射能做常量的 C# 别名和对应 System 名。字符串别名也映射，即使它不是 const 合格类型。</summary>
    public static bool TryMapType(string typeName, out string visualBasicType)
    {
        var trimmed = typeName.Trim();
        if (IdentifierSanitizer.IsStringType(trimmed))
        {
            visualBasicType = "String";
            return true;
        }

        switch (trimmed)
        {
            case "bool":
            case "System.Boolean":
                visualBasicType = "Boolean";
                return true;
            case "byte":
            case "System.Byte":
                visualBasicType = "Byte";
                return true;
            case "sbyte":
            case "System.SByte":
                visualBasicType = "SByte";
                return true;
            case "char":
            case "System.Char":
                visualBasicType = "Char";
                return true;
            case "decimal":
            case "System.Decimal":
                visualBasicType = "Decimal";
                return true;
            case "double":
            case "System.Double":
                visualBasicType = "Double";
                return true;
            case "float":
            case "System.Single":
                visualBasicType = "Single";
                return true;
            case "int":
            case "System.Int32":
                visualBasicType = "Integer";
                return true;
            case "long":
            case "System.Int64":
                visualBasicType = "Long";
                return true;
            case "short":
            case "System.Int16":
                visualBasicType = "Short";
                return true;
            case "uint":
            case "System.UInt32":
                visualBasicType = "UInteger";
                return true;
            case "ulong":
            case "System.UInt64":
                visualBasicType = "ULong";
                return true;
            case "ushort":
            case "System.UInt16":
                visualBasicType = "UShort";
                return true;
            default:
                visualBasicType = "";
                return false;
        }
    }

    /// <summary>
    /// 多行值收成 vbLf 拼接，与 C# 11+ raw string 一样把 CR 折成 LF。
    /// 引号写成 ""。无法放进 Const 的控制字符用 ChrW，并由调用方改成 Shared ReadOnly。
    /// </summary>
    public static string FormatString(string? value, out bool needsRuntime)
    {
        needsRuntime = false;
        var content = (value ?? "").Replace("\r\n", "\n").Replace("\r", "\n");
        var parts = new List<string>();
        var text = new StringBuilder();
        foreach (var ch in content)
        {
            string? token = ch switch
            {
                '\n' => "vbLf",
                '\t' => "vbTab",
                '\0' => "vbNullChar",
                '\b' => "vbBack",
                '\f' => "vbFormFeed",
                '\v' => "vbVerticalTab",
                _ => null,
            };
            if (token is null && char.IsControl(ch))
            {
                token = "ChrW(" + ((int)ch).ToString(CultureInfo.InvariantCulture) + ")";
                needsRuntime = true;
            }

            if (token is null)
            {
                text.Append(ch);
                continue;
            }

            Flush(text, parts);
            parts.Add(token);
        }

        Flush(text, parts);
        return parts.Count == 0 ? "\"\"" : string.Join(" & ", parts);
    }

    public static bool TryFormatValue(string typeName, string? rawValue, out string expression)
    {
        expression = "";
        var type = typeName.Trim();
        var value = (rawValue ?? "").Trim();
        if (IdentifierSanitizer.IsStringType(type))
        {
            expression = FormatString(rawValue, out _);
            return true;
        }

        if (type == "bool" || type == "System.Boolean")
        {
            if (value.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                expression = "True";
                return true;
            }

            if (value.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                expression = "False";
                return true;
            }

            return false;
        }

        if (type == "char" || type == "System.Char")
        {
            return TryFormatChar(value, out expression);
        }

        return TryFormatNumber(type, value, out expression);
    }

    private static void Flush(StringBuilder text, List<string> parts)
    {
        if (text.Length == 0)
        {
            return;
        }

        parts.Add("\"" + text.ToString().Replace("\"", "\"\"") + "\"");
        text.Clear();
    }

    private static bool TryFormatChar(string value, out string expression)
    {
        expression = "";
        if (value.Length < 3 || value[0] != '\'' || value[value.Length - 1] != '\'')
        {
            return false;
        }

        var body = value.Substring(1, value.Length - 2);
        if (!TryDecodeCharBody(body, out var ch))
        {
            return false;
        }

        expression = ch switch
        {
            '\n' => "Microsoft.VisualBasic.ControlChars.Lf",
            '\r' => "Microsoft.VisualBasic.ControlChars.Cr",
            '\t' => "Microsoft.VisualBasic.ControlChars.Tab",
            '\0' => "Microsoft.VisualBasic.ControlChars.NullChar",
            '\b' => "Microsoft.VisualBasic.ControlChars.Back",
            '\f' => "Microsoft.VisualBasic.ControlChars.FormFeed",
            '\v' => "Microsoft.VisualBasic.ControlChars.VerticalTab",
            '"' => "\"\"\"\"c",
            _ => "\"" + ch + "\"c",
        };
        return true;
    }

    private static bool TryDecodeCharBody(string body, out char ch)
    {
        ch = '\0';
        if (body.Length == 1)
        {
            ch = body[0];
            return body[0] != '\\';
        }

        if (body.Length == 2 && body[0] == '\\')
        {
            switch (body[1])
            {
                case '\\': ch = '\\'; return true;
                case '\'': ch = '\''; return true;
                case '"': ch = '"'; return true;
                case 'n': ch = '\n'; return true;
                case 'r': ch = '\r'; return true;
                case 't': ch = '\t'; return true;
                case '0': ch = '\0'; return true;
                case 'a': ch = '\a'; return true;
                case 'b': ch = '\b'; return true;
                case 'f': ch = '\f'; return true;
                case 'v': ch = '\v'; return true;
                default: return false;
            }
        }

        if (body.Length == 6 && body.StartsWith("\\u", StringComparison.Ordinal)
            && ushort.TryParse(body.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code))
        {
            ch = (char)code;
            return true;
        }

        return false;
    }

    private static bool TryFormatNumber(string type, string value, out string expression)
    {
        expression = "";
        if (!TrySplitNumber(value, out var body, out var suffix))
        {
            return false;
        }

        if (!SuffixMatches(type, suffix))
        {
            return false;
        }

        var vbBody = body.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? "&H" + body.Substring(2)
            : body;
        expression = vbBody + VisualBasicSuffix(type);
        return true;
    }

    private static bool TrySplitNumber(string value, out string body, out string suffix)
    {
        body = "";
        suffix = "";
        if (value.Length == 0)
        {
            return false;
        }

        var suffixes = new[] { "ul", "lu", "u", "l", "f", "d", "m" };
        var end = value.Length;
        foreach (var candidate in suffixes)
        {
            if (value.Length > candidate.Length
                && value.EndsWith(candidate, StringComparison.OrdinalIgnoreCase)
                && IsNumberBody(value.Substring(0, value.Length - candidate.Length)))
            {
                suffix = candidate.ToLowerInvariant();
                end = value.Length - candidate.Length;
                break;
            }
        }

        body = value.Substring(0, end);
        return IsNumberBody(body);
    }

    private static bool IsNumberBody(string body)
    {
        if (body.Length == 0)
        {
            return false;
        }

        var index = 0;
        if (body[0] == '+' || body[0] == '-')
        {
            index++;
        }

        if (index >= body.Length)
        {
            return false;
        }

        if (body.Length - index > 2 && body[index] == '0' && (body[index + 1] == 'x' || body[index + 1] == 'X'))
        {
            return HasDigits(body, index + 2, hex: true);
        }

        var sawDigit = false;
        var sawDot = false;
        var sawExp = false;
        for (var i = index; i < body.Length; i++)
        {
            var ch = body[i];
            if (ch == '_')
            {
                continue;
            }

            if (ch == '.' && !sawDot && !sawExp)
            {
                sawDot = true;
                continue;
            }

            if ((ch == 'e' || ch == 'E') && sawDigit && !sawExp)
            {
                sawExp = true;
                if (i + 1 < body.Length && (body[i + 1] == '+' || body[i + 1] == '-'))
                {
                    i++;
                }

                continue;
            }

            if (ch >= '0' && ch <= '9')
            {
                sawDigit = true;
                continue;
            }

            return false;
        }

        return sawDigit;
    }

    private static bool HasDigits(string body, int start, bool hex)
    {
        var saw = false;
        for (var i = start; i < body.Length; i++)
        {
            var ch = body[i];
            if (ch == '_')
            {
                continue;
            }

            var ok = (ch >= '0' && ch <= '9')
                || (hex && ((ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F')));
            if (!ok)
            {
                return false;
            }

            saw = true;
        }

        return saw;
    }

    private static bool SuffixMatches(string type, string suffix)
    {
        if (suffix.Length == 0)
        {
            return true;
        }

        return suffix switch
        {
            "u" => type is "uint" or "System.UInt32",
            "l" => type is "long" or "System.Int64",
            "ul" or "lu" => type is "ulong" or "System.UInt64",
            "f" => type is "float" or "System.Single",
            "d" => type is "double" or "System.Double",
            "m" => type is "decimal" or "System.Decimal",
            _ => false,
        };
    }

    private static string VisualBasicSuffix(string type)
        => type switch
        {
            "short" or "System.Int16" => "S",
            "ushort" or "System.UInt16" => "US",
            "uint" or "System.UInt32" => "UI",
            "long" or "System.Int64" => "L",
            "ulong" or "System.UInt64" => "UL",
            "float" or "System.Single" => "F",
            "double" or "System.Double" => "R",
            "decimal" or "System.Decimal" => "D",
            _ => "",
        };
}