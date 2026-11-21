using Microsoft.Data.Sqlite;

namespace JobTracker.Data;

/// <summary>
/// Se encarga de DOS cosas: saber dónde está el archivo SQLite y entregar conexiones.
/// También crea la tabla la primera vez que se ejecuta el programa.
/// </summary>
public class DatabaseContext
{
    private readonly string _connectionString;

    /// <summary>Ruta completa del archivo .db (se muestra en el pie de la ventana).</summary>
    public string DatabasePath { get; }

    public DatabaseContext(string? databasePath = null)
    {
        // El archivo se guarda en la carpeta de datos del usuario (no en bin/), así los
        // registros sobreviven a recompilar el proyecto y no se suben a GitHub.
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JobTracker");
        Directory.CreateDirectory(folder);

        DatabasePath = databasePath ?? Path.Combine(folder, "jobtracker.db");
        _connectionString = new SqliteConnectionStringBuilder { DataSource = DatabasePath }.ToString();
    }

    /// <summary>Crea una conexión nueva. Quien la use debe cerrarla (using).</summary>
    public SqliteConnection CreateConnection() => new SqliteConnection(_connectionString);

    /// <summary>Crea la tabla si no existe. Es seguro llamarlo en cada arranque.</summary>
    public void Initialize()
    {
        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Applications (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                Company         TEXT NOT NULL,
                Position        TEXT NOT NULL,
                ApplicationDate TEXT NOT NULL,   -- formato yyyy-MM-dd
                Link            TEXT,
                Salary          REAL,
                Currency        TEXT NOT NULL DEFAULT 'COP',
                Status          TEXT NOT NULL,   -- nombre del enum ApplicationStatus
                Notes           TEXT
            );";
        command.ExecuteNonQuery();

        // TODO(FUTURO): cuando agregues columnas nuevas (Source, AiSummary, MatchScore...)
        // usa ALTER TABLE Applications ADD COLUMN ... protegido con try/catch, o una tabla
        // SchemaVersion para llevar el control de migraciones.
    }
}
