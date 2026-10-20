# Plain Craft Launcher (PCL) 本地功能恢复补丁安装器源码

这是第三方独立修复工具，原 PCL 作者为龙腾猫跃。原项目及许可见 resources/LICENCE 和 https://github.com/Meloong-Git/PCL 。本工具无网络下载、遥测或账号操作；只处理使用者明确选择的 EXE 和可选的两项本地设置。

## 编译

Windows，.NET Framework 4.8，Visual Studio 的 Roslyn C# 编译器。编译不需要 NuGet、Python 或 Mono.Cecil，因为 resources 中已提供严格限定输入与输出 SHA-256 的补丁数据。

在 PowerShell 中执行：

```powershell
.\build.ps1
# 如果自动探测未找到编译器：
.\build.ps1 -Compiler '你的 Visual Studio 路径\MSBuild\Current\Bin\Roslyn\csc.exe'
```

输出为 bin/PCLRestoreSetup.exe 及 .exe.config；另生成两个独立测试程序。接收者使用已编译安装器不需要开发环境。build.ps1 不会更改机器上的执行策略；若本机策略阻止执行，应按自己的组织策略处理或手工执行脚本中的编译命令。

`src/PatchEngine.cs` 实现文件校验、受限二进制补丁、原版备份、原子替换、事务回执、失败回退和显式恢复。用目标路径对应的命名互斥量阻止同一路径的并行操作。只接受固定的原版哈希；恢复时只覆盖已知原版/恢复版，不会覆盖其他版本。用户后来改过的配置会保留。

`src/SetupForm.cs` 是 WinForms 中文界面，使用当前用户权限。没有静默安装、自动更新、自动结束进程或启动游戏的命令行入口。

## 重新生成补丁

需要 Python 3、指定 SHA-256 的原版 EXE 和对应的已审计恢复版 EXE。两份 EXE 均没有包含在本分享包中。启动器修改源码在分享包的 launcher-patch-source 中；原版来自使用者自行取得的官方程序。

```powershell
python .\build_delta.py '原版.exe' '恢复版.exe'
```

输出 resources/restore.patch.gz 与 patch-manifest.json。补丁采用自定义 PCLR2991 格式和 GZip，复制原版片段并插入必要字节；输入、输出均校验固定 SHA-256。它只能重建这一个已经验证的版本，不能应用于其他版本。

## 验证

测试仅接受明确传入的原版 EXE 和一个尚不存在的新测试目录，会在该目录创建副本及模拟存档标记，不操作真实游戏目录。

```powershell
.\bin\PCLRestoreTests.exe '原版.exe' '.\new-test-run'
.\bin\PCLRecoveryTests.exe '原版.exe' '.\new-recovery-test-run'
```

tests/RenderSetup.cs 可编译为 WinForms 检查程序，离屏绘制真实界面并测试校验按钮。它要求参数为：已编译安装器、输出 PNG 路径、原版 EXE 路径。

本次验证：13 项安装/回退测试、3 项中断恢复/并发/损坏备份测试，以及原生窗口渲染与校验按钮检查；使用当前 Windows 11 主机。未宣称完成跨机器验证或全面覆盖所有游戏安装格式。

分享时保留完整说明、两部分源码和上游许可。原程序资源、作者署名及购买劝导均保持。安装器及补丁数据未做代码签名。安装包通过本仓库 Releases 提供，转发时请保留源码与许可。
