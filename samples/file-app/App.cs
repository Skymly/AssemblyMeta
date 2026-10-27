#:package AssemblyMeta.Project@1.1.0
#:property BannerTitle=from-file
#:property ProjectProperty=BannerTitle
#:property EnableSourceControlManagerQueries=false
#:property RootNamespace=App

using App;

var actual = AssemblyMeta.Project.BannerTitle;
var ns = typeof(AssemblyMeta).Namespace;
Console.WriteLine(actual);
if (ns != "App" || !string.Equals(actual, "from-file", StringComparison.Ordinal))
{
    Console.Error.WriteLine($"namespace='{ns}' value='{actual}'");
    return 1;
}

return 0;
