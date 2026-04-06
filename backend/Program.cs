using backend.Data;
using backend.Services.Interfaces;
using backend.Services.Implementations;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(); 

// Habilitar Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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


// 🤖 AUTOMATIZACIÓN: Registrar AutoMapper
builder.Services.AddAutoMapper(typeof(backend.Mappings.MappingProfile));

// Configurar DbContext con MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

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

app.MapControllers(); 

app.Run();
