# AssemblyMeta.Strings

把中性 `.resx` 生成为 `AssemblyMeta.Strings` 编译期常量。默认 `internal` 时可以写进特性参数；`AssemblyMetaVisibility=public` 时成员是 `static readonly`。

```xml
<ItemGroup>
  <EmbeddedResource Include="Strings.resx" />
</ItemGroup>
```

```csharp
var hello = AssemblyMeta.Strings.Hello;
var stock = AssemblyMeta.Strings.Shopping.OutOfStock;
```

SDK 默认会把 `**/*.resx` 收进 `EmbeddedResource`。`Strings.fr.resx` 这类卫星文件不参与生成。键名里的 `_` 和 `.` 都会变成嵌套类型。路径末尾连续的 `Strings` 段都会去掉。根上的 `Strings.resx` 和 `Strings/Strings.resx` 的键直接挂在 `AssemblyMeta.Strings` 下。`UI/Strings.resx` 的 `Hello` 是 `AssemblyMeta.Strings.UI.Hello`。与外层类型同名的片段在末尾加下划线并报告 `AM0008`，键 `Strings` 是 `Strings_`。

带 `{0}` 的值仍然是字符串常量。非字符串项会跳过并报告 `AM0007`。无效键报告 `AM0010`。`AssemblyMetaStrings=false` 可排除某个文件。
