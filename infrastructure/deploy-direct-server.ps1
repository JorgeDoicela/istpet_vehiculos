$ErrorActionPreference = "Stop"

Write-Host "==> Cargando imagenes Docker..."
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
