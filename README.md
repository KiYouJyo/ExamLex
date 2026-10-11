<h1 align="center">ExamLex</h1>
<p align="center">面向 TOEFL 与 IELTS 的 Windows 原生英语词典与考试算分工具。</p>
<p align="center">
  <a href="https://github.com/KiYouJyo/ExamLex/actions/workflows/ci.yml"><img src="https://github.com/KiYouJyo/ExamLex/actions/workflows/ci.yml/badge.svg" alt="CI"></a>
  <img src="https://img.shields.io/badge/Windows-WinUI%203-0078D4" alt="WinUI 3">
  <img src="https://img.shields.io/badge/Development-0.1.4-blue" alt="Version">
  <img src="https://img.shields.io/badge/Dictionary-Empty-lightgrey" alt="Empty dictionary">
</p>
<p align="center">简体中文 · <a href="README.en-US.md">English</a> · <a href="README.ja-JP.md">日本語</a></p>

## 项目简介

ExamLex 是基于 **WinUI 3** 的本地优先桌面应用。主页布局参考开源项目 [Flint](https://github.com/sh0ckj0ckey/Flint) 的深色词典界面；本项目重新实现 UI 与业务代码，不复制原项目的词库、图标或专有品牌素材。

**当前为 0.1.4 基础工程，不是可用的完整英语词典。** 首次启动不捆绑词库。设置页可独立检查并下载 [ExamLex-Lexicon](https://github.com/KiYouJyo/ExamLex-Lexicon) 正式 Schema v1 SQLite 发布物；当前公开的只是小规模 A1 测试版，不是 Oxford A1 完整词表。不会下载或解压 Flint/ECDICT 数据包。

## 已有功能

- **词典主页与内页导航**：缩小的 DPI 感知窗口、单词搜索、内部算分页与 Flint 风格设置页、底栏设置/算分器/生词本/窗口置顶。
- **算分器入口**：主页底栏，**在生词本按钮左侧**（与需求截图红框位置一致）。
- **新版 TOEFL iBT**（2026-01-21 起）：四科 1–6、0.5 步长，四科平均并取最近 0.5；显示 CEFR 与旧制总分**参考区间**。
- **IELTS**：四科 0–9、0.5 步长，按官方 0.5 档进位。

- **ExamLex-Lexicon 独立词库**：直接读取其多表 SQLite Schema v1。设置页有专门数据库检查/下载入口，校验 SHA256 与 SQLite 完整性；软件更新完全分开。
- **自动化测试**：独立于 WinUI 的计算核心与单元测试。

> 算分器计算的是已获各科**标准分**的组合结果；不支持仅由答对题数推测正式分数。考试机构成绩报告为准；不同考试之间不作一对一的官方分数换算。

## 开发与构建

要求 Windows 10 19041+ / Windows 11、Visual Studio 2022（Windows App SDK 与 C# 桌面开发工作负载）、.NET 10 SDK。

```powershell
dotnet restore ExamLex.slnx
dotnet test tests/ExamLex.Core.Tests/ExamLex.Core.Tests.csproj -c Release
msbuild src/ExamLex/ExamLex.csproj /restore /p:Configuration=Release /p:Platform=x64
```

在 Visual Studio 打开 `ExamLex.slnx`，选择 **ExamLex** 为启动项目，使用 **x64** 运行。现已提供 [签名 MSIX 验收工作流](https://github.com/KiYouJyo/ExamLex/actions/workflows/msix-acceptance.yml)；在 Actions 的构建产物中下载 `ExamLex_0.1.4.0_x64.msix` 与公开证书。验收版不是 Microsoft Store 已审核发布包，安装说明见 [packaging/INSTALL.md](packaging/INSTALL.md)。

## 项目结构

```text
src/ExamLex/                  WinUI 3 桌面界面、词库 SQLite 服务
src/ExamLex.Core/             考试算分、词条结构（可跨平台测试）
tests/ExamLex.Core.Tests/     xUnit 测试
.github/workflows/            GitHub Actions
docs/                         架构、UI、路线图、规则与发布说明
```

## 资料与版权

- 界面布局灵感：[Flint](https://github.com/sh0ckj0ckey/Flint) · MPL-2.0，作者 sh0ckj0ckey。ExamLex 不包含其源码或内置词典。
- 托福：[ETS TOEFL score scales](https://www.ets.org/toefl/test-takers/ibt/scores/understand-scores.html)。
- 雅思：[IELTS official scoring](https://ielts.org/take-a-test/your-results/ielts-scoring-in-detail)。


详见 [路线图](docs/ROADMAP.md)、[UI 规范](docs/UI_GUIDELINES.md)、[架构](docs/ARCHITECTURE.md)、[隐私](PRIVACY.md)、[贡献指南](CONTRIBUTING.md)及[更新日志](CHANGELOG.md)。

项目代码许可见 [LICENSE](LICENSE)。ExamLex 与 ETS、IELTS 考试主办机构及 Flint 开发者无隶属关系。
