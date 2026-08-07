<#
  Backup do banco IndustrialOS (PostgreSQL) - formato custom comprimido (-Fc).
  Uso:   powershell -ExecutionPolicy Bypass -File backup-db.ps1
  Restaura: pg_restore --clean --if-exists -d industrialos <arquivo.dump>

  Config por variavel de ambiente (com defaults de DEV):
    PGHOST (localhost) PGPORT (5432) PGDATABASE (industrialos) PGUSER (industrialos)
    PGPASSWORD (senha - NAO fica no script)
    BACKUP_DIR (C:\Users\<voce>\industrialos-backups)
    BACKUP_RETENCAO_DIAS (14)
#>
$ErrorActionPreference = "Stop"

$pgBin  = "C:\Program Files\PostgreSQL\18\bin"
$pgDump = Join-Path $pgBin "pg_dump.exe"

$db     = if ($env:PGDATABASE) { $env:PGDATABASE } else { "industrialos" }
$dbUser = if ($env:PGUSER)     { $env:PGUSER }     else { "industrialos" }
$dbHost = if ($env:PGHOST)     { $env:PGHOST }     else { "localhost" }
$dbPort = if ($env:PGPORT)     { $env:PGPORT }     else { "5432" }
if (-not $env:PGPASSWORD) { $env:PGPASSWORD = "industrialos" }  # default DEV; em producao use env real

$dir      = if ($env:BACKUP_DIR) { $env:BACKUP_DIR } else { Join-Path $env:USERPROFILE "industrialos-backups" }
$retencao = if ($env:BACKUP_RETENCAO_DIAS) { [int]$env:BACKUP_RETENCAO_DIAS } else { 14 }

if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir | Out-Null }

$stamp   = Get-Date -Format "yyyyMMdd-HHmmss"
$arquivo = Join-Path $dir "industrialos-$stamp.dump"

Write-Host "Backup: $db -> $arquivo"
& $pgDump -h $dbHost -p $dbPort -U $dbUser -Fc -f $arquivo $db
if ($LASTEXITCODE -ne 0) { throw "pg_dump falhou (exit $LASTEXITCODE)" }

$tam = [math]::Round((Get-Item $arquivo).Length / 1KB, 1)
Write-Host "OK - $tam KB"

# Retencao: remove dumps mais antigos que N dias
Get-ChildItem $dir -Filter "industrialos-*.dump" |
  Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-$retencao) } |
  ForEach-Object { Write-Host "Removendo antigo: $($_.Name)"; Remove-Item $_.FullName -Force }
