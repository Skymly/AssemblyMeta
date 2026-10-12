# AssemblyMeta.Constants

把 MSBuild 项生成为 `AssemblyMeta.Constants` 强类型常量。

```xml
<ItemGroup>
  <Constant Include="Http.TimeoutSeconds" Value="30" Type="int" Comment="请求超时（秒）" />
  <Constant Include="Banner" Value="line1&#10;line2" />
  <FileConstant Include="@(Content)" />
</ItemGroup>
```

- `Type` 缺省为 `string`。字符串里的换行在 C# 11+ 使用 raw string literal。
- 非 `string` 类型把 `Value` 当作 C# 表达式原样输出。
- `FileConstant` 的值是相对项目的正斜杠路径；有 `Link` 时用链接路径。项目外且没有 `Link` 的文件会被跳过。
