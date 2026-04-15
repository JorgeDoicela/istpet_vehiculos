# Script para Automatizar la Creación del Bundle de Despliegue (Zenith 2026)
$ErrorActionPreference = "Stop"

$RELEASE_DIR = "release"
$BUNDLE_NAME = "istpet_direct_server_bundle.zip"
$IMAGES_FILE = "istpet-images.tar"

Write-Host "==> Limpiando carpeta de release..." -ForegroundColor Cyan
if (Test-Path $RELEASE_DIR) {
    Remove-Item -Path $RELEASE_DIR -Recurse -Force
}
New-Item -ItemType Directory -Path $RELEASE_DIR

Write-Host "==> Leyendo configuración del API desde .env..." -ForegroundColor Gray
$VITE_API_URL = (Get-Content .env | Select-String "VITE_API_URL=").ToString().Split("=")[1].Trim()

Write-Host "==> Construyendo imagen del Backend..." -ForegroundColor Yellow
docker build -t istpet/backend:release ./backend

Write-Host "==> Construyendo imagen del Frontend (API: $VITE_API_URL)..." -ForegroundColor Yellow
docker build --build-arg VITE_API_URL=$VITE_API_URL -t istpet/frontend:release ./frontend

Write-Host "==> Exportando imágenes a un archivo .tar (esto puede tardar)..." -ForegroundColor Yellow
docker save -o "$RELEASE_DIR/$IMAGES_FILE" istpet/backend:release istpet/frontend:release

Write-Host "==> Copiando archivos de orquestación y despliegue..." -ForegroundColor Cyan
Copy-Item "infrastructure/docker-compose.server.yml" -Destination "$RELEASE_DIR/"
Copy-Item "infrastructure/deploy-direct-server.ps1" -Destination "$RELEASE_DIR/"
Copy-Item ".env" -Destination "$RELEASE_DIR/.env" # Usa el .env principal con las variables por defecto del usuario

Write-Host "==> Creando paquete comprimido (.zip)..." -ForegroundColor Green
if (Test-Path $BUNDLE_NAME) { Remove-Item $BUNDLE_NAME }
Compress-Archive -Path "$RELEASE_DIR/*" -DestinationPath "$RELEASE_DIR/$BUNDLE_NAME" -Force

# Mover el zip a la carpeta release si no estaba ahí
if (Test-Path ".\$BUNDLE_NAME") {
    Move-Item ".\$BUNDLE_NAME" "$RELEASE_DIR/" -Force
}

Write-Host ""
Write-Host "==========================================================" -ForegroundColor Green
Write-Host " Bundle de Despliegue creado con éxito en: $RELEASE_DIR/" -ForegroundColor Green
Write-Host " Archivo final: $BUNDLE_NAME" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
