using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<TipoLicencia> TipoLicencias { get; set; }
        public DbSet<Instructor> Instructores { get; set; }
        public DbSet<Vehiculo> Vehiculos { get; set; }
        public DbSet<Mantenimiento> Mantenimientos { get; set; }
        public DbSet<Curso> Cursos { get; set; }
        public DbSet<Estudiante> Estudiantes { get; set; }
        public DbSet<Matricula> Matriculas { get; set; }
        public DbSet<Practica> Practicas { get; set; }
        public DbSet<Asignacion> Asignaciones { get; set; }
        public DbSet<AsignacionInstructorVehiculo> AsignacionesInstructores { get; set; }
        public DbSet<CategoriaVehiculo> CategoriasVehiculos { get; set; }
        public DbSet<VehiculoOperacion> VehiculosOperaciones { get; set; }
        public DbSet<HorarioAlumno> HorariosAlumnos { get; set; }
        public DbSet<CategoriaExamenConduccion> CategoriasExamenes { get; set; }
        public DbSet<MatriculaExamenConduccion> MatriculasExamenesConduccion { get; set; }
        public DbSet<PracticaHorarioAlumno> PracticasHorarios { get; set; }
        public DbSet<FechaHorario> FechasHorarios { get; set; }
        public DbSet<HorarioProfesor> HorariosProfesores { get; set; }

        public DbSet<ClaseActiva> ClasesActivas { get; set; }
        public DbSet<AlertaMantenimiento> AlertasMantenimiento { get; set; }

        public DbSet<Carrera> Carreras { get; set; }
        public DbSet<Modalidad> Modalidades { get; set; }
        public DbSet<Institucion> Instituciones { get; set; }
        public DbSet<Periodo> Periodos { get; set; }
        public DbSet<Seccion> Secciones { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. SEGURIDAD (usuarios_web)
            modelBuilder.Entity<Usuario>(entity => {
                entity.ToTable("usuarios_web");
                entity.HasKey(e => e.usuario);
            });

            // Mirrors de SIGAFI (Master Data)
            modelBuilder.Entity<Carrera>(entity => {
                entity.ToTable("carreras");
                entity.HasKey(e => e.idCarrera);
                entity.Property(e => e.NombreCarrera).HasColumnName("Carrera");
            });

            modelBuilder.Entity<Modalidad>(entity => {
                entity.ToTable("modalidades");
                entity.HasKey(e => e.idModalidad);
            });

            modelBuilder.Entity<Institucion>(entity => {
                entity.ToTable("instituciones");
                entity.HasKey(e => e.idInstitucion);
                entity.Property(e => e.NombreInstitucion).HasColumnName("Institucion");
            });

            // 2. TIPO LICENCIA
            modelBuilder.Entity<TipoLicencia>(entity => {
                entity.ToTable("tipo_licencia");
                entity.HasKey(e => e.id_tipo);
                entity.HasIndex(e => e.id_categoria_sigafi).IsUnique();
            });

            // 3. INSTRUCTORES (Mirroring 'profesores' schema)
            modelBuilder.Entity<Instructor>(entity => {
                entity.ToTable("profesores");
                entity.HasKey(e => e.idProfesor);
                entity.Property(e => e.idProfesor).HasColumnName("idProfesor");
                entity.Property(e => e.primerNombre).HasColumnName("primerNombre");
                entity.Property(e => e.segundoNombre).HasColumnName("segundoNombre");
                entity.Property(e => e.primerApellido).HasColumnName("primerApellido");
                entity.Property(e => e.segundoApellido).HasColumnName("segundoApellido");
                entity.Property(e => e.nombres).HasColumnName("nombres");
                entity.Property(e => e.apellidos).HasColumnName("apellidos");
            });

            // 4. VEHÍCULOS (Mirroring 'vehiculos' schema)
            modelBuilder.Entity<Vehiculo>(entity => {
                entity.ToTable("vehiculos");
                entity.HasKey(e => e.idVehiculo);
                entity.Property(e => e.idVehiculo).HasColumnName("idVehiculo");
                entity.Property(e => e.numero_vehiculo).HasColumnName("numero_vehiculo");
                entity.Property(e => e.placa).HasColumnName("placa");
                entity.Property(e => e.marca).HasColumnName("marca");
                entity.Property(e => e.modelo).HasColumnName("modelo");
                entity.Property(e => e.anio).HasColumnName("anio");
                entity.Property(e => e.chasis).HasColumnName("chasis");
                entity.Property(e => e.motor).HasColumnName("motor");
                entity.Property(e => e.observacion).HasColumnName("observacion");

                // RELACIÓN 1:1 CON VEHÍCULO OPERACIÓN
                entity.HasOne(v => v.Operacion)
                      .WithOne()
                      .HasForeignKey<VehiculoOperacion>(vo => vo.idVehiculo);
            });

            // 5. ESTUDIANTES (Mirroring 'alumnos' schema)
            modelBuilder.Entity<Estudiante>(entity => {
                entity.ToTable("alumnos");
                entity.HasKey(e => e.idAlumno);
                entity.Property(e => e.idAlumno).HasColumnName("idAlumno");
                entity.Property(e => e.primerNombre).HasColumnName("primerNombre");
                entity.Property(e => e.segundoNombre).HasColumnName("segundoNombre");
                entity.Property(e => e.apellidoPaterno).HasColumnName("apellidoPaterno");
                entity.Property(e => e.apellidoMaterno).HasColumnName("apellidoMaterno");
                entity.Property(e => e.idPeriodo).HasColumnName("idPeriodo");
                entity.Property(e => e.idNivel).HasColumnName("idNivel");
                entity.Property(e => e.idSeccion).HasColumnName("idSeccion");
                entity.Property(e => e.idModalidad).HasColumnName("idModalidad");
            });

            // 6. MATRÍCULAS (Mirroring 'matriculas' schema)
            modelBuilder.Entity<Matricula>(entity => {
                entity.ToTable("matriculas");
                entity.HasKey(e => e.idMatricula);
                entity.Property(e => e.idMatricula).HasColumnName("idMatricula");
                entity.Property(e => e.idAlumno).HasColumnName("idAlumno");
                entity.Property(e => e.idNivel).HasColumnName("idNivel");
                entity.Property(e => e.idSeccion).HasColumnName("idSeccion");
                entity.Property(e => e.idModalidad).HasColumnName("idModalidad");
                entity.Property(e => e.idPeriodo).HasColumnName("idPeriodo");
                entity.Property(e => e.paralelo).HasColumnName("paralelo");
                entity.Property(e => e.valida).HasColumnName("valida");
            });

            // 8. CURSOS
            modelBuilder.Entity<Curso>(entity => {
                entity.ToTable("cursos");
                entity.HasKey(e => e.idNivel);
                entity.Property(e => e.idNivel).HasColumnName("idNivel");
                entity.Property(e => e.idCarrera).HasColumnName("idCarrera");
                entity.Property(e => e.Nivel).HasColumnName("Nivel");
            });

            // 7. PRÁCTICAS (Mirroring 'cond_alumnos_practicas' schema)
            modelBuilder.Entity<Practica>(entity => {
                entity.ToTable("cond_alumnos_practicas");
                entity.HasKey(e => e.idPractica);
                entity.Property(e => e.idPractica).HasColumnName("idPractica");
                entity.Property(e => e.idalumno).HasColumnName("idalumno");
                entity.Property(e => e.idvehiculo).HasColumnName("idvehiculo");
                entity.Property(e => e.idProfesor).HasColumnName("idProfesor");
                entity.Property(e => e.idPeriodo).HasColumnName("idPeriodo");
                entity.Property(e => e.dia).HasColumnName("dia");
                entity.Property(e => e.fecha).HasColumnName("fecha");
                entity.Property(e => e.hora_salida).HasColumnName("hora_salida");
                entity.Property(e => e.hora_llegada).HasColumnName("hora_llegada");
                entity.Property(e => e.tiempo).HasColumnName("tiempo");
                entity.Property(e => e.ensalida).HasColumnName("ensalida");
                entity.Property(e => e.verificada).HasColumnName("verificada");
                entity.Property(e => e.user_asigna).HasColumnName("user_asigna");
                entity.Property(e => e.user_llegada).HasColumnName("user_llegada");
                
                // Detectar modo de base de datos para cancelado
                var dbMode = Environment.GetEnvironmentVariable("DATABASE_MODE") ?? "Mirror";
                if (dbMode.Equals("Direct", StringComparison.OrdinalIgnoreCase))
                {
                    entity.Ignore(e => e.cancelado);
                }
                else
                {
                    entity.Property(e => e.cancelado).HasColumnName("cancelado");
                }
            });

            // 9. ASIGNACIONES (Mirroring 'cond_alumnos_vehiculos' schema)
            modelBuilder.Entity<Asignacion>(entity => {
                entity.ToTable("cond_alumnos_vehiculos");
                entity.HasKey(e => e.idAsignacion);
            });

            modelBuilder.Entity<VehiculoOperacion>(entity => {
                entity.ToTable("vehiculos_operacion");
                entity.HasKey(e => e.idVehiculo);
                entity.Property(e => e.idVehiculo).ValueGeneratedNever();
            });


            // 10. PERIODOS
            modelBuilder.Entity<Periodo>(entity => {
                entity.ToTable("periodos");
                entity.HasKey(e => e.idPeriodo);
            });

            // 11. SECCIONES (sigafi_es.secciones: idSeccion, seccion, sufijo)
            modelBuilder.Entity<Seccion>(entity => {
                entity.ToTable("secciones");
                entity.HasKey(e => e.idSeccion);
                entity.Property(e => e.seccion).HasColumnName("seccion");
                entity.Property(e => e.sufijo).HasColumnName("sufijo");
            });

            modelBuilder.Entity<AsignacionInstructorVehiculo>(entity => {
                entity.ToTable("asignacion_instructores_vehiculos");
                entity.HasKey(e => e.idAsignacion);
            });

            modelBuilder.Entity<CategoriaVehiculo>(entity => {
                entity.ToTable("categoria_vehiculos");
                entity.HasKey(e => e.idCategoria);
            });

            modelBuilder.Entity<HorarioAlumno>(entity => {
                entity.ToTable("cond_alumnos_horarios");
                entity.HasKey(e => e.idAsignacionHorario);
                entity.Property(e => e.observacion).HasColumnType("text");
            });

            modelBuilder.Entity<CategoriaExamenConduccion>(entity => {
                entity.ToTable("categorias_examenes_conduccion");
                entity.HasKey(e => e.IdCategoria);
            });

            modelBuilder.Entity<MatriculaExamenConduccion>(entity => {
                entity.ToTable("matriculas_examen_conduccion");
                entity.HasKey(e => new { e.idMatricula, e.IdCategoria });
                entity.HasOne<Matricula>().WithMany().HasForeignKey(e => e.idMatricula);
                entity.HasOne<CategoriaExamenConduccion>().WithMany().HasForeignKey(e => e.IdCategoria);
            });

            modelBuilder.Entity<PracticaHorarioAlumno>(entity => {
                entity.ToTable("cond_practicas_horarios_alumnos");
                entity.HasKey(e => new { e.idPractica, e.idAsignacionHorario });
            });

            modelBuilder.Entity<FechaHorario>(entity => {
                entity.ToTable("fechas_horarios");
                entity.HasKey(e => e.idFecha);
            });

            modelBuilder.Entity<HorarioProfesor>(entity => {
                entity.ToTable("horario_profesores");
                entity.HasKey(e => e.idHorario);
            });



            // AUDITORÍA
            modelBuilder.Entity<AuditLog>(entity => {
                entity.ToTable("audit_logs");
                entity.HasKey(e => e.id);
            });

            // VIEWS
            modelBuilder.Entity<ClaseActiva>(entity => {
                entity.ToView("v_clases_activas").HasNoKey();
                entity.Property(e => e.idPractica).HasColumnName("id_registro");
                entity.Property(e => e.idVehiculo).HasColumnName("id_vehiculo");
                entity.Property(e => e.idAlumno).HasColumnName("idAlumno");
                entity.Property(e => e.numeroVehiculo).HasColumnName("numero_vehiculo");
            });
        }
    }
}
