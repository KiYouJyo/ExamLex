# ExamLex

ExamLex は TOEFL iBT・IELTS・TOEIC L&R 向けの Windows（WinUI 3）英語辞書アプリ／スコア計算ツールです。

**現在は v0.1.0 の基盤実装です。辞書データは一切同梱されていません。** 初回起動時に空の SQLite データベースを作成します。語彙、発音、例文、CEFR レベル、試験タグなどを将来追加できる構造です。

画面デザインは [Flint](https://github.com/sh0ckj0ckey/Flint) のダーク UI を参考に新規実装しています。Flint の辞書データ・ブランド画像は含みません。計算ツールの入口はホーム画面下部の単語帳アイコンの左隣です。

## 開発

Windows 10 19041+／Windows 11、.NET 10 SDK と WinUI 3 開発環境が必要です。

```powershell
dotnet restore ExamLex.slnx
dotnet test tests/ExamLex.Core.Tests/ExamLex.Core.Tests.csproj -c Release
dotnet build src/ExamLex/ExamLex.csproj -c Release -p:Platform=x64
```

試験機関が公表する正式スコアではなく、入力した各セクションのスコアから合計を計算します。詳細は [中文说明](README.md)、[設計](docs/ARCHITECTURE.md)、[計画](docs/ROADMAP.md) を参照してください。
