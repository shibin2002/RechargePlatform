# ============================================================================
# setup-database.ps1: One-Click SQL Server Database Initialization
# ============================================================================

param (
    [string]$ServerInstance = "localhost"
)

Write-Host "=======================================================" -ForegroundColor Cyan
Write-Host " Telecom Recharge Platform - Database Initialization" -ForegroundColor Cyan
Write-Host "=======================================================" -ForegroundColor Cyan

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$dbDir = Join-Path $scriptDir "database"

if (-not (Test-Path $dbDir)) {
    $dbDir = Join-Path (Split-Path -Parent $scriptDir) "database"
}

$files = @(
    (Join-Path $dbDir "01_schema.sql"),
    (Join-Path $dbDir "02_stored_procedures.sql"),
    (Join-Path $dbDir "03_indexes_constraints.sql"),
    (Join-Path $dbDir "04_seed_data.sql")
)

foreach ($f in $files) {
    if (Test-Path $f) {
        Write-Host "Executing script: $(Split-Path -Leaf $f)..." -ForegroundColor Yellow
        sqlcmd -S $ServerInstance -E -C -i "$f"
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Error executing $f" -ForegroundColor Red
            exit $LASTEXITCODE
        }
    } else {
        Write-Host "File not found: $f" -ForegroundColor Red
    }
}

Write-Host "`n[SUCCESS] RechargeDb database and all stored procedures successfully initialized!" -ForegroundColor Green
