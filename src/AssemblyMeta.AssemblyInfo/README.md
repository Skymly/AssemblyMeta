# AssemblyMeta.AssemblyInfo

把 SDK 默认生成的 9 个程序集特性，在编译期投影为 `AssemblyMeta.Info` 常量。

```csharp
var version = AssemblyMeta.Info.Version;
```

覆盖：`Configuration`、`Company`、`Title`、`Description`、`Product`、`Copyright`、`Version`、`InformationalVersion`、`FileVersion`。

`Version` 取 `AssemblyVersion`，不是 NuGet `Version`。即使 `GenerateAssemblyInfo=false`，只要对应 MSBuild 属性有值，常量仍会生成。
