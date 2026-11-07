param(
    [string]$Compiler = (Join-Path $PSScriptRoot '..\work\build-tools\innosetup-7.1.0\tools\ISCC.exe')
)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$appVersion = ([xml](Get-Content -Raw (Join-Path $projectRoot 'src\ZuzongNotch\V-Notch.csproj'))).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
$payloadRoot = Join-Path $projectRoot "work\setup-payload-$appVersion"
if (-not (Test-Path -LiteralPath $Compiler)) { throw 'Install Inno Setup 7 or pass -Compiler with the full path to ISCC.exe.' }
& dotnet publish (Join-Path $projectRoot 'src\ZuzongNotch\V-Notch.csproj') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:SkipNativeCompile=true -p:SkipShaderCompile=true -o $payloadRoot -v quiet
if ($LASTEXITCODE -ne 0) { throw 'Application publish failed.' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'resources\LITE_README.md') -Destination (Join-Path $payloadRoot 'HUONG_DAN.md')
$bridgeRoot = Join-Path $payloadRoot 'Bridge'
New-Item -ItemType Directory -Force -Path $bridgeRoot | Out-Null
foreach ($bridgeFile in @('ZuzongNotify.exe', 'install-codex-notify.py')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot "resources\Bridge\$bridgeFile") -Destination $bridgeRoot
}
& $Compiler /Qp "/DAppVersion=$appVersion" "/DPayloadDir=$payloadRoot" (Join-Path $PSScriptRoot 'ZuzongDynamic.iss')
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
$setupFile = Join-Path $projectRoot "outputs\ZuzongDynamic-Setup-$appVersion-x64.exe"
$setupDigest = Get-FileHash -LiteralPath $setupFile -Algorithm SHA256
"$($setupDigest.Hash.ToLowerInvariant())  $([IO.Path]::GetFileName($setupFile))" | Set-Content -Encoding ASCII -LiteralPath "$setupFile.sha256"
Write-Output $setupFile
