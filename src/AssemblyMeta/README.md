# AssemblyMeta

元包。引用它等同于同时引用 `AssemblyMeta.AssemblyInfo`、`AssemblyMeta.Constants`、`AssemblyMeta.Project`、`AssemblyMeta.Metadata`、`AssemblyMeta.Git`、`AssemblyMeta.Resources` 与 `AssemblyMeta.Strings`。

```xml
<PackageReference Include="AssemblyMeta" Version="1.0.0" />
```

生成入口是 `static partial class AssemblyMeta`。未设置 `$(AssemblyMetaNamespace)` 时用项目的 `RootNamespace`；显式设成空白时在全局命名空间。`$(AssemblyMetaVisibility)` 可设为 `public`。默认 `internal`。合格类型是 `const`，其它类型是 `static readonly`。`public` 时成员改为 `static readonly`，避免调用方把值烤进程序集。
