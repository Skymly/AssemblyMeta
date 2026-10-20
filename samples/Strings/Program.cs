using System.Reflection;
using Strings;

[assembly: AssemblyMetadata("Hello", AssemblyMeta.Strings.Hello)]

var failures = new List<string>();

void Check(string name, string actual, string expected)
{
    if (!string.Equals(actual, expected, StringComparison.Ordinal))
    {
        failures.Add($"{name}: 期望 '{expected}'，实际 '{actual}'");
    }
}

Check("Namespace", typeof(AssemblyMeta).Namespace ?? "", "Strings");
Check(nameof(AssemblyMeta.Strings.Hello), AssemblyMeta.Strings.Hello, "仅 Strings 包");
Check(nameof(AssemblyMeta.Strings.Area.Name), AssemblyMeta.Strings.Area.Name, "区域");
var hello = typeof(Program).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(attribute => attribute.Key == "Hello");
Check("AssemblyMetadata", hello?.Value ?? "", "仅 Strings 包");

if (failures.Count > 0)
{
    foreach (var failure in failures)
    {
        Console.Error.WriteLine(failure);
    }

    return 1;
}

Console.WriteLine("Strings 样例断言通过。");
return 0;
