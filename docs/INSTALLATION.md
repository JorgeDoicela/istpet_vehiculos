# Guía de Instalación y Despliegue ISTPET System

Este documento explica los pasos necesarios para instalar, configurar y ejecutar el sistema de gestión vehicular de ISTPET.

## Requisitos Previos
- **Backend**: .NET 8 SDK instalado.
- **Base de Datos**: MySQL Server (v8.0+) o MariaDB.
- **Frontend**: Node.js (v20+) y npm.

## Configuración Inicial

### 1. Base de Datos (MySQL)
Ejecuta el script SQL `istpet_vehiculos` en tu servidor MySQL para crear las 11 tablas, las vistas y los procedimientos almacenados originales.

### 2. Configuración del Backend
Edita el archivo `backend/appsettings.json` con tus credenciales de base de datos:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=istpet_vehiculos;User=root;Password=tu_password;"
}
```

## Ejecución del Sistema

### Iniciar el Backend (.NET 8)
1. Abre una terminal en la carpeta `backend/`.
2. Restaura los paquetes y compila con:
   ```bash
   dotnet restore
   dotnet build
   ```
3. Inicia el servidor API (puerto 5112 bloqueado por el frontend):
   ```bash
   dotnet run
   ```

### Iniciar el Frontend (React + Vite)
1. Abre una terminal en la carpeta `frontend/`.
2. Instala las dependencias profesionales (Axios, Lucide, Tailwind):
   ```bash
   npm install
   ```
3. Inicia el servidor de desarrollo en modo responsivo:
   ```bash
   npm run dev
   ```
4. Abre tu navegador en `http://localhost:5173`.

## Despliegue en Producción

### Generar Build de Frontend
```bash
cd frontend
npm run build
```

### Generar Publish de Backend
```bash
cd backend
dotnet publish -c Release -o ./publish
```

## Mantenimiento Continuo
- **Sincronización**: Utiliza el botón "Ejecutar Sincronización Segura" en el Dashboard para ingerir datos externos una vez configuradas las fuentes.
- **Logs**: Revisa la tabla `sync_logs` para auditorías técnicas de la integridad de los datos.
