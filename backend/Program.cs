using backend.Data;
using backend.Hosting;
using backend.Services.Helpers;
using backend.Services.Implementations;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────────────────────────────────────
// CADENAS DE CONEXIÓN
// Prioridad: variable de entorno > appsettings (Development / Production)
// ─────────────────────────────────────────────────────────────────────────────
var connectionString =
    Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

var sigafiConnectionString =
    Environment.GetEnvironmentVariable("SIGAFI_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("SigafiConnection");

if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "❌ No se encontró la cadena de conexión local. " +
        "Define DATABASE_URL o ConnectionStrings:DefaultConnection.");

if (string.IsNullOrWhiteSpace(sigafiConnectionString))
    throw new InvalidOperationException(
        "❌ No se encontró la cadena de conexión SIGAFI. " +
        "Define SIGAFI_CONNECTION_STRING o ConnectionStrings:SigafiConnection.");

// TiDB Cloud: SSL obligatorio
if (connectionString.Contains("tidbcloud.com", StringComparison.OrdinalIgnoreCase)
    && !connectionString.Contains("SslMode", StringComparison.OrdinalIgnoreCase))
    connectionString += ";SslMode=Required;";

// ─────────────────────────────────────────────────────────────────────────────
// JWT
// ─────────────────────────────────────────────────────────────────────────────
var jwtSection = builder.Configuration.GetSection("JwtSettings");
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY")
             ?? jwtSection["Key"]
             ?? throw new InvalidOperationException("❌ JwtSettings:Key no configurada.");

if (jwtKey.Length < 32)
    throw new InvalidOperationException("❌ JWT Key debe tener al menos 32 caracteres.");

