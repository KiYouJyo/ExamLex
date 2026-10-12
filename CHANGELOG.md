# Changelog

## [0.2.0] - 2026-10-12

- First stable, signed GitHub release with standalone x64 MSIX and SHA256 checksums.
- Eikura-style README branding, application icon, release/CI badges and installation section.
- Make the Lexicon database refresh button match the application's refresh icon.
- Bundle the latest *published* ExamLex-Lexicon canonical SQLite database in every build, with SHA256, schema and entry-count checks. Seed on first launch or upgrade only when the bundled version is newer, preserving newer downloaded databases.
- Use Windows App SDK AppInstance to redirect all subsequent application launches into the existing visible or tray-hidden window.
- Automatic GitHub Release publishing only after signed MSIX CI, installation and verification succeed.

