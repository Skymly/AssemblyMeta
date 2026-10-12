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

Write-Host '== 清理本地包缓存，避免 0.1.0 命中旧包 =='
$nugetRoot = Join-Path $env:USERPROFILE '.nuget\packages'
foreach ($id in @(
    'assemblymeta',
    'assemblymeta.assemblyinfo',
    'assemblymeta.constants',
    'assemblymeta.project',
    'assemblymeta.metadata',
    'assemblymeta.git'
)) {
    $dir = Join-Path $nugetRoot $id
    if (Test-Path -LiteralPath $dir) {
        Remove-Item -LiteralPath $dir -Recurse -Force
    }
}
Get-ChildItem -Path .\samples -Recurse -Directory -Filter obj -ErrorAction SilentlyContinue | ForEach-Object {
    Remove-Item -LiteralPath $_.FullName -Recurse -Force
}

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

Write-Host '== Git 真实仓库 =='
dotnet build .\samples\Git\Git.csproj -c Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet run --project .\samples\Git\Git.csproj -c Release --no-build -- real
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host '== Git 拉取请求分支 =='
dotnet build .\samples\Git\Git.csproj -c Release -p:GITHUB_REF=refs/pull/15/merge
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet run --project .\samples\Git\Git.csproj -c Release --no-build -- pr
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host '== Git 不发布仓库地址 =='
dotnet build .\samples\Git\Git.csproj -c Release -p:PublishRepositoryUrl=false
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet run --project .\samples\Git\Git.csproj -c Release --no-build -- nourl
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host '== Git 设计时不填充 =='
dotnet build .\samples\Git\Git.csproj -c Release -p:DesignTimeBuild=true
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet run --project .\samples\Git\Git.csproj -c Release --no-build -- pending
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host '== file-based ProjectProperty =='
dotnet run --file .\samples\file-app\App.cs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host '验证通过。'

