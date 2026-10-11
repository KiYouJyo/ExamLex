# ExamLex

A local-first Windows WinUI 3 dictionary shell and score calculator for TOEFL iBT, IELTS and TOEIC Listening & Reading.

**Status: v0.1.0 foundation.** The dictionary deliberately ships with **zero vocabulary entries**. On first launch, ExamLex creates an empty user-local SQLite database with fields for pronunciations, definitions, translations, example sentences, CEFR levels and exam tags. No Flint or ECDICT data is bundled.

The UI takes inspiration from the dark layout of [Flint](https://github.com/sh0ckj0ckey/Flint) (MPL-2.0). This is a separate implementation, with no upstream word data or assets. The calculator button appears in the bottom toolbar immediately **to the left of the glossary button**.

## Build

Windows 10 19041+ or Windows 11, Visual Studio Windows App SDK workload and .NET 10:

```powershell
dotnet restore ExamLex.slnx
dotnet test tests/ExamLex.Core.Tests/ExamLex.Core.Tests.csproj -c Release
dotnet build src/ExamLex/ExamLex.csproj -c Release -p:Platform=x64
```

The TOEFL 2026 and IELTS tools average already scaled section scores; TOEIC L&R adds two scaled sections. This app does not predict scores from raw question counts or issue official score reports.

See [Chinese README](README.md), [Architecture](docs/ARCHITECTURE.md) and [Roadmap](docs/ROADMAP.md). ExamLex is independent of ETS, IELTS test owners and the author of Flint.
