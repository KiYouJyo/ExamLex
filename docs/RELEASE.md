# ExamLex release engineering / 发布流程

## 0.1.1 MSIX acceptance / 验收包

The **Signed MSIX acceptance** GitHub Action builds and signs `ExamLex_0.1.1.0_x64.msix` with the repository-configured PFX signing certificate; it also exports a **public-only** CER, SHA256 checksums, and installation instructions as one GitHub Actions artifact.

- Identity: `KiYouJyo.ExamLex`, independent from Eikura/Flint.
- Version: `0.1.1.0`; Windows x64, Windows 10 19041+ / Windows 11.
- Certificate must contain a private key and include a valid code-signing identity. Workflow updates manifest Publisher from its certificate Subject before packaging, then verifies the signed MSIX.
- Expected repository Action Secrets: `GH_RELEASE_CERTIFICATE_BASE64` and `GH_RELEASE_CERTIFICATE_PASSWORD` (Eikura convention); ExamLex-specific `EXAMLEX_CERTIFICATE_BASE64` and `EXAMLEX_CERTIFICATE_PASSWORD` can substitute.
- Never commit certificates, passwords or production PFX files. Only the exported public certificate goes to the artifact.
- Initial x64 tiles are generated from an original monogram by `scripts/New-PackageAssets.ps1`. These are temporary acceptance visuals, **not Flint artwork**.
- Download the artifact from [Signed MSIX acceptance workflow](https://github.com/KiYouJyo/ExamLex/actions/workflows/msix-acceptance.yml). Artifact retention: 30 days. See [installation instructions](../packaging/INSTALL.md).
- This workflow doesn't publish to Microsoft Store or create a GitHub Release automatically.

The regular CI workflow continues to run xUnit tests and an unpackaged-style WinUI compilation smoke check, while signed packaging is handled in a dedicated, secret-enabled workflow.

## Next

Build out reliable installation/launch smoke tests on Windows, better original ExamLex graphics, and Store-compliant publisher identity before publishing an official release.

### v0.1.1 acceptance checks

The app now uses internal settings and score pages and an independently installable Lexicon pilot database; CI verifies MSIX registration but the final appearance and interactions require a user desktop smoke test. The installer documentation is updated for this release.
