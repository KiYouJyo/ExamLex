# Architecture — ExamLex 0.1.1

ExamLex adopts Eikura's repository organization: `src/`, `tests/`, `docs/`, `.github/workflows/`.

- `ExamLex`: WinUI 3 desktop shell with fixed bottom navigation; `MainWindow` hosts dictionary home plus internal `ScoreCalculatorPage` and `SettingsPage`, not pop-up dialogs for these two major features.
- `AppSettings`: JSON settings persisted under `%LOCALAPPDATA%\ExamLex\appsettings.json` and applied to WinUI at runtime.
- `ExamLex.Core`: section-score math independent of WinUI; TOEFL 2026 and IELTS with CEFR approximate mapping.
- `DictionaryStore`: directly reads the canonical multi-table SQLite **schema v1** from the separately maintained [ExamLex-Lexicon](https://github.com/KiYouJyo/ExamLex-Lexicon). No conversion or bundled ECDICT data.
- `LexiconUpdateService`: uses GitHub release metadata and published `lexicon-manifest.json`; checks version, SHA256, size, SQLite integrity, user_version and required tables, then atomically installs `%LOCALAPPDATA%\ExamLex\lexicon-current.db`.
- `AppUpdateService`: checks the software's own GitHub release feed independently; does not download installers automatically.
- Local favorites are maintained at `%LOCALAPPDATA%\ExamLex\glossary.json`.
- CI: xUnit and Windows MSBuild; dedicated signed MSIX workflow verifies certificate, package identity and Windows installation.

The initial app ships without a dictionary. A small **A1 pilot** may be installed separately from the Lexicon GitHub release; do not represent this as the full Oxford A1 vocabulary. Provenance remains under ExamLex-Lexicon's source policy.

System hotkeys/startup and tray behavior have not yet been ported; these are shown as unavailable, never as functional switches.
