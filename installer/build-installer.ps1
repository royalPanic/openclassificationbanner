$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repositoryRoot "OpenClassBanner\OpenClassBanner.csproj"
$installerPath = Join-Path $PSScriptRoot "OpenClassBanner.iss"
$publishDirectory = Join-Path $repositoryRoot "artifacts\publish\win-x64"
$outputDirectory = Join-Path $repositoryRoot "artifacts\installer"

[xml]$project = Get-Content -Path $projectPath -Raw
$version = @($project.Project.PropertyGroup | ForEach-Object { $_.Version } | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })[0]
if ([string]::IsNullOrWhiteSpace([string]$version)) {
	throw "The project must define a Version property."
}

& dotnet publish $projectPath --configuration Release --runtime win-x64 --self-contained true --output $publishDirectory
if ($LASTEXITCODE -ne 0) {
	throw "dotnet publish failed with exit code $LASTEXITCODE."
}
if (-not (Test-Path (Join-Path $publishDirectory "OpenClassBanner.exe"))) {
	throw "The self-contained publish did not produce OpenClassBanner.exe."
}

$compilerCandidates = @()
$installRoots = @(${env:ProgramFiles(x86)}, $env:ProgramFiles) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
foreach ($installRoot in $installRoots) {
	if (-not (Test-Path $installRoot)) {
		continue
	}

	foreach ($installDirectory in Get-ChildItem -Path $installRoot -Directory -Filter "Inno Setup *" -ErrorAction SilentlyContinue) {
		$versionMatch = [regex]::Match($installDirectory.Name, '^Inno Setup\s+(\d+(?:\.\d+)*)$')
		$compilerPath = Join-Path $installDirectory.FullName "ISCC.exe"
		if ($versionMatch.Success -and (Test-Path $compilerPath)) {
			$compilerCandidates += [pscustomobject]@{
				Version = [version]$versionMatch.Groups[1].Value
				Path = $compilerPath
			}
		}
	}
}

$pathCompiler = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
if ($null -ne $pathCompiler) {
	$productVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($pathCompiler.Source).ProductVersion
	$versionMatch = [regex]::Match($productVersion, '\d+(?:\.\d+){1,3}')
	if ($versionMatch.Success) {
		$compilerCandidates += [pscustomobject]@{
			Version = [version]$versionMatch.Value
			Path = $pathCompiler.Source
		}
	}
}

if ($compilerCandidates.Count -eq 0) {
	throw "Inno Setup 6 or 7 is required to build the installer. Install either version or add a versioned ISCC.exe to PATH."
}

$selectedCompiler = $compilerCandidates | Sort-Object Version -Descending | Select-Object -First 1
Write-Host "Using Inno Setup $($selectedCompiler.Version): $($selectedCompiler.Path)"
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
& $selectedCompiler.Path "/DAppVersion=$version" "/DPublishDir=$publishDirectory" $installerPath
if ($LASTEXITCODE -ne 0) {
	throw "Inno Setup compilation failed with exit code $LASTEXITCODE."
}
