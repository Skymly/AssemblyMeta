# AssemblyMeta.Strings

Generates neutral `.resx` files as `AssemblyMeta.Strings` compile-time constants. Under the default `internal` they can be used in attribute arguments; with `AssemblyMetaVisibility=public` members become `static readonly`.

```xml
<ItemGroup>
  <EmbeddedResource Include="Strings.resx" />
</ItemGroup>
```

```csharp
var hello = AssemblyMeta.Strings.Hello;
var stock = AssemblyMeta.Strings.Shopping.OutOfStock;
```

The SDK collects `**/*.resx` into `EmbeddedResource` by default. Satellite files like `Strings.fr.resx` do not participate. `_` and `.` in keys both become nested types. Trailing `Strings` path segments are stripped. Keys from `Strings.resx` at the root and `Strings/Strings.resx` hang directly under `AssemblyMeta.Strings`; `Hello` in `UI/Strings.resx` becomes `AssemblyMeta.Strings.UI.Hello`. Segments matching the enclosing type get a trailing underscore and report `AM0008` — key `Strings` becomes `Strings_`.

Values containing `{0}` remain plain string constants. Non-string entries are skipped with `AM0007`. Invalid keys report `AM0010`. `AssemblyMetaStrings=false` excludes a file.

---

把中性 `.resx` 生成为 `AssemblyMeta.Strings` 编译期常量。默认 `internal` 时可以写进特性参数；`AssemblyMetaVisibility=public` 时成员是 `static readonly`。

SDK 默认会把 `**/*.resx` 收进 `EmbeddedResource`。`Strings.fr.resx` 这类卫星文件不参与生成。键名里的 `_` 和 `.` 都会变成嵌套类型。路径末尾连续的 `Strings` 段都会去掉。根上的 `Strings.resx` 和 `Strings/Strings.resx` 的键直接挂在 `AssemblyMeta.Strings` 下。`UI/Strings.resx` 的 `Hello` 是 `AssemblyMeta.Strings.UI.Hello`。与外层类型同名的片段在末尾加下划线并报告 `AM0008`，键 `Strings` 是 `Strings_`。

带 `{0}` 的值仍然是字符串常量。非字符串项会跳过并报告 `AM0007`。无效键报告 `AM0010`。`AssemblyMetaStrings=false` 可排除某个文件。
