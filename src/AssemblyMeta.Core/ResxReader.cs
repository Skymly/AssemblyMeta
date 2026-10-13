using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace AssemblyMeta.Core;

/// <summary>读取 resx 里的字符串项。带 type 或 mimetype 的非字符串项记入 skipped。</summary>
public static class ResxReader
{
    public readonly record struct Entry(string Name, string Value, string Comment);

    public static bool TryRead(string? xml, out IReadOnlyList<Entry> entries, out IReadOnlyList<string> skipped, out string? error)
    {
        entries = Array.Empty<Entry>();
        skipped = Array.Empty<string>();
        error = null;
        if (string.IsNullOrWhiteSpace(xml))
        {
            error = "内容为空";
            return false;
        }

        XDocument document;
        try
        {
            document = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
        }
        catch (System.Xml.XmlException ex)
        {
            error = ex.Message;
            return false;
        }

        if (document.Root is null)
        {
            error = "缺少根元素";
            return false;
        }

        var found = new List<Entry>();
        var ignored = new List<string>();
        foreach (var data in document.Descendants())
        {
            if (!string.Equals(data.Name.LocalName, "data", StringComparison.Ordinal))
            {
                continue;
            }

            var name = (string?)data.Attribute("name") ?? "";
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (!IsStringData(data))
            {
                ignored.Add(name.Trim());
                continue;
            }

            var value = ChildValue(data, "value");
            var comment = ChildValue(data, "comment").Trim();
            found.Add(new Entry(name.Trim(), value, comment));
        }

        entries = found;
        skipped = ignored;
        return true;
    }

    private static bool IsStringData(XElement data)
    {
        var mime = (string?)data.Attribute("mimetype");
        if (!string.IsNullOrWhiteSpace(mime))
        {
            return false;
        }

        var type = (string?)data.Attribute("type");
        if (string.IsNullOrWhiteSpace(type))
        {
            return true;
        }

        var typeName = type!.Split(',')[0].Trim();
        return typeName.Equals("string", StringComparison.Ordinal)
            || typeName.Equals("System.String", StringComparison.Ordinal)
            || typeName.Equals("global::System.String", StringComparison.Ordinal);
    }

    private static string ChildValue(XElement data, string localName)
    {
        foreach (var child in data.Elements())
        {
            if (string.Equals(child.Name.LocalName, localName, StringComparison.Ordinal))
            {
                return child.Value ?? "";
            }
        }

        return "";
    }
}
