<p align="center"><img src="https://github.com/KiYouJyo/ExamLex/releases/download/v0.2.0/ExamLex-Icon.png" width="128" height="128" alt="ExamLex アイコン"></p>
<h1 align="center">ExamLex</h1>
<p align="center">TOEFL・IELTS に対応する Windows ネイティブ英語辞書・スコア計算ツール。</p>
<p align="center"><a href="https://github.com/KiYouJyo/ExamLex/releases/latest"><img src="https://img.shields.io/github/v/release/KiYouJyo/ExamLex?label=Release&color=2F81F7" alt="Release"></a> <a href="https://github.com/KiYouJyo/ExamLex/actions/workflows/ci.yml"><img src="https://github.com/KiYouJyo/ExamLex/actions/workflows/ci.yml/badge.svg?branch=main" alt="CI"></a> <img src="https://img.shields.io/badge/Windows-WinUI%203-0078D4" alt="Windows WinUI 3"></p>
<p align="center"><a href="README.md">简体中文</a> | 日本語 | <a href="README.en-US.md">English</a></p>

## ダウンロード
[最新 GitHub Release](https://github.com/KiYouJyo/ExamLex/releases/latest) から署名済み x64 MSIX、公開証明書と SHA256 検証ファイルを取得してください。初回のインストール方法は [INSTALL.md](packaging/INSTALL.md) を参照してください。

## 主な機能
- オフライン英語辞書（CEFR、訳語、発音、例文などは収録データに準拠）。
- ビルド時に公開済みの最新 [ExamLex-Lexicon](https://github.com/KiYouJyo/ExamLex-Lexicon) を SHA256・SQLite 検証の上でアプリに同梱。辞書のみの更新も可能。
- TOEFL iBT（2026 年以降）・IELTS スコア計算。
- WinUI 3 / Flint ベースの画面、単一インスタンス起動、テーマ、ショートカット、トレイ対応。

**収録範囲:** 0.2.0 に同梱する辞書は A1 の試験的なパイロット版であり、試験対策用の完全版ではありません。

## ライセンス
UI の一部は [Flint](https://github.com/sh0ckj0ckey/Flint) を MPL-2.0 に従って移植しています。詳しくは [中文 README](README.md) を参照してください。
