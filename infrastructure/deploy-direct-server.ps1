# Guardamos la preferencia original
$originalPreference = $ErrorActionPreference

Write-Host "==> Deteniendo versiones anteriores (si existen)..." -ForegroundColor Yellow
$ErrorActionPreference = "Continue" # Permitimos continuar si docker compose escribe en stderr

# 1. Intentamos bajar por compose (limpio)
docker compose -f .\docker-compose.server.yml down --remove-orphans 2>$null

# 2. Por si acaso fueron creados con otro nombre de proyecto, forzamos por nombre de contenedor
Write-Host "==> Limpieza de seguridad por nombre de contenedor..." -ForegroundColor Gray
docker rm -f istpet_backend istpet_frontend 2>$null

# Restauramos la preferencia para los pasos críticos
$ErrorActionPreference = $originalPreference

Write-Host "==> Cargando imagenes Docker..." -ForegroundColor Cyan
docker load -i .\istpet-images.tar

# El paquete ya incluye .env listo para editar/usar.
Write-Host "==> Verificando archivo de entorno..."
if (!(Test-Path .\.env)) {
    Write-Host "No existe .env en esta carpeta. Verifica el contenido del paquete."
    exit 1
}

Write-Host "==> Levantando servicios en modo directo..."
docker compose -f .\docker-compose.server.yml --env-file .\.env up -d

Write-Host "==> Estado de contenedores:"
docker compose -f .\docker-compose.server.yml ps

Write-Host "Despliegue completado."
