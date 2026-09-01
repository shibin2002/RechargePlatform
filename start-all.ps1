# ============================================================================
# start-all.ps1: One-Click Local Development Startup Script
# ============================================================================

$root = $PSScriptRoot
$dotnet = "C:\Users\shibi\AppData\Local\Microsoft\dotnet\dotnet.exe"

Write-Host "=======================================================" -ForegroundColor Cyan
Write-Host " PayTelecom POS - Launching Complete Platform" -ForegroundColor Cyan
Write-Host "=======================================================" -ForegroundColor Cyan

# 1. Start Mock Telecom Provider API (Port 5005)
Write-Host "`n[1/3] Starting Mock Telecom Provider API on port 5005..." -ForegroundColor Yellow
$providerProc = Start-Process $dotnet -ArgumentList "run --project `"$root\src\MockProviderApi\MockProviderApi.csproj`"" -PassThru -NoNewWindow

# 2. Start Main Recharge API (Port 5000)
Write-Host "[2/3] Starting Main Telecom Recharge API on port 5000..." -ForegroundColor Yellow
$apiProc = Start-Process $dotnet -ArgumentList "run --project `"$root\src\RechargeApi\RechargeApi.csproj`"" -PassThru -NoNewWindow

# 3. Start Frontend (Port 5173)
Write-Host "[3/3] Starting React + TypeScript POS Frontend on port 5173..." -ForegroundColor Yellow
$frontendProc = Start-Process cmd -ArgumentList "/c npm --prefix `"$root\frontend`" run dev" -PassThru -NoNewWindow

Write-Host "`n=======================================================" -ForegroundColor Green
Write-Host " [ONLINE] All Services Successfully Launched!" -ForegroundColor Green
Write-Host "=======================================================" -ForegroundColor Green
Write-Host " - Frontend POS UI:   http://localhost:5173" -ForegroundColor White
Write-Host " - Main Recharge API: http://localhost:5000/swagger" -ForegroundColor White
Write-Host " - Mock Provider API: http://localhost:5005/swagger" -ForegroundColor White
Write-Host " - API Key Header:    X-Api-Key: pos_super_secret_api_key_2026" -ForegroundColor White
Write-Host "`nPress Ctrl+C to stop all running services..." -ForegroundColor Gray

# Keep script open and handle shutdown
try {
    while ($true) {
        Start-Sleep -Seconds 2
    }
} finally {
    Write-Host "`nStopping background processes..." -ForegroundColor Yellow
    Stop-Process -Id $providerProc.Id -ErrorAction SilentlyContinue
    Stop-Process -Id $apiProc.Id -ErrorAction SilentlyContinue
    Stop-Process -Id $frontendProc.Id -ErrorAction SilentlyContinue
    Write-Host "All processes stopped." -ForegroundColor Green
}
