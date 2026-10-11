# Changelog

## [0.1.2] - 2026-10-11
- Fix first-window physical pixel sizing via GetDpiForWindow instead of pre-layout XamlRoot.RasterizationScale; clamp to monitor work area and center.
- Compact search/title/footer sizing and ensure that main app footer does not intrude into settings page.
- Replace generic settings mockup with layout port of Flint3 settings: original section order, 96-DIP brand banner, CommunityToolkit SettingsCard/SettingsExpander visual system, expandable About card, and native back toolbar.
- Preserve ExamLex app updater and independent Lexicon database updater; omit original Flint feedback row as requested.
- Include Flint MPL-2.0 notice and full license for adapted settings layout. Unsupported system hotkeys, autostart and tray settings remain disabled pending implementation.
- Produce signed 0.1.2.0 MSIX acceptance artifact.

## [0.1.1] - 2026-10-11
- Smaller DPI-aware startup window; native per-monitor rendering.
- Internal calculator page for TOEFL iBT (2026) and IELTS; approximate IELTS CEFR displayed. TOEIC removed from calculator.
- Flint-inspired settings as a navigable page: appearance, backdrop, definitions, glossary, pronunciation, search, auto-clear.
- Separate app version check and independently updateable canonical ExamLex-Lexicon SQLite database (signed SHA256 manifest verification).
- Package version 0.1.1.0, signed x64 acceptance MSIX.

# Changelog

## [0.1.0] - 2026-10-11

- Initialized an independent Windows WinUI 3 ExamLex project, following Eikura's src/tests/docs/.github structure.
- Re-created Flint-inspired dark home screen with ExamLex branding and the score-calculator entry left of the glossary button.
- Added TOEFL iBT (2026 scale), IELTS and TOEIC L&R section score aggregation and validation.
- Added a clean local SQLite dictionary schema for translations, definitions, example sentences, CEFR and exam tags.
- Included **zero** bundled words, upstream databases, word archives or Flint image assets.
- Added xUnit tests, CI configuration, privacy, contribution, security and roadmap documents.
- Added initial MSIX sideload acceptance packaging with repository-secret signing, standalone package identity, public CER and SHA256 manifest.
