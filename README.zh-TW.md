# AssemblyMeta

[English](README.md) | [简体中文](README.zh-CN.md) | **繁體中文** | [日本語](README.ja.md)

[![Verify](https://github.com/Skymly/AssemblyMeta/actions/workflows/verify.yml/badge.svg)](https://github.com/Skymly/AssemblyMeta/actions/workflows/verify.yml)
[![NuGet](https://img.shields.io/nuget/v/AssemblyMeta?label=NuGet&logo=nuget)](https://www.nuget.org/packages/AssemblyMeta)
[![GitHub Release](https://img.shields.io/github/v/release/Skymly/AssemblyMeta?label=Release)](https://github.com/Skymly/AssemblyMeta/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

對標 [ThisAssembly](https://clarius.org/ThisAssembly)：基於 Roslyn 增量原始碼產生器，將 MSBuild、組件、Git、資源與字串中繼資料在編譯期產生為強型別 C# 與 Visual Basic 常數——MIT 授權，無執行階段反射樣板程式碼。

```csharp
// 取代 Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyVersionAttribute>()...
// 類別預設位於 RootNamespace；最上層陳述式需先 using 該命名空間。
var version = AssemblyMeta.Info.Version;
var timeout = TimeSpan.FromSeconds(AssemblyMeta.Constants.Http.TimeoutSeconds);
```

## 安裝

安裝中繼套件（或依需求單獨安裝下方子套件）：

```bash
dotnet add package AssemblyMeta
```

## 概觀

`AssemblyMeta` 中繼套件彙整七個 `IIncrementalGenerator` 子套件（`AssemblyInfo`、`Constants`、`Project`、`Metadata`、`Git`、`Resources`、`Strings`），同時支援 C# 與 Visual Basic。沒有檔案錨點的資料透過 Base64 承載進入 `build_property.*`；`Strings` 的 `.resx` 檔案走 `AdditionalFiles`，IDE 存檔即即時更新。

- 架構說明（簡體中文）：[docs/架构.md](docs/架构.md)
- 使用說明（簡體中文）：[docs/使用说明.md](docs/使用说明.md)
- 端對端驗證：`./eng/verify.ps1`
- 變更紀錄：[CHANGELOG.md](CHANGELOG.md) · 文件網站建置：`dotnet docfx docs/docfx.json` → `artifacts/docs`

## 差異化（vs ThisAssembly）

兩者皆採用 `IIncrementalGenerator`。AssemblyMeta 會產生 C# 與 Visual Basic 程式碼；檔案型應用程式（`#:package`）目前僅支援 C#。下表僅列出實際差異，其中部分屬設計取捨而非單向優勢。

| 項目 | ThisAssembly | AssemblyMeta |
|---|---|---|
| 授權 | 原始碼 MIT；2.1 起 NuGet 套件附帶 OSMF EULA：安裝時須接受，營利使用者須付維護費；無技術強制，自行編譯不受限 | SPDX `MIT`；無需接受授權，不收費 |
| 增量輸入 | AdditionalTexts 合併 AnalyzerConfigOptions，再合併 ParseOptions | 承載產生器僅接收一條承載字串與語言版本；Strings 為 resx 附加檔案加純量；七個產生器均有快取測試 |
| 換行 | 產生原始碼與多行常數值依建置 OS 而定（Windows 為 CRLF，Linux 為 LF） | 固定 `\n`，跨作業系統建置結果一致 |
| Strings | 執行階段依資源取值；支援附屬文化；`{0}` 與具名預留位置產生帶參數方法 | 編譯期常數；預留位置維持字面值；附屬 resx 不參與 |
| 成員型態 | `internal` 一律為 `const`；`public` 為 `=>` 屬性 | 非常數型別自動改為 `static readonly`；`public` 下統一為欄位，無法用於特性引數 |
| 命名空間 | 未設定與空白皆為全域命名空間 | 未設定採用 `RootNamespace`；僅明確空白才是全域 |
| Git | SDK 9+ 會誤截本機分支前綴（`feature/demo` 變成 `s/heads/feature/demo`）；短於 9 字元的 commit hash 導致建置失敗 | 分支依前綴剝離；短雜湊保留完整長度；亦識別 Azure DevOps 與 Jenkins PR 編號 |
| 文字資源 | 預設文字副檔名含 `.resx`，執行階段存取 `Text` 會找不到資訊清單資源 | `.resx` 預設不進入 Resources，交由 Strings 處理 |
| 命名強健性 | C# 關鍵字不跳脫；區段衝突會擲回例外導致產生器崩潰 | C# 關鍵字加上 `@`；VB 關鍵字使用方括號；VB 中僅大小寫差異回報 `AM0001`；衝突與改名回報 `AM0001`/`AM0008` |
| 診斷 | TA001–TA003 | `AM0001`–`AM0013`，拆分為產生器與 MSBuild 兩條路徑 |
| 發布 | 長期 `NUGET_API_KEY` | OIDC Trusted Publishing，限定授權推送者 |

仍不實作的項目：F#；Strings 無在地化與帶參數格式化方法；`AssemblyMeta.Info` 固定九個欄位，上游則收集所有 `Assembly*` 特性；無 `Root`/`RootComment`、`Kind`、Vsix、F#。文件網站以中文為主，診斷訊息為英中雙語。

## 產生進入點

- `static partial class AssemblyMeta`。未設定 `AssemblyMetaNamespace` 時採用專案的 `RootNamespace`；明確設為空白時位於全域命名空間
- 最上層陳述式需先 `using` 該命名空間，才能直接寫 `AssemblyMeta.X`
- 各子套件會將巢狀型別掛載至該 partial 類別（`Info`、`Constants`、`Git`、`Project`、`Metadata`、`Resources`、`Strings`）

## 套件

| 套件 | 功能 |
|---|---|
| `AssemblyMeta` | 中繼套件，彙整下列七個子套件 |
| `AssemblyMeta.AssemblyInfo` | 九個組件資訊常數 |
| `AssemblyMeta.Constants` | `<Constant>` / `<FileConstant>` MSBuild 項目 → 強型別常數 |
| `AssemblyMeta.Project` | `<ProjectProperty>` 選擇加入的專案屬性常數 |
| `AssemblyMeta.Metadata` | `<AssemblyMetadata>` 項目常數 |
| `AssemblyMeta.Git` | Commit/Sha/Root/Url/Branch（SDK 內建 SourceLink） |
| `AssemblyMeta.Resources` | `EmbeddedResource` 強型別存取（Text/GetBytes/GetStream） |
| `AssemblyMeta.Strings` | 中性 `.resx` 編譯期常數 |

## 技術架構

每個子套件包含兩個專案：

- `AssemblyMeta.X.csproj` — 封裝：封裝分析器 DLL 與 `buildTransitive` 下的 props/targets
- `AssemblyMeta.X.Generator.csproj` — `netstandard2.0` 分析器，參考 `Microsoft.CodeAnalysis.CSharp`

資料流向：

```
MSBuild props/targets
  → CompilerVisibleProperty / CompilerVisibleItemMetadata
  → AnalyzerConfigOptions（build_property.* / build_metadata.*）
  → IncrementalGenerator 讀取
  → 輸出 C# 原始碼（AssemblyMeta partial class）
```

重點：

- `buildTransitive` 將 props 與 targets 交給撰寫 `PackageReference` 的專案；分析器停留在該層，不會傳遞至再參考該專案的下層專案
- 六個承載產生器的增量輸入僅有一條承載字串與 C# 語言版本；Strings 的輸入則為 `.resx` `AdditionalFiles` 加上兩個純量屬性
- C# 與 Visual Basic 各自擁有獨立產生器，不以語言分派介面實作；`SourceEmission.Emit` 仍僅服務 C#。VB 組件位於 `analyzers/dotnet/roslyn4.11/vb`

## 測試策略

- 單元測試：Roslyn 編譯斷言（由 `Microsoft.CodeAnalysis.CSharp` 驅動產生器並驗證輸出語法樹）
- 端對端測試：`samples/` 範例專案執行真實 `dotnet build`，斷言編譯產物中的常數值

## 發布與參考資料

- 發布由標籤觸發，並透過 NuGet Trusted Publishing（OIDC）自動推送；請參閱 [docs/releasing.md](docs/releasing.md)。
- ThisAssembly 原始碼：[devlooped/ThisAssembly](https://github.com/devlooped/ThisAssembly)
- 關鍵 MSBuild 機制：`CompilerVisibleProperty`、`CompilerVisibleItemMetadata`、`EnableSourceControlManagerQueries`
