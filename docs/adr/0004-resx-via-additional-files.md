# resx 经 AdditionalFiles 由生成器直读

`Strings` 的 resx 值不再由 MSBuild 任务编码进 `AssemblyMeta_StringsPayload`，改为把 resx 加进 `AdditionalFiles`，生成器读文件文本并在 Roslyn 层解析。IDE 里保存 resx 后生成物即时刷新，不再等下一次构建。

通道按数据形态分工：没有文件锚点的属性和项仍走单属性 Base64 载荷（`build_metadata` 无法枚举，editorconfig 会截断）；真实文件的内容走 `AdditionalFiles`，这是 Roslyn 给文件输入设计的通道。命名空间与可见性两个标量直接经 `build_property.AssemblyMetaNamespace`、`build_property.AssemblyMetaVisibility` 传入。卫星过滤与 `AssemblyMetaStrings` 排除在生成器里按 `build_metadata.AdditionalFiles.*` 逐项判定。

`EncodeStrings` 任务随之删除，`AssemblyMeta.Strings` 包不再带 `tasks/`。`AM0007`、`AM0010`、`AM0011` 改在生成器报告并带文件位置；`AM0011` 收窄为无法读取或解析，文件缺失由 `GenerateResource` 自己报错。无法识别的 `AssemblyMetaVisibility` 由 Strings 报告 `AM0012`。
