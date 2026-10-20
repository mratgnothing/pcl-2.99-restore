# Plain Craft Launcher (PCL) 本地功能恢复补丁

第三方修复工具，为指定构建的 **PCL 2.99.0.0** 恢复游戏下载和整合包安装支持。原作者为龙腾猫跃，本项目与官方无隶属关系。

## 下载与使用

1. [下载安装包](https://github.com/mratgnothing/pcl-2.99-restore/releases/latest/download/PCL-2.99-Restore-Setup-1.0.0.zip)，完整解压。
2. 正常退出 PCL，双击 `PCLRestoreSetup.exe`。
3. 选择自己电脑上的 PCL 程序，点击 **校验文件**。
4. 如果游戏目录选错，可指定包含 `versions` 子目录的 Minecraft 根目录；否则留空。
5. 阅读说明并勾选确认，点击 **备份并修复**，完成后正常打开 PCL。

安装包默认将普通自动更新改为仅提示重大漏洞更新，避免覆盖补丁；可以取消勾选。安装器无需 Python、开发工具或管理员权限，所选目录须可写。

## 回退

再次打开安装包，选择同一 PCL 程序，点击 **恢复原版**。备份位于程序旁的 `PCL/LocalRestoreBackups/`。安装后又改过的设置会保留。

## 适用范围

- Windows 10/11、.NET Framework 4.8；目前仅在 Windows 11 本机验证。
- 需要已有匹配的原版 2.99 程序；以 [SHA-256 清单](patch-manifest.json) 为准，不匹配会拒绝修改。
- 安装包不含启动器原版、Minecraft、账号配置或存档。Java、显卡、模组冲突和损坏存档需要单独排查。
- 已通过 16 项安装/恢复测试及界面校验测试；游戏验证覆盖 Minecraft 1.20.1 和最小 Modrinth 整合包安装。尚未逐项验证所有整合包格式或加载器。
- 安装器未做代码签名。手动安装官方更新仍可能覆盖补丁，新版本需要重新适配。

## 源码与署名

[安装器源码与构建](installer-source/README.md) · [启动器补丁源码](launcher-patch-source/README.md) · [详细使用说明](docs/使用说明.txt)

[PCL 官方项目](https://github.com/Meloong-Git/PCL) · [赞助原作者龙腾猫跃](https://meloong.com/afd/a/LTCat) · [上游许可与指南](LICENCE)
