---
name: goal-v1
description: AssemblyMeta 1.0 无人值守续跑：每轮检测现场、只走一跳、写状态后停下；到达可验收的 1.0 即停，不发布。由 /goal 里的 "Follow /goal-v1" 加载。
disable-model-invocation: true
---

# Goal v1

`/goal` 只写 Outcome 与验证条件，本技能负责每一轮续跑。本目录由用户拥有，运行中不改动。

**终点**：`goal/v1` 分支满足 [gate](references/gate.md)，验收报告已写好。然后停下，由用户验收并亲自发布。

**对标**：devlooped/ThisAssembly 的公开行为与完成度。对齐行为，不照抄代码。特色功能、架构与性能优化都在范围内，各有准入线（[chart](references/chart.md)）。

## 不可越过的线

发布全部留给用户：不建 `v*` tag，不 `git push --tags`，不 `dotnet nuget push`，不 `gh release`，不合并也不推送 `main`，不启用或触发会发布的流水线，不写入任何密钥。`dotnet pack` 只输出到 `artifacts/`。

- 只往 `goal/v1` 提交，只做普通 `git push origin goal/v1`。`main` 领先 `origin/main` 的提交是用户的事，不去推。
- 绿色靠改实现得来：不删测试、不放宽断言、不加 `NoWarn` 或 `#pragma warning disable`，`TreatWarningsAsErrors` 保持开启。
- `CONTEXT.md` 与既有 ADR 是用户拍板的：可以新增条目，不改写既有条目。与之冲突的选项出局（[council](references/council.md)）。
- 凭据、面板点击、只有人能做的步骤：不拦其余进度就开一张 `ready-for-human` 票，继续做别的；拦住全部进度才 Blocked stop。

## 现场规则

- 状态只存在于 git、GitHub issue（map 与票）、`docs/plans/1.0.md`。不靠对话记忆。
- **一次一票。** 实现票串行：没有并行实现，没有第二个工作树，没有第二个写代码的 agent。并行只留给只读子代理（会议成员、审查员、研究员）。
- 提交只 `git add` 本票涉及的路径，不用 `git add -A`。提交信息、issue、文档用简体中文，提交信息格式 `#N 标题`。
- Shell 是 PowerShell，不依赖 `&&`。`./eng/verify.ps1` 耗时长，放后台并轮询。
- 收尾前删掉临时文件（`_*.py`、`_*.md`、scratch）。
- 阻塞边、frontier、claim 的 `gh` 写法见 `docs/agents/issue-tracker.md`。

## Process

### 1. Detect

读现场，忽略对话记忆：`git status -sb`、当前分支、`git log main..HEAD --oneline`、`gh issue list --state all --limit 300 --json number,title,state,labels,assignees,body`、`docs/plans/1.0.md`。

Done when 能说出：当前分支、我 claim 且未关闭的票、frontier（open + unblocked + unassigned）、Gate 是否全真。

### 2. 选一跳

第一个命中的，做完停。

| | 条件 | 动作 |
|---|---|---|
| 0 | `goal/v1` 不存在，或没有起跑提交 | 起跑（[chart](references/chart.md) Hop 0） |
| A | 有我 claim 的 open 实现票，或工作区有该票未收尾的改动 | 续做该票的实现回路 |
| B | 没有 A，且有 unblocked、unassigned 的 `ready-for-agent` 实现票 | 实现回路，编号最小者 |
| C | open `wayfinder:map` 有 frontier 票 | 解决一张 map 票，三方会议代答 |
| D | map 无 frontier、无 fog，spec 未发布或有决策没落成票 | `/to-spec` → `/to-tickets` |
| E | map 还有 fog | `/wayfinder`，只把 fog 毕业成票 |
| F | 从未存在过 `wayfinder:map`（含已关闭） | 立图（[chart](references/chart.md) Hop F） |
| R | A–F 不命中，且 map 的 Decisions so far 没有架构复扫记录 | 架构复扫（[chart](references/chart.md) Hop R） |
| G | A–R 均不命中 | Gate 复核（[gate](references/gate.md)） |

Done when 恰好一跳已结束，或已 Blocked stop。

### 3. 实现回路（hop A、B）

读 [review](references/review.md) 后开始。

1. **Claim**：`gh issue edit <n> --add-assignee @me`；记下 `base` = 当前 HEAD。
2. **Plan**：读票面、评论、`CONTEXT.md`、相关 ADR，只读需要的代码，写本票计划（范围、测试 seam、风险）。计划不出票面范围、不碰公共契约就自批，直接执行，不进入等待审批的 Plan 模式。否则开会（[council](references/council.md)）。
3. **Build**：`/tdd`，只在票面或 spec 已定的 seam 上写测试，红 → 绿，一次一个竖切。性能票先度量再优化。清洁室规则见 [gate](references/gate.md)。
4. **Verify**：`./eng/verify.ps1` 全绿。
5. **自审回路**（[review](references/review.md)）：直到最近一轮针对当前 HEAD 的审查 MustFix = 0。
6. **Close**：在票上评论（提交、verify 结果、自审轮数、会议决议、ShouldFix 去向），`gh issue close <n>`，`git push origin goal/v1`。

Done when 票已关闭、分支已推送（推送失败则写明，不阻塞）、工作区没有临时文件。

### 4. 三方会议

任何一处 skill 要问人，都由 [council](references/council.md) 代答：`/grilling` 的问题、seam 确认、`/to-tickets` 的 quiz、`/triage` 的推荐、需要判断的 Plan 批准。不自己答，不等人。子代理调用失败重试一次，仍失败则 Blocked stop，不一个人分饰三角。

### 5. Blocked stop

不调用 `UpdateGoal`。在 map 上评论：试过的路径、证据、卡点、解开需要什么；map 还不存在就写在对话里。然后停。

Reaching a token budget is not completion.

### 6. Hop close-out

在 map 上评论四行（map 还不存在就写在对话里），然后**停**，不开始下一跳：

- 当前 / 下一步 / 刚做完的一跳（名称 + 链接）
- 证据（测试、提交、关闭的票、`ci: none`）
- 剩余
- 续跑该走的下一跳

`UpdateGoal complete` 只在 Gate 新鲜复核全真、验收报告已提交并推送之后调用。
