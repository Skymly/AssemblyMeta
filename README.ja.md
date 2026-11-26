# AssemblyMeta

[English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | **日本語**

[ThisAssembly](https://clarius.org/ThisAssembly) に着想を得た Roslyn ソースジェネレーターです。プロジェクト／アセンブリのメタ情報をコンパイル時に厳密型定数として生成し、実行時リフレクションのボイラープレートを排除します。

```csharp
// Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyVersionAttribute>()... の代わりに
// クラスは既定で RootNamespace に生成されます。トップレベルステートメントではその名前空間を using してください。
var version = AssemblyMeta.Info.Version;
var timeout = TimeSpan.FromSeconds(AssemblyMeta.Constants.Http.TimeoutSeconds);
```

- ライセンス：MIT（完全無料・メンテナンス料モデルなし）
- NuGet：[`AssemblyMeta`](https://www.nuget.org/packages/AssemblyMeta)（Trusted Publishing、タグ駆動リリース）
- リポジトリ：[`Skymly/AssemblyMeta`](https://github.com/Skymly/AssemblyMeta)

## 現在の状況

1.0.0 の挙動は [CHANGELOG.md](CHANGELOG.md) に記録されています。メタパッケージは AssemblyInfo、Constants、Project、Metadata、Git、Resources、Strings を集約します。ジェネレーターは `IIncrementalGenerator` を実装し、ファイルアンカーのないデータは `build_property.*` 経由の単一 Base64 ペイロードとして渡されます。Strings の `.resx` は `AdditionalFiles` 経由で読み取られ、IDE 保存時に即座に再生成されます。ドキュメントサイトは `dotnet docfx docs/docfx.json` で `artifacts/docs` にビルドされます。

- アーキテクチャ（中国語）：[docs/架构.md](docs/架构.md)
- 利用ガイド（中国語）：[docs/使用说明.md](docs/使用说明.md)
- E2E 検証：`./eng/verify.ps1`

## リリース

パッケージバージョンは `Directory.Build.props` の `AssemblyMetaPackageVersion` のみに定義されています。`v` + そのバージョンのタグをプッシュします（現在が `1.2.0` なら `v1.2.0`）。

`.github/workflows/release.yml` はそのタグで `./eng/verify.ps1` を実行したのち、NuGet Trusted Publishing を用いて `https://api.nuget.org/v3/index.json` へ公開します。7 つのサブパッケージを先にプッシュし、続けてメタパッケージを公開したあと、対応する `CHANGELOG.md` セクションから GitHub Release を作成します。長期 API キーは使わず、リポジトリに NuGet シークレットも保存しません。`Skymly` または `wys0610` がプッシュしたタグのみ公開され、エージェント経由のプッシュは `github.triggering_actor` で判定されます。それ以外のユーザーがプッシュしたタグはサイレントスキップではなく失敗します。

nuget.org 側のポリシーは `Skymly/AssemblyMeta` リポジトリ、`release.yml` ワークフローファイル名、パッケージオーナー `Skym`、パッケージ名 `AssemblyMeta*` を信頼します。ポリシーに GitHub Environment が設定されていないため、ジョブ側にも Environment を追加してはいけません。ポリシー作成から 7 日以内に 1 回公開を成功させると永続化されます。

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

## 参考

- ThisAssembly ソース：[devlooped/ThisAssembly](https://github.com/devlooped/ThisAssembly)
- 主要な MSBuild 機構：`CompilerVisibleProperty`、`CompilerVisibleItemMetadata`、`EnableSourceControlManagerQueries`
