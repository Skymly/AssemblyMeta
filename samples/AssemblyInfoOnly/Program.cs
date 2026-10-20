using AssemblyInfoOnly;

var failures = new List<string>();

void Check(string name, string actual, string expected)
{
    if (!string.Equals(actual, expected, StringComparison.Ordinal))
    {
        failures.Add($"{name}: 期望 '{expected}'，实际 '{actual}'");
    }
}

Check("Namespace", typeof(AssemblyMeta).Namespace ?? "", "AssemblyInfoOnly");
Check(nameof(AssemblyMeta.Info.Title), AssemblyMeta.Info.Title, "AssemblyInfoOnly");
Check(nameof(AssemblyMeta.Info.Product), AssemblyMeta.Info.Product, "AssemblyInfoOnly");
Check(nameof(AssemblyMeta.Info.Company), AssemblyMeta.Info.Company, "AssemblyInfoOnly");
Check(nameof(AssemblyMeta.Info.Configuration), AssemblyMeta.Info.Configuration, "Release");
Check(nameof(AssemblyMeta.Info.Version), AssemblyMeta.Info.Version, "1.0.0.0");
Check(nameof(AssemblyMeta.Info.FileVersion), AssemblyMeta.Info.FileVersion, "1.0.0.0");
Check(nameof(AssemblyMeta.Info.InformationalVersion), AssemblyMeta.Info.InformationalVersion, "1.0.0");

if (failures.Count > 0)
{
    foreach (var failure in failures)
    {
        Console.Error.WriteLine(failure);
    }

    return 1;
}

Console.WriteLine("AssemblyInfo 直接引用断言通过。");
return 0;
