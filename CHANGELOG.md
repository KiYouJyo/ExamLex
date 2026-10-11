# Changelog

## [0.1.4] - 2026-10-11
- Restore the agreed sense-first entry structure in the Windows UI: part-of-speech headers, Chinese/Japanese/English fields for each sense, pronunciation, inflections, synonyms, antonyms, valency, phrases/collocations, examples, CEFR and exam tags.
- Reserve each section with "待补充" when the installed lexicon lacks that data, instead of hiding the content area.
- Read related translations, examples, expressions, and semantic relations by sense ID; preserve all imported senses and associated example translations without the former 5-definition/3-example display caps.
- Fix developer acceptance versioning: ship a separately installable 0.1.4.0 signed MSIX; users must update the app itself to see the new entry layout.
- Lexicon data updates remain independent; this app build supports existing canonical SQLite schema v1.

## [0.1.3] - 2026-10-11
- Restore Flint-proportioned 660 × 470 effective-pixel launch window (AppWindow converts through actual HWND DPI).
- Restore Flint's 38-DIP title header, 54-DIP search row, 52-DIP bottom toolbar, Times New Roman title and compact 17-DIP controls.
- Neutral dark palette replaces mismatched green tint; remove oversized central placeholder text.
- Settings in-page top header now matches Flint's “Settings”; remove extra top back arrow and restore footer-only arrow.
- Reset settings scroll position to top when opened.

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
