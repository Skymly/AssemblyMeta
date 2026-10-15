#:package AssemblyMeta.Project@1.0.0
#:property BannerTitle=from-file
#:property ProjectProperty=BannerTitle
#:property EnableSourceControlManagerQueries=false

var actual = AssemblyMeta.Project.BannerTitle;
Console.WriteLine(actual);
return string.Equals(actual, "from-file", StringComparison.Ordinal) ? 0 : 1;
