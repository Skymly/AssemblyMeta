# 起跑、立图、拆票

## Hop 0 起跑

1. `git switch -c goal/v1`，从本地 `main` 当前 HEAD 切出。
2. 补标签（缺才建，`gh label list` 先看）：五个分诊标签（映射见 `docs/agents/triage-labels.md`）、`wayfinder:map`、`wayfinder:research`、`wayfinder:prototype`、`wayfinder:grilling`、`wayfinder:task`、`spec`。
3. 第一个提交：原样纳入未跟踪的 `docs/adr/0001-git-placeholder-not-written-to-repository-properties.md`（用户已写的决定）和 `.cursor/skills/goal-v1/`，不改内容。信息：`起跑：纳入 ADR 0001 与 goal-v1 提示词`。
4. 基线：`./eng/verify.ps1`。绿：记进 `docs/plans/1.0.md` 的现状。红：开一张 `bug` + `ready-for-agent` 票，写明失败原因，它是第一张实现票。
5. `git push -u origin goal/v1`。

Done when 分支已推送，标签齐全，基线结果有记录。

## Hop F 立图

### 1. 清洁室研究

派一个只读 `/research` 子代理，读上游公开资料：README、`docs`、NuGet 包页、changelog、发布说明、issue 里描述的行为。需要确认边界行为时，它可以读上游源码。产出 `docs/research/thisassembly-parity.md`，只写行为规格。

每行一项能力：

- 可观察行为：输入（csproj、resx、属性）→ 生成物。用自己的话，加输入输出示例。
- 边界情形。
- AssemblyMeta 现状：已对齐 / 部分 / 缺失 / 刻意不同（链接 README「差异化」或 ADR）。
- 证据：本仓的测试或文档位置。

矩阵不含上游代码块，不含上游内部结构。实现者之后只读这份矩阵，不读上游源码。

已知线索，研究时逐条核实，不当结论用：Strings 对 `{0}`、具名占位符是否生成带参方法；Resources 的文本扩展名与 `Kind`、`Comment` 元数据；Git 是否还有 `Commit`、`Sha`、`Root`、`Url`、`Branch` 之外的成员；`Vsix`、`Prerequisites` 是否值得对齐；上游的 CI 与包版本方案；各包 README 与包元数据。

### 2. 现状盘点

一个只读子代理扫本仓，写进 `docs/plans/1.0.md` 的「现状」：公共契约表面（生成成员、MSBuild 属性与元数据、诊断码、包 ID）、测试覆盖与增量缓存测试、包元数据缺什么、有没有 CI、文档站、最低 Roslyn 版本。

### 3. 路线文档

`docs/plans/1.0.md`：Destination（就是 [gate](gate.md)）、五条工作流、非目标、决议索引、Worth exploring。

五条工作流：

- **对齐**：矩阵里「缺失」「部分」的行。
- **架构**：既有 #1–#5，再加 Hop R 的复扫。
- **性能**：先立度量，再优化。每项优化记前后数据。没有数据的优化不做。增量缓存的断言已存在，补分配或耗时的度量，度量工具放在 `eng/` 或独立的 dev-only 项目，不进包，不进 `verify.ps1`。
- **发布就绪**：契约文档、包元数据、CI 文件、`CHANGELOG.md`、版本号、验收报告。
- **特色功能**（可选）：见下面的准入线。

### 4. 立图

建 map：标题 `AssemblyMeta 1.0 路线图`，标签 `wayfinder:map`，body 按 `/wayfinder` 的格式。Destination 指向 gate，Notes 指向本技能、`CONTEXT.md`、ADR，fog 写进 Not yet specified。能明确提出的问题建成子票，再二次补阻塞边。

### 5. 分诊既有 #1–#5

#1 是总议题，不当票做；#2–#5 是上一轮架构审查产出的设计票。用 `/triage`，推荐交会议。通过的加 `ready-for-agent`（已有 `enhancement`），挂到 map 下，按票面自述的先后建阻塞边（#3 自述实现等 #4）。不通过的写决议后 `wontfix` 关闭。

同一跳不解决任何 map 票。

Done when 矩阵已落盘，map 已建，#1–#5 已分诊。

## Hop C 解决一张 map 票

按 `/wayfinder-next`：认领 frontier 里编号最小的一张，按类型解决，记录，停。

- `grilling`：`/grilling` + `/domain-modeling`。提问方是[三方会议](council.md)，整轮 frontier 合成一次会议。
- `research`：只读子代理，结果写在票上。可并行。
- `prototype`：做会议推荐的那个版本，链接产物。
- `task`：能自己做的就做；只有人能做的 → Blocked stop。

记录：评论决议、关闭票、把一行指针追加到 map 的 Decisions so far、毕业新的 fog。

## Hop D 拆 spec 与票

按 `/to-spec` → `/to-tickets`，每条工作流一份 spec。

- seam 由会议确认：用尽量高的 seam，越少越好，理想是一个。
- 票是竖切：窄而完整，单独可验证，塞得进一个上下文窗口。先做 prefactor。
- `/to-tickets` 的 quiz 交会议。
- 建之前先列出已有子票，不建第二套。
- spec 是父议题，不是实现票：发布后去掉 `ready-for-agent`，加 `spec`，挂到 map 下。子票与 spec 在同一跳创建，编号写回 `docs/plans/1.0.md`。

## Hop E 毕业 fog

`/wayfinder` 只做到把 fog 毕业成票为止。不解决任何票。

## Hop R 架构复扫

只做一次，在对齐、特色、性能的票全部关闭之后。

1. `/improve-codebase-architecture`，范围取本分支的 git 热点、`CONTEXT.md`、ADR，再加上各票评论里的 Worth exploring。HTML 报告写进临时目录，不提交。
2. 候选交会议挑。只有 Strong 立票，最多 5 张，与 ADR 冲突的不立。
3. Worth exploring 与 Speculative 写进 `docs/plans/1.0.md`，不立票。
4. 在 map 的 Decisions so far 追加「架构复扫完成」。

## 特色功能准入线

1. 立足项目与程序集上已存在的事实。不引入「一次构建」的快照：时间戳、机器、用户、CI 身份都不进生成物。
2. 确定性：同一输入，同一输出。
3. 有 `docs/使用说明.md` 的章节，有经 seam 的测试。
4. 会议通过。1.0 之内最多三个，每个一张竖切票。
5. 对齐缺口先于特色功能。

线索，会议裁决，不是承诺：带参 Strings 之后，卫星 resx 与中性 resx 的占位符一致性诊断；把 `GetCustomAttribute<Assembly*Attribute>` 一类反射样板迁到 `AssemblyMeta.Info.*` 的迁移指南或分析器；面向 ThisAssembly 用户的迁移说明。
