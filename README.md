<h1 align="center">ExamLex</h1>
<p align="center">面向 TOEFL、IELTS 与 TOEIC 的 Windows 原生英语词典与考试算分工具。</p>
<p align="center">
  <a href="https://github.com/KiYouJyo/ExamLex/actions/workflows/ci.yml"><img src="https://github.com/KiYouJyo/ExamLex/actions/workflows/ci.yml/badge.svg" alt="CI"></a>
  <img src="https://img.shields.io/badge/Windows-WinUI%203-0078D4" alt="WinUI 3">
  <img src="https://img.shields.io/badge/Development-0.1.0-blue" alt="Version">
  <img src="https://img.shields.io/badge/Dictionary-Empty-lightgrey" alt="Empty dictionary">
</p>
<p align="center">简体中文 · <a href="README.en-US.md">English</a> · <a href="README.ja-JP.md">日本語</a></p>

## 项目简介

ExamLex 是基于 **WinUI 3** 的本地优先桌面应用。主页布局参考开源项目 [Flint](https://github.com/sh0ckj0ckey/Flint) 的深色词典界面；本项目重新实现 UI 与业务代码，不复制原项目的词库、图标或专有品牌素材。

**当前为 0.1.0 基础工程，不是可用的完整英语词典。** 首次启动创建空的 SQLite 词典，**不包含任何预装单词数据**，也不下载或解压 Flint/ECDICT 数据包。后续词库导入须另行实现并核实数据授权。

## 已有功能

- **深色词典主页**：搜索框、词条卡片、底栏设置/算分器/生词本/窗口置顶。
- **算分器入口**：主页底栏，**在生词本按钮左侧**（与需求截图红框位置一致）。
- **新版 TOEFL iBT**（2026-01-21 起）：四科 1–6、0.5 步长，四科平均并取最近 0.5；显示 CEFR 与旧制总分**参考区间**。
- **IELTS**：四科 0–9、0.5 步长，按官方 0.5 档进位。
- **TOEIC Listening & Reading**：两科各 5–495、步长 5；相加得到 10–990 总分。
- **空词库基础**：SQLite 词条结构预留音标、释义、英汉解释、例句、CEFR 及考试标签；用户收藏词条保存于本机 JSON。
- **自动化测试**：独立于 WinUI 的计算核心与单元测试。

> 算分器计算的是已获各科**标准分**的组合结果；不支持仅由答对题数推测正式分数。考试机构成绩报告为准；不同考试之间不作一对一的官方分数换算。

## 开发与构建

要求 Windows 10 19041+ / Windows 11、Visual Studio 2022（Windows App SDK 与 C# 桌面开发工作负载）、.NET 10 SDK。

```powershell
dotnet restore ExamLex.slnx
dotnet test tests/ExamLex.Core.Tests/ExamLex.Core.Tests.csproj -c Release
dotnet build src/ExamLex/ExamLex.csproj -c Release -p:Platform=x64
```

在 Visual Studio 打开 `ExamLex.slnx`，选择 **ExamLex** 为启动项目，使用 **x64** 运行。当前不包含 MSIX、商店签名或自动更新；发布包将在后续阶段建立。

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
- 托业：[ETS TOEIC L&R scoring](https://www.ets.org/toeic/test-takers/listening-reading/scores.html)。

详见 [路线图](docs/ROADMAP.md)、[UI 规范](docs/UI_GUIDELINES.md)、[架构](docs/ARCHITECTURE.md)、[隐私](PRIVACY.md)、[贡献指南](CONTRIBUTING.md)及[更新日志](CHANGELOG.md)。

项目代码许可见 [LICENSE](LICENSE)。ExamLex 与 ETS、IELTS 考试主办机构及 Flint 开发者无隶属关系。
