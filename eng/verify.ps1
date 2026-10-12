$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

Write-Host '== 构建并测试 =='
dotnet test .\AssemblyMeta.slnx -c Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host '== 打包到本地源 =='
Get-ChildItem -LiteralPath .\artifacts\packages -File -ErrorAction SilentlyContinue | ForEach-Object { Remove-Item -LiteralPath $_.FullName -Force }
dotnet pack .\AssemblyMeta.slnx -c Release -o .\artifacts\packages
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host '== 元包样例 =='
dotnet build .\samples\AssemblyMeta.Sample\AssemblyMeta.Sample.csproj -c Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet run --project .\samples\AssemblyMeta.Sample\AssemblyMeta.Sample.csproj -c Release --no-build
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host '== 直接引用 AssemblyInfo =='
dotnet build .\samples\AssemblyInfoOnly\AssemblyInfoOnly.csproj -c Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet run --project .\samples\AssemblyInfoOnly\AssemblyInfoOnly.csproj -c Release --no-build
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host '== GenerateAssemblyInfo=false 仍生成常量 =='
dotnet build .\samples\AssemblyInfoOnly\AssemblyInfoOnly.csproj -c Release -p:GenerateAssemblyInfo=false
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet run --project .\samples\AssemblyInfoOnly\AssemblyInfoOnly.csproj -c Release --no-build
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host '验证通过。'

