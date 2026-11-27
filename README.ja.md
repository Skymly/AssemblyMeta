# AssemblyMeta

[English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | **日本語**

[![Verify](https://github.com/Skymly/AssemblyMeta/actions/workflows/verify.yml/badge.svg)](https://github.com/Skymly/AssemblyMeta/actions/workflows/verify.yml)
[![NuGet](https://img.shields.io/nuget/v/AssemblyMeta?label=NuGet&logo=nuget)](https://www.nuget.org/packages/AssemblyMeta)
[![GitHub Release](https://img.shields.io/github/v/release/Skymly/AssemblyMeta?label=Release)](https://github.com/Skymly/AssemblyMeta/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

[ThisAssembly](https://clarius.org/ThisAssembly) に着想を得た Roslyn インクリメンタルソースジェネレーターです。MSBuild・アセンブリ・Git・リソース・文字列のメタ情報をコンパイル時に C# / Visual Basic の厳密型定数として生成し、MIT ライセンスで実行時リフレクションのボイラープレートを排除します。

```csharp
// Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyVersionAttribute>()... の代わりに
// クラスは既定で RootNamespace に生成されます。トップレベルステートメントではその名前空間を using してください。
var version = AssemblyMeta.Info.Version;
var timeout = TimeSpan.FromSeconds(AssemblyMeta.Constants.Http.TimeoutSeconds);
```

## インストール

メタパッケージ（または後述の各サブパッケージ）をインストールします：

```bash
dotnet add package AssemblyMeta
```

## 概要

`AssemblyMeta` メタパッケージは 7 つの `IIncrementalGenerator` サブパッケージ（`AssemblyInfo`、`Constants`、`Project`、`Metadata`、`Git`、`Resources`、`Strings`）を集約し、C# と Visual Basic の双方に対応します。ファイルアンカーのないデータは単一 Base64 ペイロードとして `build_property.*` 経由で渡され、`Strings` の `.resx` は `AdditionalFiles` 経由で読み取られます。

- アーキテクチャ（中国語）：[docs/架构.md](docs/架构.md)
- 利用ガイド（中国語）：[docs/使用说明.md](docs/使用说明.md)
- E2E 検証：`./eng/verify.ps1`
- 変更履歴：[CHANGELOG.md](CHANGELOG.md) · ドキュメントビルド：`dotnet docfx docs/docfx.json` → `artifacts/docs`

## ThisAssembly との違い

どちらも `IIncrementalGenerator` を採用しています。AssemblyMeta は C# と Visual Basic を生成しますが、file-based apps（`#:package`）は引き続き C# のみです。以下の表は実際の差分のみを記載しており、一部は優劣ではなくトレードオフです。

| 項目 | ThisAssembly | AssemblyMeta |
|---|---|---|
| ライセンス | ソース MIT；2.1 以降の NuGet パッケージは OSMF EULA 付属（導入時同意・収益利用者の保守料あり；技術的強制はなく自前ビルドは自由） | SPDX `MIT`；同意不要・費用なし |
| インクリメンタル入力 | AdditionalTexts と AnalyzerConfigOptions、さらに ParseOptions を結合 | 1 つのペイロード文字列と言語バージョン（Strings は resx AdditionalFiles + スカラー）；全 7 ジェネレーターにキャッシュテストあり |
| 改行コード | 生成ソースと複数行定数値がビルド OS に依存（Windows は CRLF、Linux は LF） | 常に `\n`；OS 間で出力が同一 |
| Strings | 実行時リソース解決；サテライトカルチャ対応；`{0}` や名前付きプレースホルダーから引数付きメソッド生成 | コンパイル時定数；プレースホルダーはリテラル維持；サテライト resx は対象外 |
| メンバー形状 | `internal` は常に `const`；`public` は `=>` プロパティ | 非定数型は `static readonly`；`public` ではすべてフィールドとなり属性引数には使用不可 |
| 名前空間 | 未設定・空文字のどちらもグローバル | 未設定時は `RootNamespace`；明示的な空文字のみグローバル |
| Git | SDK 9+ でローカルブランチ接頭辞の除去を誤る（`feature/demo` → `s/heads/feature/demo`）；9 文字未満のハッシュでビルド失敗 | 接頭辞単位で除去；短縮ハッシュも全長維持；Azure DevOps / Jenkins の PR 番号も認識 |
| テキストリソース | 既定テキスト拡張子に `.resx` が含まれ、`Text` 参照時にマニフェストリソース未検出になる | `.resx` は既定で Resources から除外し Strings へ回す |
| 命名の堅牢性 | C# キーワードをエスケープしない；セグメント衝突時に例外でクラッシュ | C# キーワードに `@`、VB キーワードに角括弧を付与；VB で大文字小文字のみ差がある場合は `AM0001`、衝突やリネームは `AM0001`/`AM0008` を報告 |
| 診断 | TA001–TA003 | `AM0001`–`AM0013`（ジェネレーター経路と MSBuild 経路に分割） |
| 公開方式 | 長期 `NUGET_API_KEY` | 指定プッシャー限定の OIDC Trusted Publishing |

引き続き対象外：F#；Strings のローカライズおよび引数付きフォーマットメソッド；`AssemblyMeta.Info` は 9 フィールド固定（上流は全 `Assembly*` 属性を収集）；`Root`/`RootComment`、`Kind`、Vsix は非対応。ドキュメントサイトは中国語中心、診断メッセージは英中バイリンガルです。

## エントリーポイント

- `static partial class AssemblyMeta`。`AssemblyMetaNamespace` 未設定時はプロジェクトの `RootNamespace` を使用し、明示的に空文字を指定するとグローバル名前空間になります
- トップレベルステートメントから直接 `AssemblyMeta.X` を参照するには、その名前空間の `using` が必要です
- 各サブパッケージはこの partial クラスにネスト型（`Info`、`Constants`、`Git`、`Project`、`Metadata`、`Resources`、`Strings`）を追加します

## パッケージ

| パッケージ | 用途 |
|---|---|
| `AssemblyMeta` | 以下 7 つのサブパッケージを集約するメタパッケージ |
| `AssemblyMeta.AssemblyInfo` | 9 種類のアセンブリ情報定数 |
| `AssemblyMeta.Constants` | `<Constant>` / `<FileConstant>` MSBuild アイテム → 厳密型定数 |
| `AssemblyMeta.Project` | `<ProjectProperty>` オプトイン型プロジェクトプロパティ定数 |
| `AssemblyMeta.Metadata` | `<AssemblyMetadata>` アイテム定数 |
| `AssemblyMeta.Git` | Commit/Sha/Root/Url/Branch（SDK 組み込み SourceLink） |
| `AssemblyMeta.Resources` | `EmbeddedResource` への厳密型アクセス（Text/GetBytes/GetStream） |
| `AssemblyMeta.Strings` | ニュートラル `.resx` のコンパイル時定数 |

## アーキテクチャ

各サブパッケージは 2 つのプロジェクトで構成されます：

- `AssemblyMeta.X.csproj` — パッケージング：アナライザー DLL と `buildTransitive` 配下の props/targets を同梱
- `AssemblyMeta.X.Generator.csproj` — `Microsoft.CodeAnalysis.CSharp` を参照する `netstandard2.0` アナライザー

データフロー：

```
MSBuild props/targets
  → CompilerVisibleProperty / CompilerVisibleItemMetadata
  → AnalyzerConfigOptions（build_property.* / build_metadata.*）
  → IncrementalGenerator が読み取り
  → C# ソース生成（AssemblyMeta partial class）
```

主なポイント：

- `buildTransitive` により、`PackageReference` を記述したプロジェクトへ props/targets が渡されます。アナライザーはその層に留まり、さらにそれを参照するプロジェクトへは波及しません
- 6 つのペイロードジェネレーターは 1 本のペイロード文字列と C# 言語バージョンのみをインクリメンタル入力とします。Strings は `.resx` の `AdditionalFiles` と 2 つのスカラープロパティを入力とします
- C# と Visual Basic はそれぞれ独立したジェネレーターを持ち、言語ディスパッチ層は設けません。`SourceEmission.Emit` は引き続き C# 専用で、VB アセンブリは `analyzers/dotnet/roslyn4.11/vb` に配置されます

## テスト方針

- 単体テスト：Roslyn コンパイルアサーション（`Microsoft.CodeAnalysis.CSharp` でジェネレーターを駆動し、出力構文木を検証）
- E2E：`samples/` のサンプルプロジェクトを実際に `dotnet build` し、コンパイル結果内の定数値を検証

## リリース・参考

- リリースはタグ駆動で NuGet Trusted Publishing（OIDC）により公開されます。詳細は [docs/releasing.md](docs/releasing.md) を参照してください。
- ThisAssembly ソース：[devlooped/ThisAssembly](https://github.com/devlooped/ThisAssembly)
- 主要な MSBuild 機構：`CompilerVisibleProperty`、`CompilerVisibleItemMetadata`、`EnableSourceControlManagerQueries`
