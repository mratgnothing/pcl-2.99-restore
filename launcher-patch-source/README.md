# Plain Craft Launcher (PCL) 本地功能恢复版 2.99.0.0 修改源码

第三方基于 PCL 独立修改；原作者龙腾猫跃，赞助地址 https://meloong.com/afd/a/LTCat 。原作者署名、正版购买劝导及赞助资源保留不变。原 LICENCE 随附，不替换或改写上游许可。

本目录提供八个方法修改的全部实现、同版本整合包模块以及辅助编译/验证工具源码。完整官方公开源码可从 https://github.com/Meloong-Git/PCL/tree/2.99.0.0 或 https://codeload.github.com/Meloong-Git/PCL/zip/refs/tags/2.99.0.0 获取；本分享包没有包含原程序及其私有编译引用。

- RestorePcl.cs：移植两个整合包安装方法，恢复版本列表与相关界面入口，添加第三方标识；验证修改方法白名单、资源和引用，再输出候选 EXE 与审计。
- PrepareReference.cs：仅在编译工作区从使用者自己的原程序中提取依赖，并产生公开化的编译引用。不得把此临时引用当成正式启动器或重新分发。
- prepare_donor.py：将官方源码中 ModModpack.vb 的两处 RestrictMcDownload 替换为 False，并生成 VB 编译响应文件。
- ModModpack.restored.vb：对应生成的完整模块。
- VerifyInstalled.cs：比较原程序和恢复版的全部方法、内嵌资源、字段常量、依赖引用与固定哈希。
- audit.txt、installed-verification.txt：本次候选和已安装程序的验证结果。

## 构建环境和目录

Windows / .NET Framework 4.8，Python 3，Visual Studio Roslyn 的 csc.exe 与 vbc.exe；NuGet Mono.Cecil 0.11.6 和 Microsoft.NETFramework.ReferenceAssemblies.net48 1.0.3。

在独立工作目录放置 prepare_donor.py，官方源码解压到 source/PCL-2.99.0.0，net48 引用包放到 tools/net48，Mono.Cecil.dll 和自行编译的辅助工具放到 tools。C# 辅助工具引用 Mono.Cecil.dll；PrepareReference 另需引用 System.IO.Compression.dll。

1. 编译 PrepareReference.cs、RestorePcl.cs 和 VerifyInstalled.cs。
2. 运行 PrepareReference，参数为：原程序 EXE、reference 输出目录、tools/net48/build/.NETFramework/v4.8。
3. 运行 prepare_donor.py，生成 ModModpack.restored.vb 与 donor.rsp。
4. 使用 vbc.exe @donor.rsp 编译 donor.dll。
5. 运行 RestorePcl，参数为：原程序 EXE、donor.dll、候选 EXE、reference/raw、net48 引用目录。
6. 核验哈希与方法审计，并在隔离游戏目录验证后使用。
7. 如需重新生成安装器补丁，在 installer-source 中使用 build_delta.py，输入原程序和候选程序，再编译安装器。

这里只接受原版 SHA-256 `6b6e1260cb5a1d6a303855f1c60cab3d0bc1aebba7e40a964180188b271e7386`。预期恢复版 SHA-256 `6be10780d1f1471bb5bddefdfbcb3966dbc90cc2f00d2dbce579fa75190932b9`。源码中的原版哈希保护不应用于其他构建；改变保护条件不代表完成兼容性验证。

正式分享安装包使用已经核验的二进制差分，不会在接收者电脑上提取私有编译引用或执行源码构建。源码、差分生成器和补丁清单均保留，便于独立检查。
