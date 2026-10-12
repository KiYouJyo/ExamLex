# Changelog

## [0.1.8] - 2026-10-12
- Add owner-supplied HiDPI tile variants and unplated taskbar icon assets so the icon is crisp and transparent.
- Use themed Fluent Acrylic dropdown surfaces with strong tint and system fallback, instead of opaque gray panels or see-through overlays.
- Remove all ContentDialog and create-glossary Flyout calls. Show errors and update states inline in Flint's existing About cards; spinner is inside each check-update button.
- Download and validate official signed ExamLex MSIX releases, then offer a Restart & Update button. Lexicon updates also finish with a restart button.
- Implement per-user startup registration, global hotkeys, full/compact activation, close-to-tray with Show/Exit menu, installed TTS voice selection and volume.
- Persist all existing Flint settings, including glossary switch, English definitions, search style and theme.
- CI still verifies signed MSIX identity, install/uninstall, icon assets and unit tests. Actual theme/transparency appearance requires Windows desktop verification.

## [0.1.7] - 2026-10-12
- Remove the overlapping home header from all internal Flint pages; give each page its own draggable header.
- Wire the calculator's footer back button to return to the dictionary homepage, and restore home title-bar binding on return.
- Fix score-calculator footer rule spanning only 2 of 3 columns (pin column was omitted).
- Keep selection dropdowns completely opaque. Modal dialogs now apply a Mica system backdrop with a tinted translucent content panel; solid-color fallbacks are controlled by Windows.
- Make packaged Start tile background transparent. The owner-provided v3 PNG/ICO artwork was already alpha-transparent; square backdrop came from the MSIX VisualElements BackgroundColor setting.
- Signed package version 0.1.7.0; full Windows CI and MSIX install/uninstall acceptance required before delivery.

## [0.1.6] - 2026-10-12
- Fix duplicate window/header back arrow on every internal page: original Flint title is only shown for the dictionary homepage, and each internal page owns its own title.
- Add the Flint-style fixed bottom back/navigation bar to TOEFL / IELTS score calculator so content remains scrollable without hiding the return control.
- Replace generated EL placeholder icon tiles with the owner-provided `ExamLex_Complete_Icon_Assets_v3.zip` image masters, including MSIX icons and the .ico for the executable/titlebar/taskbar. Source archive remains tracked; workflows install its assets before build, without regenerating arbitrary artwork.
- Use Windows App SDK system Mica backdrops on native ComboBox Popup, flyouts and ContentDialog surfaces; themed semi-translucent brushes give OS fallback.
- Each page keeps its independent original Flint footer; one pin button is placed in the last column of each footer. Other footer actions stay to its left. Pin state is synchronized across pages through WindowPinService, without a floating overlay.
- Version 0.1.6.0; packaged x64 MSIX continues to use repository signing certificate.

## [0.1.5] - 2026-10-11
- Port Flint 3.4.3's SettingsPage.xaml structure and all original control templates; replace only branded content and wire ExamLex services.
- Vendor original Flint ThemeDictionary and CustomControlStyle resources (Modern/Round/Classic search, settings radio buttons) under MPL-2.0 with copyright attribution.
- Restore original Flint's 580 × 386 DIP initial window dimensions, persist resizing and use HWND DPI to derive physical pixel sizes.
- Restore Flint's 38-DIP title row, auto-sized search textbox, original footer icons and 72-DIP logo placeholder (brand-swapped).
- Preserve TOEFL/IELTS internal calculator entry beside Glossary, local vocabulary services, and independent app/database update actions.
- Version synchronized to 0.1.5.0.
- Replace custom pop-up glossary with an internal Flint-derived GlossariesPage and Flint-shaped GlossaryWordsPage, with the original tab selector, collection tile layout and toolbar.


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
