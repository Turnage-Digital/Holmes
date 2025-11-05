#!/usr/bin/env pwsh
# Rebuilds EF migrations and databases for Holmes modules.
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$configuration = "Debug"
$startupProject = "src/Holmes.Server/Holmes.Server.csproj"
$migrationName = "Initial"
$migrationOutputDir = "Migrations"

$modules = @(
    @{
        Name = "Core"
        Project = "src/Modules/Core/Holmes.Core.Infrastructure.Sql/Holmes.Core.Infrastructure.Sql.csproj"
        Context = "Holmes.Core.Infrastructure.Sql.CoreDbContext"
    }
)

function Invoke-DotNetEf
{
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    Write-Host "dotnet ef $( $Arguments -join ' ' )" -ForegroundColor Cyan
    & dotnet ef @Arguments

    if ($LASTEXITCODE -ne 0)
    {
        throw "dotnet ef exited with code $LASTEXITCODE."
    }
}

function Get-EfCommonArgs
{
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$Module
    )

    return @(
        "--project", $Module.Project,
        "--startup-project", $startupProject,
        "--context", $Module.Context,
        "--configuration", $configuration,
        "--verbose"
    )
}

function Reset-ModuleMigrations
{
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$Module
    )

    if (-not (Test-Path -Path $Module.Project))
    {
        Write-Warning "Skipping $( $Module.Name ) – project '$( $Module.Project )' not found."
        return
    }

    $commonArgs = Get-EfCommonArgs -Module $Module

    $removeArgs = @("migrations", "remove") + $commonArgs + @("--force")
    Invoke-DotNetEf -Arguments $removeArgs

    $addArgs = @("migrations", "add", $migrationName) + $commonArgs + @("--output-dir", $migrationOutputDir)
    Invoke-DotNetEf -Arguments $addArgs
}

function Update-ModuleDatabase
{
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$Module
    )

    if (-not (Test-Path -Path $Module.Project))
    {
        Write-Warning "Skipping $( $Module.Name ) – project '$( $Module.Project )' not found."
        return
    }

    $commonArgs = Get-EfCommonArgs -Module $Module
    $updateArgs = @("database", "update") + $commonArgs
    Invoke-DotNetEf -Arguments $updateArgs
}

function Remove-ModuleDatabase
{
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$Module
    )

    if (-not (Test-Path -Path $Module.Project))
    {
        Write-Warning "Skipping $( $Module.Name ) – project '$( $Module.Project )' not found."
        return
    }

    $commonArgs = Get-EfCommonArgs -Module $Module
    $dropArgs = @("database", "drop") + $commonArgs + @("--force")
    Invoke-DotNetEf -Arguments $dropArgs
}

$coreModule = $modules | Where-Object { $_.Name -eq "Core" }

if (-not $coreModule)
{
    throw "Core module configuration not found."
}

Remove-ModuleDatabase -Module $coreModule

foreach ($module in $modules)
{
    Reset-ModuleMigrations -Module $module
}

foreach ($module in $modules)
{
    Update-ModuleDatabase -Module $module
}
