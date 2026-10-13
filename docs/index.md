# AssemblyMeta

把项目和程序集元信息在编译期生成为强类型常量。入口是全局命名空间里的 `static partial class AssemblyMeta`。

| 包 | 生成结果 |
|---|---|
| `AssemblyMeta.AssemblyInfo` | `AssemblyMeta.Info` |
| `AssemblyMeta.Constants` | `AssemblyMeta.Constants` |
| `AssemblyMeta.Project` | `AssemblyMeta.Project` |
| `AssemblyMeta.Metadata` | `AssemblyMeta.Metadata` |
| `AssemblyMeta.Git` | `AssemblyMeta.Git` |
| `AssemblyMeta.Resources` | `AssemblyMeta.Resources`，成员为 `Text`、`GetBytes`、`GetStream` |
| `AssemblyMeta.Strings` | `AssemblyMeta.Strings`，来自中性 resx 的编译期常量 |

元包 `AssemblyMeta` 一次引用上面全部子包。安装和各块的写法见[使用说明](使用说明.md)，数据通道见[架构](架构.md)。
