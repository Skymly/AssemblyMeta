# AssemblyMeta.AssemblyInfo

把 SDK 默认生成的 9 个程序集特性，在编译期投影为 `AssemblyMeta.Info` 常量。

```csharp
var version = AssemblyMeta.Info.Version;
```

覆盖：`Configuration`、`Company`、`Title`、`Description`、`Product`、`Copyright`、`Version`、`InformationalVersion`、`FileVersion`。

九个成员总会生成。生成器读到空字符串就写空字符串，不把空值改成程序集名。使用 Microsoft.NET.Sdk 时，空的 `Company`、`AssemblyTitle`、`Product` 会先被 SDK 设成程序集名。`Description` 和 `Copyright` 未设置时仍是空字符串。商标以及其他程序集特性不生成成员。

`Version` 取 `AssemblyVersion`，不是 NuGet `Version`。即使 `GenerateAssemblyInfo=false`，这九个常量仍会生成。
