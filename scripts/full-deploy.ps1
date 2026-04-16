# Script de Despliegue TOTAL ISTPET Vehículos (v2.1 - Logging Edition)
$ErrorActionPreference = "Stop"
$SERVER_IP = "192.168.7.50"
$SSH_USER = "Administrador"
$BASE_PATH = Get-Location
$DIST_IIS = "dist_iis"
$TIMESTAMP = Get-Date -Format "yyyyMMdd_HHmm"
$BUNDLE_NAME = "ISTPET_DEPLOY_$TIMESTAMP.zip"

Write-Host "==> Iniciando Despliegue Automatizado vía SSH..." -ForegroundColor Cyan

# 1. Construcción Local
Write-Host "==> Generando compilaciones locales..." -ForegroundColor Gray
if (Test-Path $DIST_IIS) { Remove-Item -Path $DIST_IIS -Recurse -Force }
New-Item -ItemType Directory -Path "$DIST_IIS/backend"
New-Item -ItemType Directory -Path "$DIST_IIS/frontend"

dotnet publish ./backend -c Release -o "$BASE_PATH/$DIST_IIS/backend"
cmd /c "npm run build --prefix ./frontend"
Copy-Item "./frontend/dist/*" -Destination "$DIST_IIS/frontend" -Recurse -Force

# 2. Empaquetado
Write-Host "==> Creando paquete..." -ForegroundColor Gray
if (Test-Path $BUNDLE_NAME) { Remove-Item $BUNDLE_NAME }
Compress-Archive -Path "$DIST_IIS/*" -DestinationPath "$BUNDLE_NAME" -Force

# 3. Transferencia
Write-Host "==> Subiendo paquete al servidor..." -ForegroundColor Cyan
scp "./$BUNDLE_NAME" "${SSH_USER}@${SERVER_IP}:Desktop/"

# 4. Instalación Remota (Con visibilidad total y sin errores de comillas)
Write-Host "==> Ejecutando instalación remota..." -ForegroundColor Green

# Definimos el bloque de comandos para el servidor
$RemoteScript = @"
    Import-Module WebAdministration -ErrorAction SilentlyContinue
    Write-Output '--- SERVIDOR: Gestionando Bloqueos (IIS Recycle) ---'
    
    # Detener Sites y Pools relacionados con Logistica
    `$sites = Get-Website | Where-Object { `$_.name -like '*Logistica*' }
    foreach (`$s in `$sites) { 
        Write-Output ('Deteniendo Sitio: ' + `$s.name)
        Stop-WebSite -Name `$s.name -ErrorAction SilentlyContinue 
    }
    
    `$pools = Get-ChildItem 'IIS:\AppPools\' | Where-Object { `$_.name -like '*Logistica*' }
    foreach (`$p in `$pools) { 
        Write-Output ('Deteniendo Pool: ' + `$p.name)
        Stop-WebAppPool -Name `$p.name -ErrorAction SilentlyContinue 
    }

    # Fuerza bruta para liberar archivos si el Pool no se cerró a tiempo
    Write-Output '--- SERVIDOR: Verificando procesos remanentes ---'
    Stop-Process -Name "w3wp" -Force -ErrorAction SilentlyContinue
    Stop-Process -Name "dotnet" -Force -ErrorAction SilentlyContinue
    Start-Sleep -s 3

    Write-Output '--- SERVIDOR: Iniciando Extracción ---'
    if (!(Test-Path 'C:\temp_deploy')) { New-Item -ItemType Directory -Path 'C:\temp_deploy' }
    Expand-Archive -Path "C:\Users\Administrador\Desktop\$BUNDLE_NAME" -DestinationPath 'C:\temp_deploy' -Force
    
    Write-Output '--- SERVIDOR: Copiando Backend ---'
    robocopy 'C:\temp_deploy\backend' 'C:\inetpub\wwwroot\apiLogistica' /E /R:5 /W:5 /MT:32
    
    Write-Output '--- SERVIDOR: Copiando Frontend ---'
    robocopy 'C:\temp_deploy\frontend' 'C:\inetpub\wwwroot\logistica' /E /R:5 /W:5 /MT:32

    # Reiniciar todo
    foreach (`$p in `$pools) { 
        Write-Output ('Iniciando Pool: ' + `$p.name)
        Start-WebAppPool -Name `$p.name -ErrorAction SilentlyContinue 
    }
    foreach (`$s in `$sites) { 
        Write-Output ('Iniciando Sitio: ' + `$s.name)
        Start-WebSite -Name `$s.name -ErrorAction SilentlyContinue 
    }

    Remove-Item -Path 'C:\temp_deploy' -Recurse -Force
    Remove-Item -Path "C:\Users\Administrador\Desktop\$BUNDLE_NAME" -Force
    Write-Output '--- SERVIDOR: Proceso Finalizado ---'
"@

# Codificación Base64 para evitar problemas con Where-Object y comillas
$Bytes = [System.Text.Encoding]::Unicode.GetBytes($RemoteScript)
$EncodedCommand = [Convert]::ToBase64String($Bytes)

ssh "${SSH_USER}@${SERVER_IP}" "powershell -EncodedCommand $EncodedCommand"

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host " DESPLIEGUE FINALIZADO EXITOSAMENTE" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
