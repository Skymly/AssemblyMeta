using System.Reflection;
using SampleApp;

[assembly: AssemblyMetadata("Banner", AssemblyMeta.Constants.Banner)]
[assembly: AssemblyMetadata("StringHello", AssemblyMeta.Strings.Hello)]

var failures = new List<string>();

void Check(string name, string actual, string expected)
{
    if (!string.Equals(actual, expected, StringComparison.Ordinal))
    {
        failures.Add($"{name}: 期望 '{expected}'，实际 '{actual}'");
    }
}

Check("Namespace", typeof(AssemblyMeta).Namespace ?? "", "SampleApp");
Check(nameof(AssemblyMeta.Info.Configuration), AssemblyMeta.Info.Configuration, AssemblyMeta.Constants.ExpectedConfiguration);
Check(nameof(AssemblyMeta.Info.Company), AssemblyMeta.Info.Company, "Skymly");
Check(nameof(AssemblyMeta.Info.Title), AssemblyMeta.Info.Title, "AssemblyMeta 示例标题");
Check(nameof(AssemblyMeta.Info.Product), AssemblyMeta.Info.Product, "AssemblyMeta 示例");
Check(nameof(AssemblyMeta.Info.Copyright), AssemblyMeta.Info.Copyright, "Copyright © 2026 Skymly");
Check(nameof(AssemblyMeta.Info.Version), AssemblyMeta.Info.Version, "1.2.3.0");
Check(nameof(AssemblyMeta.Info.FileVersion), AssemblyMeta.Info.FileVersion, "1.2.3.4");
Check(nameof(AssemblyMeta.Info.InformationalVersion), AssemblyMeta.Info.InformationalVersion, "1.2.3-beta.4+local");
Check(nameof(AssemblyMeta.Info.Description), AssemblyMeta.Info.Description, "第一行\n第二行 \"引号\" ; 分号 # 井号");
Check(nameof(AssemblyMeta.Constants.Banner), AssemblyMeta.Constants.Banner, "line1\nline2");
Check(nameof(AssemblyMeta.Constants.Weird), AssemblyMeta.Constants.Weird, "a;b#c \"q\"");
Check("Http.TimeoutSeconds", AssemblyMeta.Constants.Http.TimeoutSeconds.ToString(), "30");
Check(nameof(AssemblyMeta.Constants.Content.Docs.License), AssemblyMeta.Constants.Content.Docs.License, "Content/Docs/License.md");
Check("readme", AssemblyMeta.Constants.Content.Docs._12._Readme_copy_, "Content/Docs/12. Readme (copy).txt");
Check(nameof(AssemblyMeta.Constants.Included.Readme), AssemblyMeta.Constants.Included.Readme, "Included/Readme.txt");
Check(nameof(AssemblyMeta.Project.BannerTitle), AssemblyMeta.Project.BannerTitle, "AssemblyMeta 横幅");
Check(nameof(AssemblyMeta.Project.Configuration), AssemblyMeta.Project.Configuration, AssemblyMeta.Info.Configuration);
Check(nameof(AssemblyMeta.Project.Company), AssemblyMeta.Project.Company, "Skymly");
Check(nameof(AssemblyMeta.Project.WeirdProject), AssemblyMeta.Project.WeirdProject, "a;b#c\n第二行");
Check(nameof(AssemblyMeta.Project.AssemblyName), AssemblyMeta.Project.AssemblyName, "AssemblyMeta.Sample");
Check(nameof(AssemblyMeta.Project.TargetFrameworkIdentifier), AssemblyMeta.Project.TargetFrameworkIdentifier, ".NETCoreApp");
Check(nameof(AssemblyMeta.Metadata.DemoKey), AssemblyMeta.Metadata.DemoKey, "a;b#c");
Check(nameof(AssemblyMeta.Metadata.Area.Name), AssemblyMeta.Metadata.Area.Name, "nested");
Check("Git.Sha", AssemblyMeta.Git.Commit.StartsWith(AssemblyMeta.Git.Sha, StringComparison.Ordinal) ? "ok" : AssemblyMeta.Git.Sha, "ok");
if (AssemblyMeta.Git.Commit.Length == 0 || AssemblyMeta.Git.Commit == "[pending build]")
{
    failures.Add("Git.Commit 未在真实构建中填充");
}
if (AssemblyMeta.Git.Root.Contains('\\'))
{
    failures.Add("Git.Root 仍包含反斜杠: " + AssemblyMeta.Git.Root);
}
var demo = System.Reflection.CustomAttributeExtensions.GetCustomAttributes<System.Reflection.AssemblyMetadataAttribute>(typeof(Program).Assembly)
    .FirstOrDefault(attribute => attribute.Key == "DemoKey");
Check("AssemblyMetadataAttribute", demo?.Value ?? "", "a;b#c");
var stringHello = System.Reflection.CustomAttributeExtensions.GetCustomAttributes<System.Reflection.AssemblyMetadataAttribute>(typeof(Program).Assembly)
    .FirstOrDefault(attribute => attribute.Key == "StringHello");
Check("StringHello", stringHello?.Value ?? "", "你好");
const string license = "示例许可证文本。\r\n";
var licenseText = AssemblyMeta.Resources.Content.Docs.License.Text;
Check("Resources.License", licenseText, license);
if (!object.ReferenceEquals(licenseText, AssemblyMeta.Resources.Content.Docs.License.Text))
{
    failures.Add("Resources.License.Text 没有缓存");
}
var licenseBytes = AssemblyMeta.Resources.Content.Docs.License.GetBytes();
Check("Resources.License.GetBytes", System.Text.Encoding.UTF8.GetString(licenseBytes), license);
using (var stream = AssemblyMeta.Resources.Content.Docs.License.GetStream())
{
    using var copy = new System.IO.MemoryStream();
    stream.CopyTo(copy);
    Check("Resources.License.GetStream", System.Text.Encoding.UTF8.GetString(copy.ToArray()), license);
}
Check("Resources.Readme", AssemblyMeta.Resources.Content.Docs._12._Readme_copy_.Text, "这是一份带空格和括号的说明。\r\n");
Check("Resources.Linked", AssemblyMeta.Resources.Included.Readme.Text, "链接进来的说明文件。\r\n");
var blob = AssemblyMeta.Resources.Content.Blob.GetBytes();
if (blob.Length != 4 || blob[0] != 1 || blob[1] != 2 || blob[2] != 3 || blob[3] != 255)
{
    failures.Add("Resources.Blob 字节不符: " + string.Join(",", blob));
}
Check(nameof(AssemblyMeta.Strings.Hello), AssemblyMeta.Strings.Hello, "你好");
Check(nameof(AssemblyMeta.Strings.User.InvalidCredentials), AssemblyMeta.Strings.User.InvalidCredentials, "凭证无效");
Check(nameof(AssemblyMeta.Strings.Greeting), AssemblyMeta.Strings.Greeting, "你好，{0}");
Check(nameof(AssemblyMeta.Strings.Errors.NotFound), AssemblyMeta.Strings.Errors.NotFound, "找不到 {0}");
var resourceType = typeof(AssemblyMeta).GetNestedType("Resources");
foreach (var unexpected in new[] { "Strings", "Strings_", "Errors" })
{
    if (resourceType?.GetNestedType(unexpected) is not null)
    {
        failures.Add("resx 进入了 Resources." + unexpected);
    }
}

if (failures.Count > 0)
{
    foreach (var failure in failures)
    {
        Console.Error.WriteLine(failure);
    }

    return 1;
}

Console.WriteLine("AssemblyMeta 样例断言通过。");
return 0;
