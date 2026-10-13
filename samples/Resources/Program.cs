const string marker = "仅 Resources 包。\n";
var text = AssemblyMeta.Resources.Content.Note.Text;
if (!string.Equals(text, marker, StringComparison.Ordinal))
{
    Console.Error.WriteLine("Text: " + text);
    return 1;
}

var bytes = AssemblyMeta.Resources.Content.Note.GetBytes();
if (!string.Equals(System.Text.Encoding.UTF8.GetString(bytes), text, StringComparison.Ordinal))
{
    Console.Error.WriteLine("GetBytes 不符");
    return 1;
}

using (var stream = AssemblyMeta.Resources.Content.Note.GetStream())
{
    using var copy = new System.IO.MemoryStream();
    stream.CopyTo(copy);
    if (!string.Equals(System.Text.Encoding.UTF8.GetString(copy.ToArray()), text, StringComparison.Ordinal))
    {
        Console.Error.WriteLine("GetStream 不符");
        return 1;
    }
}

Console.WriteLine("Resources 样例断言通过。");
return 0;
