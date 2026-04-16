# Script de Automatización de Despliegue ISTPET Vehículos (Zenith 2026)
# Este script construye el Backend y Frontend y crea un paquete listo para IIS.

$ErrorActionPreference = "Stop"
$BASE_PATH = Get-Location
$DIST_IIS = "dist_iis"
$TIMESTAMP = Get-Date -Format "yyyyMMdd_HHmm"
$ZIP_NAME = "ISTPET_DEPLOY_BUNDLE_$TIMESTAMP.zip"

Write-Host "==> Iniciando Automatización de Despliegue..." -ForegroundColor Cyan

# 1. Limpieza
Write-Host "==> Limpiando carpeta temporal..." -ForegroundColor Gray
if (Test-Path $DIST_IIS) { Remove-Item -Path $DIST_IIS -Recurse -Force }
New-Item -ItemType Directory -Path "$DIST_IIS/backend"
New-Item -ItemType Directory -Path "$DIST_IIS/frontend"

# 2. Construcción Backend (.NET)
Write-Host "==> Generando publicación del Backend (Cuidado: debe ser Release)..." -ForegroundColor Yellow
dotnet publish ./backend -c Release -o "$BASE_PATH/$DIST_IIS/backend"

# 3. Construcción Frontend (Vite)
Write-Host "==> Construyendo Frontend..." -ForegroundColor Yellow
# Usamos cmd /c para evitar problemas de política de ejecución de PowerShell
cmd /c "npm run build --prefix ./frontend"

# 4. Copiar Frontend al bundle
Write-Host "==> Moviendo Frontend a la carpeta de despliegue..." -ForegroundColor Gray
Copy-Item "./frontend/dist/*" -Destination "$DIST_IIS/frontend" -Recurse -Force

# 5. Empaquetado final
Write-Host "==> Creando paquete ZIP unificado..." -ForegroundColor Green
Compress-Archive -Path "$DIST_IIS/*" -DestinationPath "$ZIP_NAME" -Force

Write-Host ""
Write-Host "==========================================================" -ForegroundColor Green
Write-Host " PROCESO COMPLETADO CON ÉXITO" -ForegroundColor Green
Write-Host " Paquete generado: $ZIP_NAME" -ForegroundColor Green
Write-Host " Ubicación: $BASE_PATH\$ZIP_NAME" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
Write-Host "Instrucción: Copia este archivo al servidor IIS y extrae" 
Write-Host "el contenido de /backend y /frontend en sus respectivas carpetas."
