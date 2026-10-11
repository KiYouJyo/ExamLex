# Privacy / 隐私声明

ExamLex 0.1.1 does not require an account and does not upload search terms, saved vocabulary, score inputs or identifiable user data. It saves settings in `%LOCALAPPDATA%\ExamLex\appsettings.json`, favorites in `glossary.json`, and an independently installed SQLite dictionary in `lexicon-current.db`. Calculator scores are processed in memory.

When a user explicitly chooses **检查更新**, the app makes HTTPS requests to GitHub to read the public latest-release metadata. Confirming a database update downloads a versioned SQLite release from `KiYouJyo/ExamLex-Lexicon`. The download is subject to SHA256 and SQLite integrity checks. GitHub will receive ordinary connection metadata such as IP address as part of this request. The app does not send your search history to GitHub.

No account sign-in, analytics SDK or advertising SDK is included in this version. The app and dictionary are updated separately, and the software never silently installs a GitHub release.
