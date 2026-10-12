# AssemblyMeta.Project

把 opt-in 的 MSBuild 属性生成为 `AssemblyMeta.Project` 常量。

```xml
<PropertyGroup>
  <Foo>Bar</Foo>
  <ProjectProperty>Foo;Configuration</ProjectProperty>
</PropertyGroup>
<ItemGroup>
  <ProjectProperty Include="Company" Comment="公司名称" />
</ItemGroup>
```

```csharp
var company = AssemblyMeta.Project.Company;
var foo = AssemblyMeta.Project.Foo;
```

`$(ProjectProperty)` 按分号拆分，file-based apps 可以写 `#:property ProjectProperty=Foo`。

默认还会带上 `AssemblyName`、`RootNamespace`、`TargetFrameworkIdentifier`、`TargetFrameworkMoniker`、`TargetFrameworkVersion`、`RuntimeIdentifier`、`RuntimeIdentifiers`。不需要的用 `<ProjectProperty Remove="..." />` 去掉。

点号会变成嵌套类型。空注释会生成“项目属性 {名称}。”
