# AssemblyMeta.AssemblyInfo

Projects the 9 SDK-generated assembly attributes as `AssemblyMeta.Info` constants at compile time.

```csharp
var version = AssemblyMeta.Info.Version;
```

Covers `Configuration`, `Company`, `Title`, `Description`, `Product`, `Copyright`, `Version`, `InformationalVersion` and `FileVersion`.

All nine members are always emitted. An empty string stays an empty string — the generator never substitutes the assembly name. Under Microsoft.NET.Sdk the SDK itself fills empty `Authors` with the assembly name, empty `Company` with `Authors`, and empty `AssemblyTitle`/`Product` with the assembly name. Unset `Description` and `Copyright` remain empty strings. Trademarks and other assembly attributes produce no members.

`Version` maps `AssemblyVersion`, not the NuGet `Version`. The nine constants are emitted even with `GenerateAssemblyInfo=false`.

---

把 SDK 默认生成的 9 个程序集特性，在编译期投影为 `AssemblyMeta.Info` 常量。

覆盖：`Configuration`、`Company`、`Title`、`Description`、`Product`、`Copyright`、`Version`、`InformationalVersion`、`FileVersion`。

九个成员总会生成。生成器读到空字符串就写空字符串，不把空值改成程序集名。使用 Microsoft.NET.Sdk 时，空的 `Authors` 会先被 SDK 设成程序集名，空的 `Company` 设成 `Authors`，空的 `AssemblyTitle` 和 `Product` 设成程序集名。`Description` 和 `Copyright` 未设置时仍是空字符串。商标以及其他程序集特性不生成成员。

`Version` 取 `AssemblyVersion`，不是 NuGet `Version`。即使 `GenerateAssemblyInfo=false`，这九个常量仍会生成。
