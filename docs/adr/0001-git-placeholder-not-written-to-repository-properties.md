# Git 占位符不写回 Repository 属性

设计时写进生成结果的五个值是 `[pending build]`，但 `RepositoryCommit`、`RepositorySha`、`RepositoryRoot`、`RepositoryUrl`、`RepositoryBranch` 是 SDK / SourceLink 的属性名。Git module 一次调用给出两组结果：查询关闭时，生成值是占位符；写回这些属性的仍是解析结果。把占位符写回属性，后面的 target 会把它当成路径或提交号。为了填属性再算一遍，占位规则会再次裂开。`EncodeGit` 只编码生成值，不读这些属性。
