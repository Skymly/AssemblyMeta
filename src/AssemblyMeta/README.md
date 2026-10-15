# AssemblyMeta

元包。引用它等同于同时引用 `AssemblyMeta.AssemblyInfo`、`AssemblyMeta.Constants`、`AssemblyMeta.Project`、`AssemblyMeta.Metadata`、`AssemblyMeta.Git`、`AssemblyMeta.Resources` 与 `AssemblyMeta.Strings`。

```xml
<PackageReference Include="AssemblyMeta" Version="1.0.0" />
```

生成入口是全局命名空间中的 `static partial class AssemblyMeta`。可用 `$(AssemblyMetaNamespace)` 覆盖命名空间，用 `$(AssemblyMetaVisibility)` 设为 `public`。默认 `internal`。合格类型是 `const`，其它类型是 `static readonly`。`public` 时成员改为 `static readonly`，避免调用方把值烤进程序集。
