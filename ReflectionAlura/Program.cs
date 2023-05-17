using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;

var orm = new Orm<Pessoa>("Data Source=hello.db");
var x = orm.Insert(new Pessoa
{
    Id = 1,
    Nome = "Thamirys",
    DataDeNascimento = DateTime.Now,
    Ativo = true
});

orm.Insert(new Pessoa
{
    Id = 2,
    Nome = "Diego",
    DataDeNascimento = DateTime.Now,
    Ativo = true
});

orm.Insert(new Pessoa
{
    Id = 3,
    Nome = "Diana",
    DataDeNascimento = DateTime.Now,
    Ativo = true
});


var pessoa = orm.SelectById(1);
if (pessoa is not null)
{
    Console.WriteLine($"{pessoa.Id} - {pessoa.Nome} - {pessoa.DataDeNascimento} - {pessoa.Ativo}");
    pessoa.Nome += " EDITADO";
    orm.Update(pessoa);
}


orm.DeleteById(2);


var all = orm.SelectAll();
foreach (var p in all)
{
    Console.WriteLine($"{p.Id} - {p.Nome} - {p.DataDeNascimento} - {p.Ativo}");
}


public class Orm<T> where T : class, new()
{
    private readonly string _connectionString;


    public Orm(string connectionString)
    {
        _connectionString = connectionString;
    }

    public T Insert(T entity)
    {
        var propertiesValues = PropertiesManager.GetPropertiesValues(entity);


        var tableName = typeof(T).Name;
        var columns = string.Empty;
        var parameters = string.Empty;


        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var command = connection.CreateCommand();
        for (var i = 0; i < propertiesValues.Count; i++)
        {
            var propertyValue = propertiesValues[i];
            command.Parameters.AddWithValue($"${propertyValue.Name}", propertyValue.Value);
            columns += propertyValue.Name;
            parameters += $"${propertyValue.Name}";
            if (i < 0 || i >= propertiesValues.Count - 1) continue;
            columns += ",";
            parameters += ",";
        }
        command.CommandText = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters})";
        var x = command.ExecuteNonQuery();
        Console.WriteLine(command.CommandText);
        return entity;
    }

    public T Update(T entity)
    {
        var propertiesValues = PropertiesManager.GetPropertiesValues(entity);


        var tableName = typeof(T).Name;
        var columnsValues = string.Empty;
        object? id = null;


        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var command = connection.CreateCommand();
        for (var i = 0; i < propertiesValues.Count; i++)
        {
            var propertyValue = propertiesValues[i];
            if (propertyValue.IsId)
                id = propertyValue.Value;

            command.Parameters.AddWithValue($"${propertyValue.Name}", propertyValue.Value);
            columnsValues += $"{propertyValue.Name} = ${propertyValue.Name}";
            if (i < 0 || i >= propertiesValues.Count - 1) continue;
            columnsValues += ",";
        }
        command.CommandText = $"UPDATE {tableName} SET {columnsValues} WHERE ID = {id}";
        var x = command.ExecuteNonQuery();
        Console.WriteLine(command.CommandText);
        return entity;
    }


    public IReadOnlyList<T> SelectAll()
    {
        var tableName = typeof(T).Name;
        var sql = $"SELECT * FROM {tableName}";
        var all = Select(sql);
        return all;
    }

    public T? SelectById(int id)
    {
        var tableName = typeof(T).Name;
        var sql = $"SELECT * FROM {tableName} WHERE Id = {id}";
        var all = Select(sql);
        return all.FirstOrDefault();
    }

    public void DeleteById(int id)
    {
        var properties = PropertiesManager.GetProperties<T>();

        var tableName = typeof(T).Name;
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = $"DELETE FROM {tableName} WHERE Id = {id}";
        _ = command.ExecuteNonQuery();
    }

    private IReadOnlyList<T> Select(string sql)
    {
        var properties = PropertiesManager.GetProperties<T>();

        var tableName = typeof(T).Name;
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = sql;

        using var reader = command.ExecuteReader();
        return CreateEntities(reader);
    }

    private IReadOnlyList<T> CreateEntities(SqliteDataReader reader)
    {
        var entities = new List<T>();
        var properties = PropertiesManager.GetProperties<T>();
        while (reader.Read())
        {
            var entity = new T();
            foreach (var property in properties)
            {
                var value = Convert.ChangeType(reader[property.Name], property.Type);
                var type = typeof(T);
                var propertyInfo = type.GetProperty(property.Name);
                propertyInfo!.SetValue(entity, value);
            }
            entities.Add(entity);
        }
        return entities;
    }
}


public static class PropertiesManager
{
    private static readonly Dictionary<Type, IReadOnlyList<Property>> _cache = new();
    public static IReadOnlyList<Property> GetPropertiesValues<T>(T instance)
    {
        var values = new List<Property>();
        foreach (var property in instance!.GetType().GetProperties())
        {
            var value = property!.GetValue(instance, null);
            values.Add(new Property(property.Name, property.PropertyType, property.Name == "Id", value));
        }
        return values;
    }

    public static IReadOnlyList<Property> GetProperties<T>()
    {
        var type = typeof(T);
        if (_cache.TryGetValue(type, out var properties)) return properties;

        properties = typeof(T).GetProperties()
            .Select(x => new Property(x.Name, x.PropertyType, x.Name == "Id"))
            .ToList();
        _cache.Add(type, properties);
        return properties;
    }
}


public record Property(string Name, Type Type, bool IsId, object? Value = null);


public class Pessoa
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateTime DataDeNascimento { get; set; }
    public bool Ativo { get; set; }
}
