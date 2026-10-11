# Third-party notices

## Flint (sh0ckj0ckey) — Mozilla Public License 2.0

Source: https://github.com/sh0ckj0ckey/Flint

The settings page in `src/ExamLex/Views/SettingsPage.xaml` adapts the layout and control arrangement of Flint3's `Flint3/Views/SettingsPage.xaml`: card/expander arrangement, categories and navigation. Adaptations include independent ExamLex branding, settings persistence, removed author feedback links, and ExamLex/ExamLex-Lexicon update commands.

Those modifications of the Flint settings layout are provided under the Mozilla Public License Version 2.0; a copy is included at `LICENSES/MPL-2.0.txt`. The full source for the modified settings page is available in this repository. ExamLex's separately authored components remain under the root MIT license. No Flint logo artwork or dictionary databases are included.

CommunityToolkit for WinUI is used for its `SettingsCard` and `SettingsExpander` controls, as Flint3 does.

Upstream source XAML and styles are preserved unmodified under `upstream-flint/Flint3/` to make visual parity auditing possible. Their adapted working copies in `src/ExamLex/Views/SettingsPage.xaml` and `src/ExamLex/ThemeResource/` retain MPL-2.0 license, see `LICENSES/MPL-2.0.txt`.

The source-derived glossary navigation UI is based on Flint3's `GlossariesPage.xaml` and `GlossaryWordsPage.xaml`; original copies are under `upstream-flint/Flint3/Views/`. Their ExamLex adaptations are likewise MPL-2.0.
