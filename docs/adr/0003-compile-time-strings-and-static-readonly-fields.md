# 生成成员保持编译期常量与 static readonly 字段

Strings 保持编译期常量。`{0}` 和具名占位符留在字面量里，调用方自己格式化。卫星 resx 不参与。改成运行时按清单资源取值，会让值不再是生成源码的纯函数。

`AssemblyMetaVisibility=public` 时，原来可以 const 的成员是 `static readonly` 字段，不是 get-only 属性。字段不会把值内联进引用方。发布后再在字段和属性之间互换，是破坏性变更。

这两条都是对 ThisAssembly 的有意差异。见 README「差异化」。
