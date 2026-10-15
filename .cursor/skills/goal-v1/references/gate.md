# 1.0 Gate 与验收

## Gate

每一条都可以被命令或文件检查，Hop G 逐条新鲜读取。

1. **对齐**：`docs/research/thisassembly-parity.md` 每行是「已对齐」「超越」或「非目标」（带决议链接）。没有「缺失」「部分」。
2. **票**：没有 open 的 `ready-for-agent`；#1–#5 与立图后建的票都已关闭；map 无 frontier、无 fog；spec 父议题的子票全部关闭。
3. **验证**：`./eng/verify.ps1` 在 HEAD 上通过；`dotnet test AssemblyMeta.slnx -c Release` 无警告。
4. **契约**：`docs/契约.md` 写明生成的成员形状、MSBuild 属性与元数据名、诊断码（含登记表）、包 ID、最低编译器与 SDK 版本，以及 1.0 之后的 SemVer 承诺；有测试守住（生成物金样或等价）。
5. **性能**：每个生成器的增量缓存测试通过；`docs/plans/1.0.md` 里有优化前后的度量数据。
6. **打包**：版本只有一个来源，值为 `1.0.0`；八个包 `dotnet pack` 通过且无警告；包元数据完整（描述、README、许可、仓库、标签、作者），图标与符号包由会议裁决并留决议；有测试或脚本断言包内布局（`analyzers/dotnet/cs`、`tasks`、`buildTransitive`）。
7. **文档**：README、各包 README、`docs/使用说明.md`、`docs/架构.md` 与行为一致；文档站构建通过；有 `CHANGELOG.md`；文档语言策略有会议决议并已执行。
8. **CI**：`.github/workflows/` 里有构建、测试、打包的流水线文件，只在 PR 与推送到 `main` 时运行，没有发布步骤，不引用密钥。
9. **卫生**：`git status` 干净（`artifacts/` 已被忽略），没有临时文件，没有未登记的 TODO、FIXME。
10. **未发布**：`git tag --list 'v*'` 为空；`git ls-remote --tags origin 'v*'` 为空；没有 nupkg 被推送；`main` 没被动过。

## 清洁室

- 上游公开文档可读。上游源码只由研究子代理读，产出只含行为规格（自己的话，加输入输出示例），不含代码块。
- 实现者，即主 agent 与实现阶段的子代理，不读上游源码，只读矩阵。
- 公开契约（生成的类型与成员名、MSBuild 属性与元数据名）与上游的差异是有意的，见 README「差异化」。实现结构自己设计。
- 赞助检查、维护费、遥测都不引入。

## Hop G 收尾

1. 新鲜读取全部十条。任一为假：回到 Detect，把缺口变成票或做掉，不带病收尾。
2. 全真：写 `docs/plans/1.0-验收.md`，提交，推送。内容：
   - 一句话结论，分支名与 HEAD SHA。
   - 怎么验收：命令清单与预期输出（`./eng/verify.ps1`、`artifacts/packages` 下的包、`samples/`）。
   - 对齐矩阵摘要、特色功能清单、架构与性能的变化（含数据）。
   - 决议索引：会议记录链接，含少数意见。
   - **请你复议**：被 ADR 或 `CONTEXT.md` 否决的选项，以及它们妨碍 1.0 的理由。
   - 1.0 之后：Worth exploring，以及仍 open 的 `ready-for-human` 票。
   - 留给你的发布步骤，只列出，不执行：合并 `goal/v1` 到 `main`；建 `v1.0.0` tag；八个包的 `dotnet nuget push` 顺序与命令（需要你的 API key）；GitHub Release。
3. 调用 `UpdateGoal complete`。
4. 给用户的最后一条消息：分支、报告路径、验收命令、留给他的发布步骤。然后停。
