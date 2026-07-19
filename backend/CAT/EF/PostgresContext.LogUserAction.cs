using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;

namespace CAT.EF;

public partial class PostgresContext
{
    public async Task<Guid> LogUserActionAsync(
        string userId,
        string actionType,
        string? tableName = null,
        Guid? recordId = null,
        string? oldValues = null,
        string? newValues = null,
        string? sessionId = null,
        string status = "success",
        string? errorMessage = null,
        object? additionalInfo = null
    )
    {
        await using var conn = new NpgsqlConnection(Database.GetConnectionString());
        await conn.OpenAsync();

        var sql = @"
            SELECT log_user_action(
                @p_user_id::uuid,
                @p_action_type::varchar,
                @p_table_name::varchar,
                @p_record_id::uuid,
                @p_old_values::jsonb,
                @p_new_values::jsonb,
                @p_session_id::varchar,
                @p_status::varchar,
                @p_error_message::text,
                @p_additional_info::jsonb
            );";

        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("p_user_id", NpgsqlDbType.Uuid, Guid.Parse(userId));
        cmd.Parameters.AddWithValue("p_action_type", NpgsqlDbType.Varchar, actionType);
        cmd.Parameters.AddWithValue("p_table_name", NpgsqlDbType.Varchar, (object?)tableName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_record_id", NpgsqlDbType.Uuid, (object?)recordId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_old_values", NpgsqlDbType.Jsonb, (object?)oldValues ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_new_values", NpgsqlDbType.Jsonb, (object?)newValues ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_session_id", NpgsqlDbType.Varchar, (object?)sessionId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_status", NpgsqlDbType.Varchar, status);
        cmd.Parameters.AddWithValue("p_error_message", NpgsqlDbType.Text, (object?)errorMessage ?? DBNull.Value);

        var additionalJson = additionalInfo != null
            ? JsonSerializer.Serialize(additionalInfo)
            : null;
        cmd.Parameters.AddWithValue("p_additional_info", NpgsqlDbType.Jsonb, (object?)additionalJson ?? DBNull.Value);

        var result = await cmd.ExecuteScalarAsync();
        return (Guid)result!;
    }
}