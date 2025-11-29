#!/usr/bin/env pwsh
# Rebuilds EF migrations and databases for Holmes modules.
#
# Usage:
#   ./ef-reset.ps1              # Full reset: drops DB, removes migrations, regenerates migrations, applies them
#   ./ef-reset.ps1 -QuickReset  # Quick reset: drops DB, applies existing migrations (for seed data testing)
#
param(
    [switch]$QuickReset
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$configuration = "Debug"
$startupProject = "src/Holmes.App.Server/Holmes.App.Server.csproj"
$migrationOutputDir = "Migrations"

$modules = @(
    @{
        Name = "Identity"
        Project = "src/Holmes.Identity.Server/Holmes.Identity.Server.csproj"
        StartupProject = "src/Holmes.Identity.Server/Holmes.Identity.Server.csproj"
        Context = "Holmes.Identity.Server.Data.ApplicationDbContext"
        MigrationName = "InitialIdentity"
        MigrationOutputDir = "Migrations/Application"
    },
    @{
        Name = "IdentityServerConfiguration"
        Project = "src/Holmes.Identity.Server/Holmes.Identity.Server.csproj"
        StartupProject = "src/Holmes.Identity.Server/Holmes.Identity.Server.csproj"
        Context = "Duende.IdentityServer.EntityFramework.DbContexts.ConfigurationDbContext"
        MigrationName = "InitialIdentityServerConfiguration"
        MigrationOutputDir = "Migrations/Configuration"
    },
    @{
        Name = "IdentityServerGrants"
        Project = "src/Holmes.Identity.Server/Holmes.Identity.Server.csproj"
        StartupProject = "src/Holmes.Identity.Server/Holmes.Identity.Server.csproj"
        Context = "Duende.IdentityServer.EntityFramework.DbContexts.PersistedGrantDbContext"
        MigrationName = "InitialIdentityServerGrants"
        MigrationOutputDir = "Migrations/Operational"
    },
    @{
        Name = "Core"
        Project = "src/Modules/Core/Holmes.Core.Infrastructure.Sql/Holmes.Core.Infrastructure.Sql.csproj"
        Context = "Holmes.Core.Infrastructure.Sql.CoreDbContext"
        MigrationName = "InitialEventStore"
    },
    @{
        Name = "Users"
        Project = "src/Modules/Users/Holmes.Users.Infrastructure.Sql/Holmes.Users.Infrastructure.Sql.csproj"
        Context = "Holmes.Users.Infrastructure.Sql.UsersDbContext"
        MigrationName = "InitialUsers"
    },
    @{
        Name = "Customers"
        Project = "src/Modules/Customers/Holmes.Customers.Infrastructure.Sql/Holmes.Customers.Infrastructure.Sql.csproj"
        Context = "Holmes.Customers.Infrastructure.Sql.CustomersDbContext"
        MigrationName = "InitialCustomers"
    },
    @{
        Name = "Subjects"
        Project = "src/Modules/Subjects/Holmes.Subjects.Infrastructure.Sql/Holmes.Subjects.Infrastructure.Sql.csproj"
        Context = "Holmes.Subjects.Infrastructure.Sql.SubjectsDbContext"
        MigrationName = "InitialSubjects"
    },
    @{
        Name = "Intake"
        Project = "src/Modules/Intake/Holmes.Intake.Infrastructure.Sql/Holmes.Intake.Infrastructure.Sql.csproj"
        Context = "Holmes.Intake.Infrastructure.Sql.IntakeDbContext"
        MigrationName = "InitialIntake"
    },
    @{
        Name = "Workflow"
        Project = "src/Modules/Workflow/Holmes.Workflow.Infrastructure.Sql/Holmes.Workflow.Infrastructure.Sql.csproj"
        Context = "Holmes.Workflow.Infrastructure.Sql.WorkflowDbContext"
        MigrationName = "InitialWorkflow"
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

    $startup = if ($Module.ContainsKey("StartupProject") -and $Module.StartupProject)
    {
        $Module.StartupProject
    }
    else
    {
        $startupProject
    }

    return @(
        "--project", $Module.Project,
        "--startup-project", $startup,
        "--context", $Module.Context,
        "--configuration", $configuration,
        "--verbose"
    )
}

function Ensure-ModuleMigrations
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
    $name = if ($Module.ContainsKey("MigrationName") -and $Module.MigrationName)
    {
        $Module.MigrationName
    }
    else
    {
        "InitialMigration"
    }
    $outputDir = if ($Module.ContainsKey("MigrationOutputDir") -and $Module.MigrationOutputDir)
    {
        $Module.MigrationOutputDir
    }
    else
    {
        $migrationOutputDir
    }

    $addArgs = @("migrations", "add", $name) + $commonArgs + @("--output-dir", $outputDir)
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

    $moduleOutputDir = if ($Module.ContainsKey("MigrationOutputDir") -and $Module.MigrationOutputDir)
    {
        $Module.MigrationOutputDir
    }
    else
    {
        $migrationOutputDir
    }

    $migrationDir = Join-Path (Split-Path $Module.Project) $moduleOutputDir
    if (Test-Path -Path $migrationDir)
    {
        Write-Host "Removing existing migrations for $( $Module.Name )..." -ForegroundColor Yellow
        Remove-Item -Path $migrationDir -Recurse -Force
    }
}

$databaseDropped = $false
$processedModules = @()

if ($QuickReset)
{
    Write-Host "Quick reset mode: Dropping database and applying existing migrations (no migration regeneration)." -ForegroundColor Green
}
else
{
    Write-Host "Full reset mode: Dropping database, removing migrations, and regenerating from scratch." -ForegroundColor Green
}

foreach ($module in $modules)
{
    if (-not (Test-Path -Path $module.Project))
    {
        Write-Warning "Skipping $( $module.Name ) – project '$( $module.Project )' not found."
        continue
    }

    if (-not $databaseDropped)
    {
        Remove-ModuleDatabase -Module $module
        $databaseDropped = $true
    }
    else
    {
        Write-Host "Database already dropped – skipping drop for $( $module.Name )." -ForegroundColor Yellow
    }

    if ($QuickReset)
    {
        # Quick reset: Just apply existing migrations without regenerating them
        Update-ModuleDatabase -Module $module
    }
    else
    {
        # Full reset: Remove old migrations, create new ones, then apply
        Reset-ModuleMigrations -Module $module
        Ensure-ModuleMigrations -Module $module
        Update-ModuleDatabase -Module $module
    }

    $processedModules += $module.Name
}

if (-not $processedModules)
{
    Write-Warning "No modules were processed. Ensure module project paths are correct."
}
