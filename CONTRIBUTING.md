# Contributing / 贡献指南

1. Open an issue to describe the proposed feature or bug.
2. Use a feature branch, keep score algorithms in `ExamLex.Core`, and add or update xUnit tests.
3. Preserve the dark desktop UX and put user data under LocalAppData, never commit dictionary databases or archives.
4. Confirm rights and attribution before importing any third-party lexical content or example sentences.
5. Run `dotnet test tests/ExamLex.Core.Tests/ExamLex.Core.Tests.csproj` and a Windows x64 WinUI build before submitting a PR.

ExamLex is an independent reimplementation inspired by Flint's layout, not a derivative distribution of its dictionary content.
