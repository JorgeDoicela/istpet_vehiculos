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

Write-Host "==> Construyendo imagen del Backend..." -ForegroundColor Yellow
docker build -t istpet/backend:release ./backend

Write-Host "==> Construyendo imagen del Frontend..." -ForegroundColor Yellow
docker build -t istpet/frontend:release ./frontend

Write-Host "==> Exportando imágenes a un archivo .tar (esto puede tardar)..." -ForegroundColor Yellow
docker save -o "$RELEASE_DIR/$IMAGES_FILE" istpet/backend:release istpet/frontend:release

Write-Host "==> Copiando archivos de orquestación y despliegue..." -ForegroundColor Cyan
Copy-Item "docker-compose.server.yml" -Destination "$RELEASE_DIR/"
Copy-Item "deploy-direct-server.ps1" -Destination "$RELEASE_DIR/"
Copy-Item ".env.server.example" -Destination "$RELEASE_DIR/.env" # Se renombra a .env para el bundle listo para usar

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
