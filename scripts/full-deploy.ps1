# Script de Despliegue TOTAL ISTPET Vehiculos (v2.7 - Detailed Edition)
$ErrorActionPreference = "Stop"
# Restauramos el progreso local para ver la barra de carga del ZIP
$ProgressPreference = 'Continue'

$SERVER_IP = "192.168.7.50"
$SSH_USER = "Administrador"
$BASE_PATH = Get-Location
$DIST_IIS = "dist_iis"
$TIMESTAMP = Get-Date -Format "yyyyMMdd_HHmm"
$BUNDLE_NAME = "ISTPET_DEPLOY_$TIMESTAMP.zip"

Write-Host "`n=== INICIANDO DESPLIEGUE AUTOMATIZADO ===" -ForegroundColor Cyan

# 0. Limpieza Local
Write-Host "--- Preparando el entorno local ---" -ForegroundColor Gray
Write-Host "   - Cerrando procesos dotnet..." -ForegroundColor DarkGray
Stop-Process -Name "dotnet" -Force -ErrorAction SilentlyContinue
Stop-Process -Name "w3wp" -Force -ErrorAction SilentlyContinue
Start-Sleep -s 1

# 1. Construccion Local
Write-Host "--- 1/4 Generando compilaciones locales ---" -ForegroundColor Gray
if (Test-Path $DIST_IIS) {
    Write-Host "   - Limpiando carpeta dist anterior..." -ForegroundColor DarkGray
    Remove-Item -Path $DIST_IIS -Recurse -Force
}
New-Item -ItemType Directory -Path "$DIST_IIS/backend" | Out-Null
New-Item -ItemType Directory -Path "$DIST_IIS/frontend" | Out-Null

Write-Host "   - Compilando .NET Backend (Release)..." -ForegroundColor Cyan
dotnet publish ./backend -c Release -o "$BASE_PATH/$DIST_IIS/backend" --verbosity quiet

Write-Host "   - Compilando React Frontend (Vite)..." -ForegroundColor Cyan
$prevErrorAction = $ErrorActionPreference
$ErrorActionPreference = "Continue"
& npm run build --prefix ./frontend 2>$null
$ErrorActionPreference = $prevErrorAction

Write-Host "   - Integrando archivos..." -ForegroundColor DarkGray
Copy-Item "./frontend/dist/*" -Destination "$DIST_IIS/frontend" -Recurse -Force

# 2. Empaquetado
Write-Host "--- 2/4 Creando paquete de distribucion ---" -ForegroundColor Gray
if (Test-Path $BUNDLE_NAME) { Remove-Item $BUNDLE_NAME }
Start-Sleep -s 1
Compress-Archive -Path "$DIST_IIS/*" -DestinationPath "$BUNDLE_NAME" -Force
Write-Host ("   - Paquete creado: " + $BUNDLE_NAME) -ForegroundColor Green

# 3. Transferencia
Write-Host ("--- 3/4 Subiendo paquete al servidor (" + $SERVER_IP + ") ---") -ForegroundColor Cyan
# scp sin -q para ver el progreso de subida
scp "./$BUNDLE_NAME" "${SSH_USER}@${SERVER_IP}:Desktop/"

# 4. Instalacion Remota
Write-Host "--- 4/4 Ejecutando instalacion remota ---" -ForegroundColor Green

$RemoteScriptTemplate = @'
    $ProgressPreference = 'SilentlyContinue'
    Import-Module WebAdministration -ErrorAction SilentlyContinue

    Write-Output '>>> [1/5] Deteniendo servicios IIS...'
    $sites = Get-Website | Where-Object { $_.name -like '*Logistica*' }
    foreach ($s in $sites) {
        Write-Output "      * Deteniendo Sitio: $($s.name)"
        Stop-WebSite -Name $s.name -ErrorAction SilentlyContinue
    }

    $pools = Get-ChildItem 'IIS:\AppPools\' | Where-Object { $_.name -like '*Logistica*' }
    foreach ($p in $pools) {
        Write-Output "      * Deteniendo Pool: $($p.name)"
        Stop-WebAppPool -Name $p.name -ErrorAction SilentlyContinue
    }

    Write-Output '>>> [2/5] Limpiando procesos bloqueados...'
    Stop-Process -Name "w3wp", "dotnet" -Force -ErrorAction SilentlyContinue
    Start-Sleep -s 2

    Write-Output '>>> [3/5] Extrayendo paquete...'
    if (!(Test-Path 'C:\temp_deploy')) { New-Item -ItemType Directory -Path 'C:\temp_deploy' | Out-Null }
    Expand-Archive -Path "C:\Users\Administrador\Desktop\ZIP_NAME_PLACEHOLDER" -DestinationPath 'C:\temp_deploy' -Force

    Write-Output '>>> [4/5] Aplicando archivos (Robocopy)...'
    Write-Output '      * Actualizando Backend...'
    # Quitamos Out-Null pero mantenemos /NFL /NDL para ver solo el resumen estadistico
    robocopy 'C:\temp_deploy\backend' 'C:\inetpub\wwwroot\apiLogistica' /E /R:5 /W:2 /MT:32 /NJH /NJS /NFL /NDL /NP /NC /NS

    Write-Output '      * Actualizando Frontend...'
    robocopy 'C:\temp_deploy\frontend' 'C:\inetpub\wwwroot\logistica' /E /R:5 /W:2 /MT:32 /NJH /NJS /NFL /NDL /NP /NC /NS

    Write-Output '>>> [5/5] Reiniciando servicios...'
    foreach ($p in $pools) {
        Write-Output "      * Iniciando Pool: $($p.name)"
        Start-WebAppPool -Name $p.name -ErrorAction SilentlyContinue
    }
    foreach ($s in $sites) {
        Write-Output "      * Iniciando Sitio: $($s.name)"
        Start-WebSite -Name $s.name -ErrorAction SilentlyContinue
    }

    Remove-Item -Path 'C:\temp_deploy' -Recurse -Force
    Remove-Item -Path "C:\Users\Administrador\Desktop\ZIP_NAME_PLACEHOLDER" -Force
    Write-Output '>>> PROCESO FINALIZADO EN EL SERVIDOR'
'@

$RemoteScript = $RemoteScriptTemplate.Replace("ZIP_NAME_PLACEHOLDER", $BUNDLE_NAME)
$Bytes = [System.Text.Encoding]::Unicode.GetBytes($RemoteScript)
$EncodedCommand = [Convert]::ToBase64String($Bytes)

# Ejecucion via SSH (quitamos -q para ver el flujo de mensajes del servidor)
ssh "${SSH_USER}@${SERVER_IP}" "powershell -EncodedCommand $EncodedCommand"

Write-Host " "
Write-Host "==========================================================" -ForegroundColor Green
Write-Host "   DESPLIEGUE FINALIZADO EXITOSAMENTE" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
