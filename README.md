<p align="center">
  <img src="https://github.com/KiYouJyo/ExamLex/releases/download/v0.2.0/ExamLex-Icon.png" width="128" height="128" alt="ExamLex 应用图标">
</p>
<h1 align="center">ExamLex</h1>
<p align="center">面向 TOEFL 与 IELTS 的 Windows 原生英语考试词典与算分器。</p>
<p align="center">
  <a href="https://github.com/KiYouJyo/ExamLex/releases/latest"><img src="https://img.shields.io/github/v/release/KiYouJyo/ExamLex?display_name=tag&sort=semver&color=2F81F7&label=Release" alt="Release"></a>
  <a href="https://github.com/KiYouJyo/ExamLex/actions/workflows/ci.yml"><img src="https://github.com/KiYouJyo/ExamLex/actions/workflows/ci.yml/badge.svg?branch=main" alt="CI"></a>
  <img src="https://img.shields.io/badge/Windows-WinUI%203-0078D4?logo=windows&logoColor=white" alt="Windows WinUI 3">
  <img src="https://img.shields.io/badge/Architecture-x64-005A9E" alt="x64">
  <img src="https://img.shields.io/badge/Dictionary-ExamLex--Lexicon-6F42C1" alt="ExamLex-Lexicon">
  <img src="https://img.shields.io/badge/Design-Local--first-2EA043" alt="Local-first">
</p>
<p align="center">简体中文 | <a href="README.ja-JP.md">日本語</a> | <a href="README.en-US.md">English</a></p>

## 简介

ExamLex 是基于 **Windows App SDK / WinUI 3** 开发的本地优先英语词典与考试算分工具。界面以开源词典 [Flint](https://github.com/sh0ckj0ckey/Flint) 为基础移植，保留其主要导航、设置及工具栏设计，同时采用独立的词库与更新机制。

## 获取应用

- **[最新 GitHub Release](https://github.com/KiYouJyo/ExamLex/releases/latest)**：下载签名的 x64 MSIX、公开证书、SHA256 校验文件及安装说明。
- **[ExamLex-Lexicon](https://github.com/KiYouJyo/ExamLex-Lexicon)**：独立维护的词典数据库，可在软件内单独检查和安装更新。
- **[安装指南](packaging/INSTALL.md)**：首次侧载时需信任发布证书；软件包不是 Microsoft Store 版本。

## 主要功能

- **词汇查询**：英语单词、词性、英中日释义、音标、形态、例句、配价与搭配、CEFR 分级（以数据库实际已有内容为准）。
- **内置离线词库**：0.2.0 开始，每次构建从 ExamLex-Lexicon **最新正式 GitHub Release** 下载并校验 SQLite，随安装包交付。安装后无需先下载词库即可查询已收录词条。
- **数据库独立更新**：在设置 → 关于中单独检查新版词库；SHA256 和 SQLite Schema 验证通过后才替换本地数据库。
- **TOEFL / IELTS 算分器**：2026 新制 TOEFL 1–6 分制四科汇总、CEFR 与旧制参考区间；IELTS 四科评分及总分计算。
- **Windows 原生体验**：浅色/深色主题、Mica / Acrylic、系统语音朗读、窗口置顶、单实例启动、系统托盘与快捷键。
- **免弹窗更新**：在原有设置卡片内检查并下载应用或词库更新，下载完成后使用“重启应用更新”按钮。
- **本地数据优先**：词典、配置和生词本保存在本机；联网只用于明确的更新检查或下载。

> **当前词库范围**：首个正式软件版本内置当前最新的 `lexicon-a1-pilot-v0.2.1` 试验数据库。它并不等于完整的 Oxford、TOEFL 或 IELTS 词汇库；请以 [ExamLex-Lexicon 的发布说明](https://github.com/KiYouJyo/ExamLex-Lexicon/releases/latest) 为准。

## 安装与更新

打开 [Releases](https://github.com/KiYouJyo/ExamLex/releases/latest) 获取 `ExamLex_0.2.0.0_x64.msix` 与公开证书。首次侧载如遇到证书信任提示，请按照 [INSTALL.md](packaging/INSTALL.md) 安装发布证书，再安装 MSIX。后续可以通过软件内“检查更新”下载并验证新的签名包；词库更新渠道与程序更新分离。

## 系统要求

- Windows 10 2004（19041）及以上 / Windows 11
- x64
- Windows App SDK（MSIX 包内提供应用运行所需框架依赖）

## 开发与构建

```powershell
dotnet test tests/ExamLex.Core.Tests/ExamLex.Core.Tests.csproj -c Release
./scripts/Install-IconAssets.ps1
./scripts/Stage-LatestLexicon.ps1
msbuild src/ExamLex/ExamLex.csproj /restore /p:Configuration=Release /p:Platform=x64
```

CI 在打包时会重新验证词库发布版本、SHA256、SQLite 完整性及实际词条数。签名包验收和 [首个正式 Release 的自动发布流程](.github/workflows/release.yml) 均可在 GitHub Actions 中查看。

## 文档与版权

- [架构](docs/ARCHITECTURE.md) · [路线图](docs/ROADMAP.md) · [发布说明](docs/RELEASE.md)
- [隐私](PRIVACY.md) · [贡献指南](CONTRIBUTING.md) · [安全政策](SECURITY.md) · [更新日志](CHANGELOG.md)
- 界面部分改编自 Flint，按原始 [MPL-2.0](LICENSES/MPL-2.0.txt) 许可保留出处。ExamLex-Lexicon 的词库数据和署名见对应仓库的许可证/发布资料。

ExamLex 与 ETS、IELTS 考试举办机构及 Flint 开发者无官方隶属关系。
