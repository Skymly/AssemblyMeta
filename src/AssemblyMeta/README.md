# AssemblyMeta

元包。引用它等同于同时引用 `AssemblyMeta.AssemblyInfo`、`AssemblyMeta.Constants`、`AssemblyMeta.Project`、`AssemblyMeta.Metadata` 与 `AssemblyMeta.Git`。

```xml
<PackageReference Include="AssemblyMeta" Version="0.1.0" />
```

生成入口是全局命名空间中的 `static partial class AssemblyMeta`。可用 `$(AssemblyMetaNamespace)` 覆盖命名空间，用 `$(AssemblyMetaVisibility)` 设为 `public`。默认 `internal`，成员为 `const`；`public` 时成员改为 `static readonly`，避免调用方把值烤进程序集。
