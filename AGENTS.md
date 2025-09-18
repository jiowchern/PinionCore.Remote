# Repository Guidelines

## 專案結構與模組組織
- 解決方案：`PinionCore.sln`（根目錄）。
- 核心程式庫：`PinionCore.Remote*`、`PinionCore.Network`、`PinionCore.Serialization`、`PinionCore.Utility`。
- 測試：`*.Test`、`*.Tests`（如：`PinionCore.Remote.Test`、`PinionCore.Integration.Tests`）。
- 範例：`PinionCore.Samples.*`（HelloWorld 客戶端/伺服器/協議）。
- 工具與產生器：`PinionCore.Remote.Tools.Protocol.Sources*`。
- 文件：`document/`。

## 建置、測試與開發指令
- 還原套件：`dotnet restore PinionCore.sln`。
- 建置（Debug/Release）：`dotnet build PinionCore.sln -c Debug` | `-c Release`。
- 測試含覆蓋率：`dotnet test PinionCore.sln -c Debug /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura`。
- 程式碼格式化（依 `.editorconfig`）：`dotnet format`（PR 前建議執行）。

## 程式風格與命名規範
- 編碼/行尾：UTF-8 BOM、CRLF；修剪行尾空白；結尾保留換行。
- 縮排：4 空白；大括號換行（`csharp_new_line_before_open_brace = all`）。
- Using 排序：`System.*` 置前。
- `var` 使用：偏好內建型別與可推斷型別。
- 命名：
  - 公開欄位：PascalCase。
  - 私有欄位：camelCase 並加結尾底線（例如：`_exampleField`）。
  - 事件：PascalCase 並加 `Event` 後綴。

## 測試指引
- 框架：NUnit（`NUnit`、`NUnit3TestAdapter`）+ `Microsoft.NET.Test.Sdk`；模擬：`NSubstitute`；覆蓋率：`coverlet.msbuild`。
- 位置：與被測專案同層之 `*.Test`/`*.Tests` 專案。
- 命名：檔名 `ClassNameTests.cs`；方法 `MethodName_State_Expected()` 或 `Should_DoThing_When_State()`。
- 要求：新功能與修復需附測試；維持或提升覆蓋率。

## Commit 與 Pull Request 準則
- Commit：祈使句、簡潔；可加範圍前綴（例：`Remote.Server: Fix binder race`）。
- PR：清楚描述、連結議題（`#123`）、提供測試證據/截圖、標註破壞性變更。
- CI 前置：於本機通過 `dotnet build` 與 `dotnet test`。

## 代理（Agent）注意事項
- 變更需遵循本檔與 `.editorconfig`；保持修改最小化且聚焦。
- 僅觸及必要專案；如變更行為，請同步更新文件與測試。
