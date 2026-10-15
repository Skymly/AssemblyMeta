# AssemblyMeta.Resources

把 `EmbeddedResource` 生成为 `AssemblyMeta.Resources`。

```xml
<ItemGroup>
  <EmbeddedResource Include="Content/Docs/License.md" />
</ItemGroup>
```

```csharp
var text = AssemblyMeta.Resources.Content.Docs.License.Text;
var bytes = AssemblyMeta.Resources.Content.Docs.License.GetBytes();
using var stream = AssemblyMeta.Resources.Content.Docs.License.GetStream();
```

路径规则与 `FileConstant` 相同。`Text` 按 UTF-8 读取并缓存。`GetBytes` 和 `GetStream` 每次重新打开清单资源，调用方负责释放流。

`.resx` 默认不进入这里。`AssemblyMetaResource=false` 可排除其它项；`AssemblyMetaResource=true` 可强制纳入 `.resx` 编译后的清单项。

与外层类型同名的片段会在末尾加下划线并报告 `AM0008`。例如 `Resources/readme.txt` 是 `AssemblyMeta.Resources.Resources_.readme`，`Docs/Docs/file.txt` 是 `AssemblyMeta.Resources.Docs.Docs_.file`。叶子名 `Text`、`GetBytes`、`GetStream`，以及根上的 `Manifest`，也会改写。
