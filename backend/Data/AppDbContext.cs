using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data
{
    /**
     * ISTPET Enterprise DbContext: Grand Mapping 2026 Edition
     * Aligned with SQL snake_case schema across all 11 tables.
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
        public DbSet<SyncLog> SyncLogs { get; set; }

        // SQL VIEWS (Read-only monitoring)
        public DbSet<ClaseActiva> ClasesActivas { get; set; }
        public DbSet<AlertaMantenimiento> AlertasMantenimiento { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. SEGURIDAD (usuarios)
            modelBuilder.Entity<Usuario>(entity => {
                entity.ToTable("usuarios");
                entity.HasKey(e => e.Id_Usuario);
                entity.Property(e => e.Id_Usuario).HasColumnName("id_usuario");
                entity.Property(e => e.UsuarioLogin).HasColumnName("usuario");
                entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
                entity.Property(e => e.Rol).HasColumnName("rol").HasDefaultValue("guardia");
                entity.Property(e => e.NombreCompleto).HasColumnName("nombre_completo");
                entity.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(true);
            });

            // 2. PARAMETRIZACIÓN (tipo_licencia)
            modelBuilder.Entity<TipoLicencia>(entity => {
                entity.ToTable("tipo_licencia");
                entity.HasKey(e => e.Id_Tipo);
                entity.Property(e => e.Id_Tipo).HasColumnName("id_tipo");
                entity.Property(e => e.Codigo).HasColumnName("codigo");
            });

            // 3. RECURSOS HUMANOS (instructores)
            modelBuilder.Entity<Instructor>(entity => {
                entity.ToTable("instructores");
                entity.HasKey(e => e.Id_Instructor);
                entity.Property(e => e.Id_Instructor).HasColumnName("id_instructor");
                entity.Property(e => e.Cedula).HasColumnName("cedula");
                entity.Property(e => e.Nombres).HasColumnName("nombres");
                entity.Property(e => e.Apellidos).HasColumnName("apellidos");
                entity.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(true);
            });

            modelBuilder.Entity<InstructorLicencia>(entity => {
                entity.ToTable("instructor_licencias");
                entity.HasKey(e => new { e.Id_Instructor, e.Id_Tipo_Licencia });
            });

            // 4. GESTIÓN DE FLOTA (vehiculos, mantenimientos)
            modelBuilder.Entity<Vehiculo>(entity => {
                entity.ToTable("vehiculos");
                entity.HasKey(e => e.Id_Vehiculo);
                entity.Property(e => e.Id_Vehiculo).HasColumnName("id_vehiculo");
                entity.Property(e => e.NumeroVehiculo).HasColumnName("numero_vehiculo");
                entity.Property(e => e.Placa).HasColumnName("placa");
                entity.Property(e => e.KmActual).HasColumnName("km_actual");
                entity.Property(e => e.KmProximoMantenimiento).HasColumnName("km_proximo_mantenimiento");
                entity.Property(e => e.EstadoMecanico).HasColumnName("estado_mecanico").HasDefaultValue("OPERATIVO");
                entity.Property(e => e.IdTipoLicencia).HasColumnName("id_tipo_licencia");
                entity.Property(e => e.IdInstructorFijo).HasColumnName("id_instructor_fijo");
                entity.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(true);
            });

            modelBuilder.Entity<Mantenimiento>(entity => {
                entity.ToTable("mantenimientos");
                entity.HasKey(e => e.Id_Mantenimiento);
                entity.Property(e => e.Id_Mantenimiento).HasColumnName("id_mantenimiento");
                entity.Property(e => e.Id_Vehiculo).HasColumnName("id_vehiculo");
                entity.Property(e => e.Fecha).HasColumnName("fecha");
                entity.Property(e => e.KmRealizado).HasColumnName("km_realizado");
                entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            });

            // 5. ACADÉMICO (estudiantes, cursos, matriculas)
            modelBuilder.Entity<Estudiante>(entity => {
                entity.ToTable("estudiantes");
                entity.HasKey(e => e.Cedula);
                entity.Property(e => e.Cedula).HasColumnName("cedula");
                entity.Property(e => e.Nombres).HasColumnName("nombres");
                entity.Property(e => e.Apellidos).HasColumnName("apellidos");
                entity.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(true);
            });

            modelBuilder.Entity<Curso>(entity => {
                entity.ToTable("cursos");
                entity.HasKey(e => e.Id_Curso);
                entity.Property(e => e.Id_Curso).HasColumnName("id_curso");
                entity.Property(e => e.IdTipoLicencia).HasColumnName("id_tipo_licencia");
                entity.Property(e => e.Nombre).HasColumnName("nombre");
                entity.Property(e => e.Nivel).HasColumnName("nivel");
                entity.Property(e => e.Paralelo).HasColumnName("paralelo");
                entity.Property(e => e.Jornada).HasColumnName("jornada");
                entity.Property(e => e.Periodo).HasColumnName("periodo");
                entity.Property(e => e.FechaInicio).HasColumnName("fecha_inicio");
                entity.Property(e => e.FechaFin).HasColumnName("fecha_fin");
                entity.Property(e => e.CupoMaximo).HasColumnName("cupo_maximo");
                entity.Property(e => e.CuposDisponibles).HasColumnName("cupos_disponibles");
                entity.Property(e => e.HorasPracticaTotal).HasColumnName("horas_practica_total");
            });

            modelBuilder.Entity<Matricula>(entity => {
                entity.ToTable("matriculas");
                entity.HasKey(e => e.Id_Matricula);
                entity.Property(e => e.Id_Matricula).HasColumnName("id_matricula");
                entity.Property(e => e.CedulaEstudiante).HasColumnName("cedula_estudiante");
                entity.Property(e => e.IdCurso).HasColumnName("id_curso");
                entity.Property(e => e.FechaMatricula).HasColumnName("fecha_matricula");
                entity.Property(e => e.HorasCompletadas).HasColumnName("horas_completadas");
                entity.Property(e => e.Estado).HasColumnName("estado");
            });

            // 6. CONTROL LOGÍSTICO (registros_salida, registros_llegada)
            modelBuilder.Entity<RegistroSalida>(entity => {
                entity.ToTable("registros_salida");
                entity.HasKey(e => e.Id_Registro);
                entity.Property(e => e.Id_Registro).HasColumnName("id_registro");
                entity.Property(e => e.IdMatricula).HasColumnName("id_matricula");
                entity.Property(e => e.IdVehiculo).HasColumnName("id_vehiculo");
                entity.Property(e => e.IdInstructor).HasColumnName("id_instructor");
                entity.Property(e => e.FechaHoraSalida).HasColumnName("fecha_hora_salida");
                entity.Property(e => e.KmSalida).HasColumnName("km_salida");
                entity.Property(e => e.ObservacionesSalida).HasColumnName("observaciones_salida");
                entity.Property(e => e.RegistradoPor).HasColumnName("registrado_por");
            });

            modelBuilder.Entity<RegistroLlegada>(entity => {
                entity.ToTable("registros_llegada");
                entity.HasKey(e => e.Id_Llegada);
                entity.Property(e => e.Id_Llegada).HasColumnName("id_llegada");
                entity.Property(e => e.IdRegistro).HasColumnName("id_registro");
                entity.Property(e => e.FechaHoraLlegada).HasColumnName("fecha_hora_llegada");
                entity.Property(e => e.KmLlegada).HasColumnName("km_llegada");
                entity.Property(e => e.ObservacionesLlegada).HasColumnName("observaciones_llegada");
                entity.Property(e => e.RegistradoPor).HasColumnName("registrado_por");
            });

            // SQL VIEWS MAPPING
            modelBuilder.Entity<ClaseActiva>(entity => {
                entity.ToView("v_clases_activas").HasNoKey();
                entity.Property(e => e.Id_Registro).HasColumnName("id_registro");
                entity.Property(e => e.Id_Vehiculo).HasColumnName("id_vehiculo");
                entity.Property(e => e.Cedula).HasColumnName("cedula");
                entity.Property(e => e.Estudiante).HasColumnName("estudiante");
                entity.Property(e => e.Placa).HasColumnName("placa");
                entity.Property(e => e.NumeroVehiculo).HasColumnName("numero_vehiculo");
                entity.Property(e => e.Instructor).HasColumnName("instructor");
                entity.Property(e => e.Salida).HasColumnName("salida");
            });

            modelBuilder.Entity<AlertaMantenimiento>(entity => {
                entity.ToView("v_alerta_mantenimiento").HasNoKey();
                entity.Property(e => e.Id_Vehiculo).HasColumnName("id_vehiculo");
                entity.Property(e => e.Km_Restantes).HasColumnName("km_restantes");
            });

            // AUDIT LOGS
            modelBuilder.Entity<SyncLog>(entity => {
                entity.ToTable("sync_logs");
                entity.HasKey(e => e.Id_Log);
                entity.Property(e => e.Id_Log).HasColumnName("id_log");
            });
        }
    }
}
