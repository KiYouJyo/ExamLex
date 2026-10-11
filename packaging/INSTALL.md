# ExamLex 0.1.0 MSIX 验收版安装说明

此验收包采用 `KiYouJyo.ExamLex` 独立包身份，版本 `0.1.0.0`，x64 架构。用于 Windows 10（19041+）/ Windows 11。

1. 从 Actions 的 **ExamLex-v0.1.0-MSIX-acceptance** Artifact 下载并解压。
2. 双击 `ExamLex_0.1.0.0_x64.msix`，在 Windows 应用安装程序中安装。
3. 如果因证书信任问题而拒绝安装：打开随包提供的 `.cer` 文件，选择「安装证书」→「本地计算机」（可能要求管理员）→「将所有的证书放入下列存储」→ **受信任人 (Trusted People)**；再双击 MSIX。只在信任证书持有者时这么做，测试结束后可按需移除证书。
4. 打开 ExamLex；检查首页搜索空状态、底部生词本左侧的计算器入口、TOEFL/IELTS/TOEIC 三种算法和窗口置顶。
5. 验收词库应该为空：没有预装 Flint/ECDICT 词条。

MSIX 使用仓库 Secrets 提供的 PFX 证书签名，证书密码及私钥不会打包；只分发公开 `.cer` 以供系统建立信任。

这是一版验收包，不是 Microsoft Store 的已审核包。若遇到 Windows App Runtime 依赖，请根据提示安装与 Windows App SDK 1.8 匹配的 Microsoft Windows App Runtime；构建配置当前优先采用自包含运行时。

安装提示：`Get-FileHash .\ExamLex_0.1.0.0_x64.msix -Algorithm SHA256` 可核对 `SHA256SUMS.txt`。不要关闭 Windows 安全检查。
