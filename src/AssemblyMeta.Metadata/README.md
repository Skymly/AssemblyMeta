# AssemblyMeta.Metadata

把 `@(AssemblyMetadata)` 生成为 `AssemblyMeta.Metadata` 常量。

```xml
<ItemGroup>
  <AssemblyMetadata Include="DemoKey" Value="Bar" />
</ItemGroup>
```

```csharp
var value = AssemblyMeta.Metadata.DemoKey;
```

这些项仍由 SDK 写成 `AssemblyMetadataAttribute`。本包只读，不添加、不删除、不改写。源码里手写的 `[assembly: AssemblyMetadata]` 不会出现在这里。
