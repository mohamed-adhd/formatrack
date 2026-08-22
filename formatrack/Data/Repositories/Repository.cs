using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using formatrack.Data;

namespace formatrack.Data.Repositories;

public abstract class Repository<T> : IRepository<T>
{
    protected abstract string TableName { get; }
    protected abstract string IdColumn { get; }
    protected abstract T Map(SqliteDataReader reader);
    protected abstract void FillInsert(SqliteCommand command, T entity);
    protected abstract void FillUpdate(SqliteCommand command, T entity);
    protected abstract string InsertSql { get; }
    protected abstract string UpdateSql { get; }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync()
    {
        await AppDbContext.InitializeAsync();
        var items = new List<T>();
        await using var connection = new SqliteConnection(AppDbContext.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqliteCommand($"SELECT * FROM {TableName} ORDER BY {IdColumn} DESC;", connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            items.Add(Map(reader));
        return items;
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        await AppDbContext.InitializeAsync();
        await using var connection = new SqliteConnection(AppDbContext.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqliteCommand($"SELECT * FROM {TableName} WHERE {IdColumn} = $id LIMIT 1;", connection);
        command.Parameters.AddWithValue("$id", id);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : default;
    }

    public virtual async Task<int> AddAsync(T entity)
    {
        await AppDbContext.InitializeAsync();
        await using var connection = new SqliteConnection(AppDbContext.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqliteCommand(InsertSql + "; SELECT last_insert_rowid();", connection);
        FillInsert(command, entity);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public virtual async Task<bool> UpdateAsync(T entity)
    {
        await AppDbContext.InitializeAsync();
        await using var connection = new SqliteConnection(AppDbContext.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqliteCommand(UpdateSql, connection);
        FillUpdate(command, entity);
        return await command.ExecuteNonQueryAsync() > 0;
    }

    public virtual async Task<bool> DeleteAsync(int id)
    {
        await AppDbContext.InitializeAsync();
        await using var connection = new SqliteConnection(AppDbContext.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqliteCommand($"DELETE FROM {TableName} WHERE {IdColumn} = $id;", connection);
        command.Parameters.AddWithValue("$id", id);
        return await command.ExecuteNonQueryAsync() > 0;
    }

    protected static string Text(SqliteDataReader reader, string name) => reader[name] == DBNull.Value ? string.Empty : Convert.ToString(reader[name]) ?? string.Empty;
    protected static int Int(SqliteDataReader reader, string name) => reader[name] == DBNull.Value ? 0 : Convert.ToInt32(reader[name]);
    protected static double Double(SqliteDataReader reader, string name) => reader[name] == DBNull.Value ? 0d : Convert.ToDouble(reader[name]);
    protected static double? NullableDouble(SqliteDataReader reader, string name) => reader[name] == DBNull.Value ? null : Convert.ToDouble(reader[name]);
    protected static DateTime Date(SqliteDataReader reader, string name) => DateTime.Parse(Text(reader, name));
    protected static DateTime? NullableDate(SqliteDataReader reader, string name) => reader[name] == DBNull.Value ? null : DateTime.Parse(Text(reader, name));
    protected static object Db(string? value) => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;
    protected static object Db(int? value) => value.HasValue ? value.Value : DBNull.Value;
    protected static object Db(double? value) => value.HasValue ? value.Value : DBNull.Value;
    protected static object Db(bool? value) => value.HasValue ? value.Value ? 1 : 0 : DBNull.Value;
    protected static bool Has(SqliteDataReader reader, string name)
    {
        for (var i = 0; i < reader.FieldCount; i++)
            if (string.Equals(reader.GetName(i), name, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }
}
