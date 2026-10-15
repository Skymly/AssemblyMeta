# /goal 粘贴稿

整段作为 Goal 粘贴，不要粘贴 SKILL.md。`Follow /goal-v1` 必须保留，续跑才会继续加载本技能。若 `/goal-v1` 无法加载，读取 `.cursor/skills/goal-v1/SKILL.md`。

```text
/goal 把 AssemblyMeta 推进到可验收的 1.0：对标 ThisAssembly，对齐完成度但不照抄代码，可加特色功能，可优化架构与性能。到可验收就停下等我验收，不要发布。Follow /goal-v1.

## Outcome
工作分支 goal/v1 满足 .cursor/skills/goal-v1/references/gate.md 的全部十条，验收报告 docs/plans/1.0-验收.md 已提交并推送。发布动作（tag、nuget push、GitHub Release、合并 main）全部留给我。

Verified by all of:
- gate.md 十条逐条为真，验收报告里每条都有证据
- ./eng/verify.ps1 在 goal/v1 的 HEAD 上通过
- 没有 open 的 ready-for-agent 票，map 无 frontier、无 fog
- git tag --list 'v*' 为空，git ls-remote --tags origin 'v*' 为空，没有任何 nupkg 被推送

Open ready-for-human 票不在验证范围。
Reaching a token budget is not completion.
```
