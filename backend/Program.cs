//Deploy
using backend.Services.Implementations;
using backend.Services.Interfaces;
using backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// 🛡️ CONFIGURACIÓN DE SEGURIDAD JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"] ?? "SUPER_SECRET_KEY_PROD_2026_DEFAULT");

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
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Habilitar Swagger con Soporte para JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "ISTPET Logistics API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
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
            new string[] {}
        }
    });
});

/**
 * 🛠️ CONFIGURACIÓN DE CORS (DESARROLLO)
 */
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

// Inyectar servicios (Arquitectura Escalable)
builder.Services.AddScoped<IVehiculoService, SqlVehiculoService>();
builder.Services.AddScoped<IEstudianteService, SqlEstudianteService>();
builder.Services.AddScoped<ILogisticaService, SqlLogisticaService>();
builder.Services.AddScoped<IDataSyncService, DataSyncService>();
builder.Services.AddScoped<ICentralStudentProvider, SqlCentralStudentProvider>();
builder.Services.AddScoped<backend.Services.Interfaces.ISigafiReportService, backend.Services.Implementations.SigafiReportService>();


// AUTOMATIZACIÓN: Registrar AutoMapper
builder.Services.AddAutoMapper(typeof(backend.Mappings.MappingProfile));

// Configurar DbContext con MySQL / TiDB Cloud (Prioridad: Nube > Local)
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
                    ?? builder.Configuration.GetConnectionString("DefaultConnection");
var sigafiConnectionString = builder.Configuration.GetConnectionString("SigafiConnection");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("❌ Error: No se encontró la cadena de conexión (DATABASE_URL).");
}
if (string.IsNullOrWhiteSpace(sigafiConnectionString))
{
    throw new InvalidOperationException("❌ Error: No se encontró ConnectionStrings:SigafiConnection para el puente de lectura SIGAFI.");
}

// 🛡️ ADAPTADOR PARA TiDB CLOUD (SSL es obligatorio)
if (connectionString.Contains("tidbcloud.com") && !connectionString.Contains("SslMode"))
{
    connectionString += ";SslMode=Required;";
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// 🛡️ CONFIGURACION DE PUERTO PARA RENDER (Solo si existe la variable PORT)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://*:{port}");
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 🛡️ Middleware de Manejo de Errores Profesional
app.UseMiddleware<backend.Middleware.ErrorHandlingMiddleware>();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
