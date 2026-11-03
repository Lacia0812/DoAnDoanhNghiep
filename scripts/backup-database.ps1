[CmdletBinding()]
param(
    [string]$ServerInstance = "localhost",
    [string]$Database = "GiaPhuWaterDeliveryDb",
    [string]$OutputDirectory = (Join-Path $PSScriptRoot "..\backups")
)

$ErrorActionPreference = "Stop"
if ($Database -notmatch '^[A-Za-z0-9_-]+$') {
    throw "Tên database chỉ được chứa chữ, số, gạch dưới hoặc gạch ngang."
}
if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    throw "Không tìm thấy sqlcmd. Hãy cài SQL Server Command Line Utilities."
}

$resolvedOutput = [System.IO.Path]::GetFullPath($OutputDirectory)
[System.IO.Directory]::CreateDirectory($resolvedOutput) | Out-Null
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupPath = Join-Path $resolvedOutput "$Database-$timestamp.bak"
$escapedPath = $backupPath.Replace("'", "''")
$query = "BACKUP DATABASE [$Database] TO DISK = N'$escapedPath' WITH COPY_ONLY, CHECKSUM, INIT, STATS = 10; RESTORE VERIFYONLY FROM DISK = N'$escapedPath' WITH CHECKSUM;"

Write-Host "Đang sao lưu $Database trên $ServerInstance..."
& sqlcmd -S $ServerInstance -E -b -Q $query
if ($LASTEXITCODE -ne 0) { throw "Sao lưu database thất bại với mã $LASTEXITCODE." }
Write-Host "Đã sao lưu và kiểm tra: $backupPath"
