[CmdletBinding()]
Param(
    [string]$Script = "build.cake",
    [string]$Target,
    [string]$Configuration,
    [ValidateSet("Quiet", "Minimal", "Normal", "Verbose", "Diagnostic")]
    [string]$Verbosity='Verbose',
    [switch]$ShowDescription,
    [Alias("WhatIf", "Noop")]
    [switch]$DryRun,
    [switch]$Experimental,
    [switch]$SkipToolPackageRestore,
    [Parameter(Position=0,Mandatory=$false,ValueFromRemainingArguments=$true)]
    [string[]]$ScriptArgs
)

if (-not (Get-Command -Name 'dotnet' -CommandType Application -ErrorAction SilentlyContinue)) {
    throw "This project requires dotnet core but it could not be found. Please install dotnet core and ensure it is available on your PATH"
}

function Get-EnvironmentVariableOrDefault {
    Param(
        [string] $Name,
        [string] $Default
    )

    $result = [System.Environment]::GetEnvironmentVariable($Name)
    if(-not $result) {
        return $Default
    } else {
        return $result
    }
}

$ToolsDir = Get-EnvironmentVariableOrDefault -Name 'TOOLS_DIR' -Default (Join-Path -Path $PSScriptRoot -ChildPath 'tools')
$CakeVersion = Get-EnvironmentVariableOrDefault -Name 'CAKE_VERSION' -Default '0.29.0'
$CakeNetcoreappVersion = Get-EnvironmentVariableOrDefault -Name 'CAKE_NETCOREAPP_VERSION' -Default '2.0'

if (-not (Test-Path -Path $ToolsDir)) {
    New-Item -ItemType Directory -Path $ToolsDir | Out-Null
}

$CakeDLL = Get-ChildItem -Path $ToolsDir -Recurse -Filter 'Cake.dll' | Select-Object -ExpandProperty FullName

###########################################################################
# INSTALL CAKE
###########################################################################

if (-not $CakeDLL) {
    Write-Output "Installing Cake $CakeVersion"

    $ToolsProj = Join-Path -Path $ToolsDir -ChildPath 'cake.csproj'
    "<Project Sdk=`"Microsoft.NET.Sdk`"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>netcoreapp$CakeNetcoreappVersion</TargetFramework></PropertyGroup></Project>" | Set-Content -Path $ToolsProj
    Start-Process -FilePath 'dotnet' -ArgumentList @('add', $ToolsProj, 'package', 'Cake.CoreCLR', '-v', $CakeVersion, '--package-directory', $ToolsDir) -NoNewWindow -Wait -ErrorAction Stop

    $CakeDLL = Get-ChildItem -Path $ToolsDir -Recurse -Filter 'Cake.dll' | Select-Object -ExpandProperty FullName

    if (-not $CakeDLL) {
        throw "Failed to install Cake $CakeVersion"
    }
}

###########################################################################
# RUN BUILD SCRIPT
###########################################################################

$cakeArguments = @("$Script");
if ($Target) { $cakeArguments += "-target=$Target" }
if ($Configuration) { $cakeArguments += "-configuration=$Configuration" }
if ($Verbosity) { $cakeArguments += "-verbosity=$Verbosity" }
if ($ShowDescription) { $cakeArguments += "-showdescription" }
if ($DryRun) { $cakeArguments += "-dryrun" }
if ($Experimental) { $cakeArguments += "-experimental" }
$cakeArguments += $ScriptArgs

Write-Output "Running build script..."
Invoke-Expression "& dotnet `"$CakeDLL`" $cakeArguments"
exit $LASTEXITCODE
