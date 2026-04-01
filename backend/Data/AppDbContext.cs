using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data
{
    /**
     * ISTPET Enterprise DbContext
     * Configured for MySQL with Professional Fluent API mappings.
     * Includes Mapping for SQL Views and Sync Auditing logs.
     */
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<TipoLicencia> TipoLicencias { get; set; }
        public DbSet<Instructor> Instructores { get; set; }
        public DbSet<InstructorLicencia> InstructorLicencias { get; set; }
        public DbSet<Vehiculo> Vehiculos { get; set; }
        public DbSet<Mantenimiento> Mantenimientos { get; set; }
        public DbSet<Curso> Cursos { get; set; }
        public DbSet<Estudiante> Estudiantes { get; set; }
        public DbSet<Matricula> Matriculas { get; set; }
        public DbSet<RegistroSalida> RegistrosSalida { get; set; }
        public DbSet<RegistroLlegada> RegistrosLlegada { get; set; }
        
        // SISTEMA DE SEGURIDAD Y AUDITORÍA
        public DbSet<SyncLog> SyncLogs { get; set; }

        // SQL VIEWS (Read-only monitoring)
        public DbSet<ClaseActiva> ClasesActivas { get; set; }
        public DbSet<AlertaMantenimiento> AlertasMantenimiento { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Audit Logs mapping
            modelBuilder.Entity<SyncLog>(entity => {
                entity.ToTable("sync_logs");
                entity.HasKey(e => e.Id_Log);
                entity.Property(e => e.Id_Log).HasColumnName("id_log");
            });

            // SQL VIEWS MAPPING
            modelBuilder.Entity<ClaseActiva>().ToView("v_clases_activas").HasNoKey();
            modelBuilder.Entity<AlertaMantenimiento>().ToView("v_alerta_mantenimiento").HasNoKey();

            // --- 1. SEGURIDAD ---
            modelBuilder.Entity<Usuario>(entity => {
                entity.ToTable("usuarios");
                entity.HasKey(e => e.Id_Usuario);
                entity.Property(e => e.Id_Usuario).HasColumnName("id_usuario");
                entity.Property(e => e.UsuarioLogin).HasColumnName("usuario");
                entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
                entity.Property(e => e.Rol).HasColumnName("rol").HasDefaultValue("guardia");
                entity.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(true);
            });

            // --- 2. PARAMETRIZACIÓN ---
            modelBuilder.Entity<TipoLicencia>(entity => {
                entity.ToTable("tipo_licencia");
                entity.HasKey(e => e.Id_Tipo);
                entity.Property(e => e.Id_Tipo).HasColumnName("id_tipo");
                entity.Property(e => e.Codigo).HasColumnName("codigo");
                entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            });

            // --- 3. RECURSOS HUMANOS ---
            modelBuilder.Entity<Instructor>(entity => {
                entity.ToTable("instructores");
                entity.HasKey(e => e.Id_Instructor);
                entity.Property(e => e.Id_Instructor).HasColumnName("id_instructor");
            });

            modelBuilder.Entity<InstructorLicencia>(entity => {
                entity.ToTable("instructor_licencias");
                entity.HasKey(e => new { e.Id_Instructor, e.Id_Tipo_Licencia });
            });

            // --- 4. GESTIÓN DE FLOTA ---
            modelBuilder.Entity<Vehiculo>(entity => {
                entity.ToTable("vehiculos");
                entity.HasKey(e => e.Id_Vehiculo);
                entity.Property(e => e.Id_Vehiculo).HasColumnName("id_vehiculo");
                entity.Property(e => e.NumeroVehiculo).HasColumnName("numero_vehiculo");
                entity.Property(e => e.Placa).HasColumnName("placa");
                entity.Property(e => e.KmActual).HasColumnName("km_actual");
                entity.Property(e => e.EstadoMecanico).HasColumnName("estado_mecanico").HasDefaultValue("OPERATIVO");
                entity.Property(e => e.IdTipoLicencia).HasColumnName("id_tipo_licencia");
                entity.Property(e => e.IdInstructorFijo).HasColumnName("id_instructor_fijo");
            });

            modelBuilder.Entity<Mantenimiento>(entity => {
                entity.ToTable("mantenimientos");
                entity.HasKey(e => e.Id_Mantenimiento);
                entity.Property(e => e.Id_Mantenimiento).HasColumnName("id_mantenimiento");
                entity.Property(e => e.Id_Vehiculo).HasColumnName("id_vehiculo");
                entity.Property(e => e.KmRealizado).HasColumnName("km_realizado");
            });

            // --- 5. ACADÉMICO ---
            modelBuilder.Entity<Curso>(entity => {
                entity.ToTable("cursos");
                entity.HasKey(e => e.Id_Curso);
                entity.Property(e => e.Id_Curso).HasColumnName("id_curso");
                entity.Property(e => e.IdTipoLicencia).HasColumnName("id_tipo_licencia");
            });

            modelBuilder.Entity<Estudiante>(entity => {
                entity.ToTable("estudiantes");
                entity.HasKey(e => e.Cedula);
                entity.Property(e => e.Cedula).HasColumnName("cedula");
            });

            modelBuilder.Entity<Matricula>(entity => {
                entity.ToTable("matriculas");
                entity.HasKey(e => e.Id_Matricula);
                entity.Property(e => e.Id_Matricula).HasColumnName("id_matricula");
                entity.Property(e => e.CedulaEstudiante).HasColumnName("cedula_estudiante");
                entity.Property(e => e.IdCurso).HasColumnName("id_curso");
            });

            // --- 6. CONTROL LOGÍSTICO ---
            modelBuilder.Entity<RegistroSalida>(entity => {
                entity.ToTable("registros_salida");
                entity.HasKey(e => e.Id_Registro);
                entity.Property(e => e.Id_Registro).HasColumnName("id_registro");
                entity.Property(e => e.IdMatricula).HasColumnName("id_matricula");
                entity.Property(e => e.IdVehiculo).HasColumnName("id_vehiculo");
                entity.Property(e => e.IdInstructor).HasColumnName("id_instructor");
            });

            modelBuilder.Entity<RegistroLlegada>(entity => {
                entity.ToTable("registros_llegada");
                entity.HasKey(e => e.Id_Llegada);
                entity.Property(e => e.Id_Llegada).HasColumnName("id_llegada");
                entity.Property(e => e.IdRegistro).HasColumnName("id_registro");
                entity.Property(e => e.KmLlegada).HasColumnName("km_llegada");
            });
        }
    }
}
