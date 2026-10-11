# Release process (planned)

The current 0.1.0 repository targets Windows WinUI 3 x64 builds without a signed package. Before the first distribution:
1. Create original ExamLex app/store logos and a package identity; never reuse Flint logo assets.
2. Select MSIX or unpackaged installer strategy and ensure Windows App SDK runtime dependencies are handled.
3. Add certificate signing through secure CI secrets; never commit PFX/keys.
4. Produce checksums, release notes and a smoke-tested installer.
5. Run score unit tests and WinUI Windows CI checks before tagging a release.

This document is not a claim that binaries or Microsoft Store releases already exist.
