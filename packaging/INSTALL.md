# ExamLex 0.1.4 MSIX 验收版安装说明

此安装包采用 `KiYouJyo.ExamLex` 独立身份，版本 `0.1.4.0`，适用于 Windows 10（19041+）及 Windows 11 x64。

1. 在 [GitHub Actions](https://github.com/KiYouJyo/ExamLex/actions/workflows/msix-acceptance.yml) 下载 **ExamLex-v0.1.4-MSIX-acceptance** 并解压。
2. 双击 `ExamLex_0.1.4.0_x64.msix` 安装。
3. 如果 Windows 报告证书不受信任，确认签名证书确实来自此项目后，右键随包的 `ExamLex_0.1.4_public.cer` → 安装证书 → 本地计算机 → 将证书放入 **受信任人 (Trusted People)** 存储，再重试安装。不要关闭系统安全保护；如果不信任证书，请不要安装。
4. 打开 ExamLex；根据实际窗口 DPI 和工作区选择宽高，不再压成极小窗口。点击底栏生词本左侧算分器图标，切换 TOEFL iBT / IELTS，检查雅思 CEFR 近似对应值。
5. 点击底栏设置进入 Flint 原版 SettingsCard 风格的内页，检查原版设置分组、关于展开卡片和底部返回按钮。
6. 在 **设置 → 关于 → ExamLex-Lexicon → 检查更新** 中获取独立 A1 试验词库（不是完整 Oxford A1）；确认下载后返回主页面查词。
7. **软件检查更新** 与 **数据库检查更新** 是两个独立按钮。软件更新只检查 ExamLex GitHub Releases，不会自行安装未知发布包。

词库测试版是 ExamLex-Lexicon 项目的独立 SQLite 发布物，启用网络下载时须保证 GitHub 可访问，并通过 SHA256 与数据库结构完整性验证后安装。签名私钥和密码不包含在包内，仅提供公开证书。

SHA256 校验：`Get-FileHash .\ExamLex_0.1.4.0_x64.msix -Algorithm SHA256` 与 `SHA256SUMS.txt` 比较。

已知限制：快捷唤醒、开机自启、系统托盘尚未移植。安装/卸载在 CI 中通过，不代表所有 Windows 缩放配置均已视觉验收。