var keyBytes = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSection["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// ─────────────────────────────────────────────────────────────────────────────
// CORS
// Desarrollo → cualquier origen (dev friendly).
// Producción → solo los orígenes de Cors:AllowedOrigins.
// ─────────────────────────────────────────────────────────────────────────────
var allowedOrigins = (Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS")
                        ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                     ?? builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentPolicy",
        p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

    options.AddPolicy("ProductionPolicy", p =>
    {
        if (allowedOrigins.Length > 0)
            p.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader();
        else
            p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// ─────────────────────────────────────────────────────────────────────────────
// RATE LIMITING  — ventana fija por IP en el endpoint de login
// ─────────────────────────────────────────────────────────────────────────────
var rlSection = builder.Configuration.GetSection("RateLimiting");
int loginPermitLimit = rlSection.GetValue("LoginPermitLimit", 10);
int loginWindowSeconds = rlSection.GetValue("LoginWindowSeconds", 30);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = loginPermitLimit,
                Window = TimeSpan.FromSeconds(loginWindowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

// ─────────────────────────────────────────────────────────────────────────────
// HEALTH CHECKS
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddCheck("bd_local", () =>
    {
        try
        {
            using var conn = new MySqlConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1";
            cmd.ExecuteScalar();
            return HealthCheckResult.Healthy("BD local istpet_vehiculos responde.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("BD local no responde.", ex);
        }
    }, tags: ["db", "local"])
    .AddCheck("bd_sigafi", () =>
    {
        try
        {
            using var conn = new MySqlConnection(sigafiConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1";
            cmd.ExecuteScalar();
            return HealthCheckResult.Healthy("BD SIGAFI responde.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("BD SIGAFI no disponible (modo espejo activo).", ex);
        }
    }, tags: ["db", "sigafi"]);

// ─────────────────────────────────────────────────────────────────────────────
// CACHÉ EN MEMORIA
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddMemoryCache();

// ─────────────────────────────────────────────────────────────────────────────
// SWAGGER (solo Development)
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "ISTPET Logistics API",
        Version = "v1",
        Description = "API de control logístico de vehículos — ISTPET 2026"
    });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization: 'Bearer {token}'",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (System.IO.File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// ─────────────────────────────────────────────────────────────────────────────
// BASE DE DATOS LOCAL
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// ─────────────────────────────────────────────────────────────────────────────
// SERVICIOS DE DOMINIO
// ─────────────────────────────────────────────────────────────────────────────
// Política explícita: las propiedades ya están en camelCase en todos los DTOs.
// Sin esta configuración, .NET 8 System.Text.Json lanza "property name collides"
// cuando el nombre ya es camelCase y el serializador intenta transformarlo.
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = null;           // usar nombres tal como están
        opts.JsonSerializerOptions.PropertyNameCaseInsensitive = true;    // deserializar sin distinción de mayúsculas
        opts.JsonSerializerOptions.DictionaryKeyPolicy = null;
    });

builder.Services.AddAutoMapper(typeof(backend.Mappings.MappingProfile));

// Circuit breaker SIGAFI — singleton compartido por todas las solicitudes
builder.Services.AddSingleton<ISigafiResiliencePipeline, SigafiResiliencePipeline>();

builder.Services.AddScoped<IVehiculoService, SqlVehiculoService>();
builder.Services.AddScoped<IEstudianteService, SqlEstudianteService>();
builder.Services.AddScoped<ILogisticaService, SqlLogisticaService>();
builder.Services.AddScoped<IDataSyncService, DataSyncService>();
builder.Services.AddScoped<ISigafiMirrorPersistenceService, SigafiMirrorPersistenceService>();
builder.Services.AddScoped<ICentralStudentProvider, SqlCentralStudentProvider>();
builder.Services.AddScoped<IAgendaPanelService, AgendaPanelService>();
builder.Services.AddScoped<SigafiExtractionProbe>();
builder.Services.AddScoped<ISigafiReportService, SigafiReportService>();

// Auditoría — singleton porque usa IServiceScopeFactory internamente
builder.Services.AddSingleton<IAuditService, SqlAuditService>();

// Sincronización automática SIGAFI → local
builder.Services.Configure<SigafiMirrorSyncOptions>(
    builder.Configuration.GetSection(SigafiMirrorSyncOptions.SectionName));
builder.Services.AddHostedService<SigafiMirrorBackgroundService>();

// ─────────────────────────────────────────────────────────────────────────────
// PUERTO (Render / Docker)
// ─────────────────────────────────────────────────────────────────────────────
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
    builder.WebHost.UseUrls($"http://*:{port}");

// ─────────────────────────────────────────────────────────────────────────────
// PIPELINE HTTP
// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

// Compatibilidad con instalaciones existentes:
// HEALER: Asegura que el esquema local esté alineado con el nuevo modelo 2026.
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        var schemaCommands = new List<string>
        {
            // -------------------------------------------------------------------------
            // 1. TABLAS DE CATÁLOGO (SI NO EXISTEN)
            // -------------------------------------------------------------------------
            "CREATE TABLE IF NOT EXISTS periodos (idPeriodo CHAR(7) PRIMARY KEY, detalle VARCHAR(100), fecha_inicial DATE, fecha_final DATE, activo TINYINT DEFAULT 1)",
            "CREATE TABLE IF NOT EXISTS carreras (idCarrera INT PRIMARY KEY, Carrera VARCHAR(100), activa TINYINT DEFAULT 1)",
            "CREATE TABLE IF NOT EXISTS secciones (idSeccion INT PRIMARY KEY, seccion VARCHAR(50))",
            "CREATE TABLE IF NOT EXISTS modalidades (idModalidad INT PRIMARY KEY, modalidad VARCHAR(50))",
            "CREATE TABLE IF NOT EXISTS instituciones (idInstitucion INT PRIMARY KEY, Institucion VARCHAR(150))",
            "CREATE TABLE IF NOT EXISTS tipo_licencia (id_tipo INT PRIMARY KEY, codigo VARCHAR(10) UNIQUE, descripcion VARCHAR(200), activo TINYINT DEFAULT 1, id_categoria_sigafi INT NULL)",
            "CREATE TABLE IF NOT EXISTS cursos (idNivel INT PRIMARY KEY, idCarrera INT, Nivel VARCHAR(50), jerarquia INT)",
            "CREATE TABLE IF NOT EXISTS categoria_vehiculos (idCategoria INT PRIMARY KEY, categoria VARCHAR(100))",
            "CREATE TABLE IF NOT EXISTS categorias_examenes_conduccion (IdCategoria INT PRIMARY KEY, categoria VARCHAR(100), activa TINYINT DEFAULT 1)",
            "CREATE TABLE IF NOT EXISTS subcategorias_vehiculos (idSubcategoria INT PRIMARY KEY, subcategoria VARCHAR(100), idCategoria INT)",
            "CREATE TABLE IF NOT EXISTS cond_alumnos_vehiculos (idAsignacion INT PRIMARY KEY AUTO_INCREMENT, idAlumno VARCHAR(14), idVehiculo INT, idProfesor VARCHAR(14), idPeriodo VARCHAR(7), fechaAsignacion DATETIME DEFAULT CURRENT_TIMESTAMP, activa TINYINT DEFAULT 1)",
            "CREATE TABLE IF NOT EXISTS cond_alumnos_horarios (idAsignacionHorario INT PRIMARY KEY, idAsignacion INT NOT NULL, idFecha INT NOT NULL, idHora INT NOT NULL, asiste TINYINT DEFAULT 0, activo TINYINT DEFAULT 1, observacion VARCHAR(100))",
            "CREATE TABLE IF NOT EXISTS cond_practicas_horarios_alumnos (idPractica INT NOT NULL, idAsignacionHorario INT NOT NULL, PRIMARY KEY (idPractica, idAsignacionHorario))",
            "CREATE TABLE IF NOT EXISTS matriculas_operacion (idMatricula INT PRIMARY KEY, horas_completadas DECIMAL(10,2) DEFAULT 0, estado VARCHAR(20) DEFAULT 'ACTIVO')",
            "CREATE TABLE IF NOT EXISTS practicas_operacion (idPractica INT PRIMARY KEY, validada TINYINT DEFAULT 0, observacion_admin TEXT NULL)",

            // -------------------------------------------------------------------------
            // 2. HARDENING DE COLUMNAS (PROFESORES / ALUMNOS / MATRICULAS)
            // -------------------------------------------------------------------------
            "ALTER TABLE profesores ADD COLUMN IF NOT EXISTS callePrincipal VARCHAR(100) NULL",
            "ALTER TABLE profesores ADD COLUMN IF NOT EXISTS calleSecundaria VARCHAR(100) NULL",
            "ALTER TABLE profesores ADD COLUMN IF NOT EXISTS numeroCasa VARCHAR(50) NULL",
            "ALTER TABLE profesores ADD COLUMN IF NOT EXISTS abreviatura VARCHAR(5) NULL",
            "ALTER TABLE profesores ADD COLUMN IF NOT EXISTS abreviatura_post VARCHAR(5) NULL",
            "ALTER TABLE profesores ADD COLUMN IF NOT EXISTS idParroquiaNacimiento INT NULL",
            "ALTER TABLE profesores ADD COLUMN IF NOT EXISTS idParroquiaResidencia INT NULL",
            "ALTER TABLE profesores ADD COLUMN IF NOT EXISTS idEtnia INT NULL",
            "ALTER TABLE profesores ADD COLUMN IF NOT EXISTS idNacionalidad INT NULL",
            "ALTER TABLE profesores ADD COLUMN IF NOT EXISTS idDiscapacidad INT NULL",
            "ALTER TABLE profesores ADD COLUMN IF NOT EXISTS emailInstitucional VARCHAR(255) NULL",
            "ALTER TABLE profesores ADD COLUMN IF NOT EXISTS tipoSangre VARCHAR(5) NULL",
            
            "ALTER TABLE alumnos ADD COLUMN IF NOT EXISTS primerIngreso TINYINT DEFAULT 1",
            "ALTER TABLE alumnos ADD COLUMN IF NOT EXISTS idEtnia INT NULL",
            "ALTER TABLE alumnos ADD COLUMN IF NOT EXISTS idNacionalidad INT NULL",
            "ALTER TABLE alumnos ADD COLUMN IF NOT EXISTS idDiscapacidad INT NULL",
            "ALTER TABLE alumnos ADD COLUMN IF NOT EXISTS email_institucional VARCHAR(255) NULL",
            
            "ALTER TABLE matriculas ADD COLUMN IF NOT EXISTS beca_colegiatura DECIMAL(10,2) DEFAULT 0",
            "ALTER TABLE matriculas ADD COLUMN IF NOT EXISTS beca_matricula DECIMAL(10,2) DEFAULT 0",

            "ALTER TABLE asignacion_instructores_vehiculos ADD COLUMN IF NOT EXISTS fecha_salidad DATE NULL",

            // -------------------------------------------------------------------------
            // 3. SANEAMIENTO DE CONSTRAINTS (NULLABILITY)
            // -------------------------------------------------------------------------
            "ALTER TABLE profesores MODIFY idEtnia INT NULL, MODIFY idNacionalidad INT NULL, MODIFY idParroquiaNacimiento INT NULL, MODIFY idParroquiaResidencia INT NULL, MODIFY idDiscapacidad INT NULL, MODIFY tipoSangre VARCHAR(5) NULL",
            "ALTER TABLE alumnos MODIFY idEtnia INT NULL, MODIFY idNacionalidad INT NULL, MODIFY idDiscapacidad INT NULL",

            // -------------------------------------------------------------------------
            // 4. TABLAS OPERATIVAS Y NUEVAS
            // -------------------------------------------------------------------------
            @"CREATE TABLE IF NOT EXISTS vehiculos_operacion (
                idVehiculo INT PRIMARY KEY,
                id_tipo_licencia INT,
                id_instructor_fijo VARCHAR(14),
                estado_mecanico VARCHAR(30) DEFAULT 'OPERATIVO'
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_spanish_ci",

            @"CREATE TABLE IF NOT EXISTS matriculas_examen_conduccion (
                idMatricula INT NOT NULL,
                idCategoria INT NOT NULL,
                nota DECIMAL(6,2) NULL,
                observacion TEXT NULL,
                usuario VARCHAR(50) NULL,
                fechaExamen DATE NULL,
                fechaIngreso DATETIME NULL,
                instructor VARCHAR(80) NULL,
                PRIMARY KEY (idMatricula, idCategoria)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_spanish_ci",

            @"CREATE TABLE IF NOT EXISTS fechas_horarios (
                idFecha INT NOT NULL,
                fecha DATE NOT NULL,
                finsemana TINYINT NOT NULL DEFAULT 0,
                dia VARCHAR(15) NULL,
                PRIMARY KEY (idFecha)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_spanish_ci",

            @"CREATE TABLE IF NOT EXISTS horario_profesores (
                idHorario INT NOT NULL,
                idAsignacion INT NULL,
                idHora INT NULL,
                idFecha INT NULL,
                asiste TINYINT DEFAULT 1,
                activo TINYINT DEFAULT 1,
                PRIMARY KEY (idHorario)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_spanish_ci",

            @"CREATE TABLE IF NOT EXISTS audit_logs (
                id          INT          NOT NULL AUTO_INCREMENT,
                usuario     VARCHAR(50)  NOT NULL,
                accion      VARCHAR(50)  NOT NULL,
                entidad_id  VARCHAR(100) NULL,
                detalles    TEXT         NULL,
                ip_origen   VARCHAR(45)  NULL,
                fecha_hora  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (id),
                INDEX idx_audit_usuario (usuario),
                INDEX idx_audit_accion  (accion),
                INDEX idx_audit_fecha   (fecha_hora)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_spanish_ci",

            "CREATE INDEX IF NOT EXISTS idx_practicas_ensalida ON cond_alumnos_practicas (ensalida, cancelado)",
            "CREATE INDEX IF NOT EXISTS idx_practicas_alumno ON cond_alumnos_practicas (idalumno)",
            "CREATE INDEX IF NOT EXISTS idx_practicas_vehiculo ON cond_alumnos_practicas (idvehiculo)",
            "CREATE INDEX IF NOT EXISTS idx_practicas_fecha ON cond_alumnos_practicas (fecha)",

            @"CREATE OR REPLACE VIEW v_clases_activas AS
            SELECT
                p.idPractica AS id_registro,
                p.idalumno AS idAlumno,
                e.primerNombre AS primer_nombre,
                e.apellidoPaterno AS apellido_paterno,
                COALESCE(CONCAT(e.apellidoPaterno, ' ', e.primerNombre), p.idalumno) AS estudiante,
                v.idVehiculo AS id_vehiculo,
                v.numero_vehiculo AS numero_vehiculo,
                v.placa AS placa,
                COALESCE(CONCAT(i.primerApellido, ' ', i.primerNombre), p.idProfesor) AS instructor,
                p.hora_salida AS salida
            FROM cond_alumnos_practicas p
            LEFT JOIN alumnos e ON p.idalumno = e.idAlumno
            LEFT JOIN vehiculos v ON p.idvehiculo = v.idVehiculo
            LEFT JOIN profesores i ON p.idProfesor = i.idProfesor
            WHERE p.ensalida = 1 AND p.cancelado = 0",

            @"CREATE OR REPLACE VIEW v_alerta_mantenimiento AS
            SELECT
                v.idVehiculo AS id_vehiculo,
                v.numero_vehiculo AS numero_vehiculo,
                v.placa AS placa
            FROM vehiculos v
            INNER JOIN vehiculos_operacion vo ON vo.idVehiculo = v.idVehiculo
            WHERE v.activo = 1 AND vo.estado_mecanico != 'OPERATIVO'"
        };

        foreach (var cmd in schemaCommands)
        {
            try { await db.Database.ExecuteSqlRawAsync(cmd); }
            catch { /* Ignorar errores de columnas/índices ya existentes */ }
        }

        // -------------------------------------------------------------------------
        // 5. SEEDING DE DATOS MAESTROS (BOOTSTRAP)
        // -------------------------------------------------------------------------
        try
        {
            // Admin Bootstrap
            await db.Database.ExecuteSqlRawAsync(@"
                INSERT IGNORE INTO usuarios_web (usuario, password, salida, ingreso, activo, asistencia, esRrhh)
                VALUES ('admin', '$2a$11$qR7iXv2D1K5z5F.h39.SDe6D1O1E1O1O1O1O1O1O1O1O1O1O1O1O1O1O', 1, 1, 1, 0, 1)");
            
            // Tipos de Licencia
            await db.Database.ExecuteSqlRawAsync(@"INSERT IGNORE INTO tipo_licencia (id_tipo, codigo, descripcion, activo) VALUES (1, 'C', 'CONDUCCIÓN NO PROFESIONAL TIPO C', 1)");
            await db.Database.ExecuteSqlRawAsync(@"INSERT IGNORE INTO tipo_licencia (id_tipo, codigo, descripcion, activo) VALUES (2, 'D', 'CONDUCCIÓN PROFESIONAL TIPO D', 1)");
            await db.Database.ExecuteSqlRawAsync(@"INSERT IGNORE INTO tipo_licencia (id_tipo, codigo, descripcion, activo) VALUES (3, 'E', 'CONDUCCIÓN PROFESIONAL TIPO E', 1)");
        }
        catch { /* Ignorar fallos de seeding */ }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Schema Healer ADVERTENCIA: {ex.Message}");
    }
}



app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ISTPET API v1");
    c.RoutePrefix = "swagger"; // Asegura acceso en /swagger
});

app.UseMiddleware<backend.Middleware.ErrorHandlingMiddleware>();

// CORS: permisivo en desarrollo, restrictivo en producción
if (app.Environment.IsDevelopment())
    app.UseCors("DevelopmentPolicy");
else
    app.UseCors("ProductionPolicy");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Health checks con respuesta JSON detallada
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (ctx, report) =>
    {
        ctx.Response.ContentType = "application/json";
        var result = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration_ms = e.Value.Duration.TotalMilliseconds
            })
        };
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(result));
    }
});

app.MapControllers();
app.Run();
