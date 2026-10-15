using System;
using System.Text;

namespace AssemblyMeta.Core.Emit;

/// <summary>C# 字面量格式化。多行字符串在 C# 11+ 使用 raw string，否则退回转义字符串。</summary>
internal static class CSharpLiteral
{
    public const int RawStringVersion = 1100;

    public static string String(string? value, int csharpLanguageVersion)
    {
        value ??= "";
        if (csharpLanguageVersion >= RawStringVersion && (value.IndexOf('\n') >= 0 || value.IndexOf('\r') >= 0))
        {
            return RawString(value);
        }

        return EscapedString(value);
    }

    public static string EscapedString(string value)
    {
        var builder = new StringBuilder(value.Length + 2);
        builder.Append('"');
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                case '\0':
                    builder.Append("\\0");
                    break;
                default:
                    if (char.IsControl(ch))
                    {
                        builder.Append("\\u");
                        builder.Append(((int)ch).ToString("x4"));
                    }
                    else
                    {
                        builder.Append(ch);
                    }

                    break;
            }
        }

        builder.Append('"');
        return builder.ToString();
    }

    /// <summary>
    /// 闭合引号放在第 0 列，避免编译器裁剪内容缩进。
    /// 内容末尾的换行需要多写一行，因为闭合分隔符前的换行不属于字符串。
    /// </summary>
    public static string RawString(string value)
    {
        var content = value.Replace("\r\n", "\n").Replace("\r", "\n");
        var delimiterLength = Math.Max(3, LongestQuoteRun(content) + 1);
        var delimiter = new string('"', delimiterLength);
        var builder = new StringBuilder(content.Length + (delimiterLength * 2) + 4);
        builder.Append(delimiter);
        builder.Append('\n');
        builder.Append(content);
        builder.Append('\n');
        builder.Append(delimiter);
        return builder.ToString();
    }

    private static int LongestQuoteRun(string value)
    {
        var run = 0;
        var max = 0;
        foreach (var ch in value)
        {
            if (ch == '"')
            {
                run++;
                if (run > max)
                {
                    max = run;
                }
            }
            else
            {
                run = 0;
            }
        }

        return max;
    }
}
