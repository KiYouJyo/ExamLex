# Roadmap

## 0.1.x — foundation
- [x] ExamLex name and WinUI 3 dark dictionary shell
- [x] Empty SQLite dictionary instead of bundled Flint/ECDICT data
- [x] TOEFL 2026 / IELTS score calculator (TOEIC deliberately removed)
- [x] Score-calculator entry left of glossary button
- [x] Source/tests/docs/.github structure and unit tests
- [ ] Windows CI smoke build verification; correct any errors found
- [ ] Dedicated ExamLex application icons and package identities

## 0.2.x — lexical content
- [ ] User-authorized dictionary importer with content license checking
- [ ] Detailed word cards: IPA/audio, example sentences, CEFR, collocations, grammar patterns/valency, exam tags
- [ ] Vocabulary list management, search, spaced review
- [ ] zh-CN / ja-JP / en-US localization resources

## 0.3.x — distribution
- [ ] Signed side-load and Microsoft Store packaging
- [ ] Release workflow, integrity hashes, update channels
- [ ] Performance and screen-reader audits

## 0.1.1 — acceptance feedback
- [x] Logical-size startup window and PerMonitorV2 DPI manifest
- [x] TOEFL/IELTS internal calculator page, approximate IELTS CEFR; TOEIC calculator option removed
- [x] Flint-inspired internal settings with persisted working appearance/search/preferences
- [x] Separate software release and Lexicon SQLite release checks
- [x] Pilot Lexicon release integration with manifest, hash and schema verification
- [ ] Port Flint's global hotkeys, autostart and tray lifecycle safely
- [ ] Inspect text rendering at 100/125/150/200 percent display scaling on physical devices

## 0.1.5 — upstream parity
- [x] Use Flint's complete original search/radio templates and theme resource dictionary
- [x] Port source-derived Flint SettingsPage XAML and preserve unmodified reference files
- [x] Restore Flint default 580 × 386 DIP viewport and original homepage structure
- [ ] Port the upstream system hotkey, startup task, and tray lifecycle without stub toggles
- [ ] Complete manual visual comparison on 100%, 125%, 150%, 200% Windows scales
