# AssemblyMeta

把项目和程序集上已经存在的事实，按来源分成不同概念。没有「一次构建」这个概念。

## Language

**构建**:
不使用。没有「一次构建的快照」。
_Avoid_: Build, 构建信息, 构建快照, CI 环境快照

**项目属性**:
一条被消费者点名后才纳入的 MSBuild 属性。
_Avoid_: 构建属性, Build 属性

**目标框架标识**:
这一次编译所针对的框架，由标识符、Moniker 和版本三部分构成。它是一组项目属性。
_Avoid_: TFM, 短名, TargetFramework, 构建目标

**目标框架短名**:
框架的别名，例如 `net8.0`。它不是目标框架标识。
_Avoid_: TFM, Build.Tfm, 构建目标

**程序集信息**:
写进程序集特性的那一组值。
_Avoid_: 元数据, AssemblyMetadata, 构建信息

**构建配置**:
Debug 或 Release。它是程序集信息的一项。
_Avoid_: 构建信息, CI 配置, Build

**分支**:
这次编译对应的 Git 分支。
_Avoid_: CI 身份, CI 环境快照, 构建编号
