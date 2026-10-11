# Architecture

ExamLex follows the separation used in Eikura: `src/` for app and domain projects, `tests/` for unit tests, `docs/` for design notes, and `.github/workflows/` for CI.

- `ExamLex`: WinUI 3 front end (`MainWindow`), localized Chinese-first labels, calculator dialog, persistent glossary and SQLite dictionary service.
- `ExamLex.Core`: pure .NET score aggregation and dictionary models; no WinUI dependencies.
- `ExamLex.Core.Tests`: xUnit checks for TOEFL half-step rounding, IELTS quarter rules, TOEIC addition, and invalid section scores.
- Database: per-user SQLite under LocalAppData. The schema includes `word`, `phonetic`, `translation`, `definition`, `cefr`, `examples_json`, and `exam_tags_json`. No records or licensed corpus are distributed.
- Calculator inputs are never saved; glossary favorites are saved as user-local JSON.

Future: add an abstract licensed-data importer and database migrations, a richer lexical schema for part-of-speech/valency/collocations, localization resources (zh-CN/ja-JP/en-US), signing and MSIX release pipeline.
