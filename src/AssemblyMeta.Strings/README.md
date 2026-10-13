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

SDK 默认会把 `**/*.resx` 收进 `EmbeddedResource`。`Strings.fr.resx` 这类卫星文件不参与生成。键名里的 `_` 和 `.` 都会变成嵌套类型。根上的 `Strings.resx` 键直接挂在 `AssemblyMeta.Strings` 下。路径最后一段是 `Strings` 时去掉这一段，但保留父目录，例如 `UI/Strings.resx` 的 `Hello` 是 `AssemblyMeta.Strings.UI.Hello`。

带 `{0}` 的值仍然是字符串常量。非字符串项会跳过并报告 `AM0007`。无效键报告 `AM0010`。`AssemblyMetaStrings=false` 可排除某个文件。
