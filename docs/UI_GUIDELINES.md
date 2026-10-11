# UI guidelines / 界面规范

ExamLex's home screen is an independent WinUI 3 dark recreation inspired by the open-source Flint app. Key blocks:

- Brand heading: `ExamLex` at the top left.
- Search: full-width high-contrast rounded textbox below the title.
- Content: scrollable result cards (word + phonetics, translations/definitions, examples, CEFR, exam tags).
- Footer: **settings | flex spacer | calculator | glossary | pin**.

The `CalculatorButton` must remain immediately **left of `GlossaryButton`**. This is the red-marked slot in the original reference screenshot (at approximately x=875, y=700 on a 1122×746 image). Use a calculator glyph and a tooltip, not a red rectangle in production.

Dictionary empty state is intentional. Do not show fake example words or placeholder definitions as real dictionary records. Ensure controls support keyboard and accessible names.
