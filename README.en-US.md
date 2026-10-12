<p align="center"><img src="https://github.com/KiYouJyo/ExamLex/releases/download/v0.2.0/ExamLex-Icon.png" width="128" height="128" alt="ExamLex icon"></p>
<h1 align="center">ExamLex</h1>
<p align="center">Native Windows English examination dictionary and TOEFL / IELTS score calculator.</p>
<p align="center"><a href="https://github.com/KiYouJyo/ExamLex/releases/latest"><img src="https://img.shields.io/github/v/release/KiYouJyo/ExamLex?label=Release&color=2F81F7" alt="Release"></a> <a href="https://github.com/KiYouJyo/ExamLex/actions/workflows/ci.yml"><img src="https://github.com/KiYouJyo/ExamLex/actions/workflows/ci.yml/badge.svg?branch=main" alt="CI"></a> <img src="https://img.shields.io/badge/Windows-WinUI%203-0078D4" alt="Windows WinUI 3"></p>
<p align="center"><a href="README.md">简体中文</a> | <a href="README.ja-JP.md">日本語</a> | English</p>

## Install
Download the signed x64 MSIX, public signing certificate and SHA256 checksums from the [latest release](https://github.com/KiYouJyo/ExamLex/releases/latest). Follow [INSTALL.md](packaging/INSTALL.md) for certificate trust instructions on first sideload.

## Features
- Offline English dictionary with available CEFR tags, translations, pronunciations, examples and word information.
- Bundles the latest **published** [ExamLex-Lexicon](https://github.com/KiYouJyo/ExamLex-Lexicon) at build time, verified by SHA256 and SQLite schema, with separate in-app updates.
- TOEFL iBT (2026 scale) and IELTS score calculators.
- Windows App SDK / WinUI 3, Flint-derived UI, single-instance launch, theme, global shortcut and tray support.
- In-page app update status, download and restart without modal dialogs.

**Dictionary scope:** The initial 0.2.0 release bundles a limited A1 *pilot* lexicon, not a complete exam dictionary.

## Credits
The UI derives from [Flint](https://github.com/sh0ckj0ckey/Flint) under MPL-2.0. ExamLex is not affiliated with exam providers. See [Chinese README](README.md) for build, architecture and license information.
