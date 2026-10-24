# AssemblyMeta.Metadata

Generates `@(AssemblyMetadata)` as `AssemblyMeta.Metadata` constants.

```xml
<ItemGroup>
  <AssemblyMetadata Include="DemoKey" Value="Bar" />
</ItemGroup>
```

```csharp
var value = AssemblyMeta.Metadata.DemoKey;
```

Dots become nested types. The items are still emitted by the SDK as `AssemblyMetadataAttribute`. This package only reads — it does not add, remove or rewrite items. Hand-written `[assembly: AssemblyMetadata]` attributes in source do not appear here.

---

把 `@(AssemblyMetadata)` 生成为 `AssemblyMeta.Metadata` 常量。

点号变成嵌套类型。这些项仍由 SDK 写成 `AssemblyMetadataAttribute`。本包只读，不添加、不删除、不改写。源码里手写的 `[assembly: AssemblyMetadata]` 不会出现在这里。
