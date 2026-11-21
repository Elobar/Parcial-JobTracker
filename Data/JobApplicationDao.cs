using System.Globalization;
using JobTracker.Models;
using Microsoft.Data.Sqlite;

namespace JobTracker.Data;

/// <summary>
/// DAO (Data Access Object): único lugar del programa que escribe SQL.
/// Aquí viven las cuatro operaciones CRUD usando ADO.NET con comandos parametrizados.
/// </summary>
public class JobApplicationDao
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string Columns = "Id, Company, Position, ApplicationDate, Link, Salary, Currency, Status, Notes";

    private readonly DatabaseContext _db;

    public JobApplicationDao(DatabaseContext db) => _db = db;

    // ---------------------------------------------------------------- CREATE
    public int Insert(JobApplication application)
    {
        using var connection = _db.CreateConnection();
        connection.Open();

        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                INSERT INTO Applications (Company, Position, ApplicationDate, Link, Salary, Currency, Status, Notes)
                VALUES ($company, $position, $date, $link, $salary, $currency, $status, $notes);";
            AddParameters(command, application);
            command.ExecuteNonQuery();
        }

        using var idCommand = connection.CreateCommand();
        idCommand.CommandText = "SELECT last_insert_rowid();";
        var id = Convert.ToInt32(idCommand.ExecuteScalar());

        application.Id = id;
        return id;
    }

    // ------------------------------------------------------------------ READ
    public List<JobApplication> GetAll()
    {
        var result = new List<JobApplication>();

        using var connection = _db.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {Columns} FROM Applications ORDER BY ApplicationDate DESC, Id DESC;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(Map(reader));
        }

        return result;
    }

    public JobApplication? GetById(int id)
    {
        using var connection = _db.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {Columns} FROM Applications WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);

        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    // ---------------------------------------------------------------- UPDATE
    public bool Update(JobApplication application)
    {
        using var connection = _db.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Applications
               SET Company = $company, Position = $position, ApplicationDate = $date,
                   Link = $link, Salary = $salary, Currency = $currency,
                   Status = $status, Notes = $notes
             WHERE Id = $id;";
        AddParameters(command, application);
        command.Parameters.AddWithValue("$id", application.Id);

        return command.ExecuteNonQuery() > 0;
    }

    // ---------------------------------------------------------------- DELETE
    public bool Delete(int id)
    {
        using var connection = _db.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Applications WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);

        return command.ExecuteNonQuery() > 0;
    }

    // ------------------------------------------------------------- helpers
    private static void AddParameters(SqliteCommand command, JobApplication app)
    {
        command.Parameters.AddWithValue("$company", app.Company);
        command.Parameters.AddWithValue("$position", app.Position);
        command.Parameters.AddWithValue("$date", app.ApplicationDate.ToString(DateFormat, CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$link", (object?)app.Link ?? DBNull.Value);
        // Microsoft.Data.Sqlite guarda 'decimal' como TEXT; lo convertimos a double para usar la columna REAL.
        command.Parameters.AddWithValue("$salary", app.Salary.HasValue ? (object)(double)app.Salary.Value : DBNull.Value);
        command.Parameters.AddWithValue("$currency", app.Currency);
        command.Parameters.AddWithValue("$status", app.Status.ToString());
        command.Parameters.AddWithValue("$notes", (object?)app.Notes ?? DBNull.Value);
    }

    /// <summary>Convierte la fila actual del lector en un objeto JobApplication.</summary>
    private static JobApplication Map(SqliteDataReader reader)
    {
        var date = DateTime.ParseExact(reader.GetString(3), DateFormat, CultureInfo.InvariantCulture);
        decimal? salary = reader.IsDBNull(5) ? null : (decimal)reader.GetDouble(5);
        var status = Enum.TryParse<ApplicationStatus>(reader.GetString(7), out var parsed)
            ? parsed
            : ApplicationStatus.Applied;

        return new JobApplication(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            date,
            reader.IsDBNull(4) ? null : reader.GetString(4),
            salary,
            reader.GetString(6),
            status,
            reader.IsDBNull(8) ? null : reader.GetString(8));
    }
}
