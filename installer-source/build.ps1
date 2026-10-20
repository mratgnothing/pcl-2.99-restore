param([string]$Compiler)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$projectRoot=$PSScriptRoot
if (-not $Compiler) {
    $vswhere=Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path -LiteralPath $vswhere) {
        $installation=& $vswhere -latest -products '*' -property installationPath
        if ($installation) { $Compiler=Join-Path $installation 'MSBuild\Current\Bin\Roslyn\csc.exe' }
    }
}
if (-not $Compiler -or -not (Test-Path -LiteralPath $Compiler)) { throw 'Specify -Compiler with a Roslyn csc.exe supporting .NET Framework.' }
$buildOutput=Join-Path $projectRoot 'bin'
New-Item -ItemType Directory -Path $buildOutput -Force | Out-Null
$common=@('/nologo','/optimize+','/platform:anycpu','/reference:System.Core.dll','/reference:System.Xml.dll',('/resource:'+(Join-Path $projectRoot 'resources\restore.patch.gz')+',PclRestore.restore.patch.gz'))
$sources=@((Join-Path $projectRoot 'src\PatchEngine.cs'),(Join-Path $projectRoot 'src\SetupForm.cs'))
& $Compiler @common /target:winexe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll ('/win32manifest:'+(Join-Path $projectRoot 'src\setup.manifest')) ('/out:'+(Join-Path $buildOutput 'PCLRestoreSetup.exe')) ('/resource:'+(Join-Path $projectRoot 'resources\README.txt')+',PclRestore.README.txt') ('/resource:'+(Join-Path $projectRoot 'resources\LICENCE')+',PclRestore.LICENCE') @sources
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
Copy-Item -LiteralPath (Join-Path $projectRoot 'src\PCLRestoreSetup.exe.config') -Destination $buildOutput -Force
& $Compiler @common /target:exe ('/out:'+(Join-Path $buildOutput 'PCLRestoreTests.exe')) (Join-Path $projectRoot 'src\PatchEngine.cs') (Join-Path $projectRoot 'tests\InstallerTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Test runner compilation failed.' }
& $Compiler @common /target:exe ('/out:'+(Join-Path $buildOutput 'PCLRecoveryTests.exe')) (Join-Path $projectRoot 'src\PatchEngine.cs') (Join-Path $projectRoot 'tests\RecoveryTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Recovery test runner compilation failed.' }
Write-Output ('Built '+$buildOutput)
