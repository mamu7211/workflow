using System.Text.RegularExpressions;
using Npgsql;

namespace Workflow.Engine.Activities;

public sealed partial class DatabaseQueryActivity : ActivityBase
{
    public override string Type => "DatabaseQuery";

    [GeneratedRegex(@"^\s*SELECT\s", RegexOptions.IgnoreCase)]
    private static partial Regex SelectPattern();

    public override async Task<ActivityResult> ExecuteAsync(ActivityContext context, CancellationToken cancellationToken = default)
    {
        var connectionString = context.GetProperty<string>("connectionString");
        var query = context.GetProperty<string>("query");
        var parameters = context.GetProperty<Dictionary<string, object?>>("parameters");

        if (string.IsNullOrEmpty(connectionString))
            return ActivityResult.Faulted("Property 'connectionString' is required.");

        if (string.IsNullOrEmpty(query))
            return ActivityResult.Faulted("Property 'query' is required.");

        // Security: only SELECT queries allowed
        if (!SelectPattern().IsMatch(query))
            return ActivityResult.Faulted("Only SELECT queries are allowed.");

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var cmd = new NpgsqlCommand(query, connection);

            if (parameters is not null)
            {
                foreach (var (key, value) in parameters)
                    cmd.Parameters.AddWithValue(key, value ?? DBNull.Value);
            }

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            var rows = new List<Dictionary<string, object?>>();

            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new Dictionary<string, object?>();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                rows.Add(row);
            }

            return ActivityResult.Completed(new Dictionary<string, object?>
            {
                ["rows"] = rows,
                ["rowCount"] = rows.Count
            });
        }
        catch (Exception ex)
        {
            return ActivityResult.Faulted($"Database query failed: {ex.Message}");
        }
    }
}
