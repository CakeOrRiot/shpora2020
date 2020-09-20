using ORM.Contracts;
using ORM.Db;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime;
using System.Globalization;

namespace ORM
{
    public interface ISerializer
    {
        string Serialize<T>(T obj, T originalObj = null) where T : DbEntity;
        T Deserialize<T>(string dbAnswer) where T : DbEntity;
    }

    public interface ITracker
    {
        DbEntity this[DbEntity key] { get; set; }
        void Add<T>(T key, T value) where T : DbEntity;
        IEnumerable<DbEntity> Keys { get; }
    }

    public class Tracker : ITracker
    {
        private Dictionary<DbEntity, DbEntity> CurrentToOriginal { get; set; }
        public Tracker()
        {
            CurrentToOriginal = new Dictionary<DbEntity, DbEntity>();
        }

        public void Add<T>(T key, T value)
            where T : DbEntity
        {
            CurrentToOriginal.Add(key, value);
        }

        public IEnumerable<DbEntity> Keys { get => CurrentToOriginal.Keys; }

        public DbEntity this[DbEntity key]
        {
            get => CurrentToOriginal[key];
            set => CurrentToOriginal[key] = value;
        }
    }

    public interface ICopier
    {
        T GetCopy<T>(T obj) where T : DbEntity;
    }

    public class PropertiesCopier : ICopier
    {
        public T GetCopy<T>(T obj)
            where T : DbEntity
        {
            var type = typeof(T);
            var constructors = type.GetConstructors();
            if (constructors.Length == 0)
                return null;
            var constructor = constructors.First();
            var parameters = constructor.GetParameters().Select(param => param.DefaultValue).ToArray();
            var copy = (T)constructor.Invoke(parameters);
            foreach (var property in type.GetProperties())
            {
                var value = property.GetValue(obj, BindingFlags.GetProperty, null, null, CultureInfo.InvariantCulture);
                property.SetValue(copy, value, BindingFlags.SetProperty, null, null, CultureInfo.InvariantCulture);
            }
            return copy;
        }
    }

    public class Cash
    {
        private Dictionary<string, DbEntity> cash;
        public readonly ITracker tracker;
        private readonly ICopier copier;
        public Cash()
        {
            cash = new Dictionary<string, DbEntity>();
            tracker = new Tracker();
            copier = new PropertiesCopier();
        }

        public Cash(Cash cash)
        {
            copier = new PropertiesCopier();
            tracker = new Tracker();
            this.cash = new Dictionary<string, DbEntity>();
            foreach (var key in cash.Keys)
            {
                Add(key, cash[key]);
            }
        }

        public void Add<T>(string key, T item)
            where T : DbEntity
        {
            cash[key] = item;
            tracker.Add(item, copier.GetCopy(item));
        }

        public void Add(Cash items)
        {
            foreach (var key in items.Keys)
            {
                var type = items[key].GetType();
                Add(key, items[key]);
            }
        }

        public bool Contains(string key)
        {
            return cash.ContainsKey(key);
        }

        public IEnumerable<string> Keys { get => cash.Keys; }

        public DbEntity this[string key]
        {
            get => cash[key];
        }
    }

    public static class StringExtensions
    {
        public static IEnumerable<string> Tokenize(this string input, char separator, char escape)
        {
            return input.Tokenize(new char[] { separator }, escape);
        }

        public static IEnumerable<string> Tokenize(this string input, char[] separators, char escape)
        {
            if (input is null)
                yield break;
            var buffer = new StringBuilder();
            bool escaping = false;
            foreach (char ch in input)
            {
                if (escaping)
                {
                    buffer.Append(ch);
                    escaping = false;
                }
                else if (ch == escape)
                {
                    buffer.Append(ch);
                    escaping = true;
                }
                else if (separators.Contains(ch))
                    yield return buffer.Flush();
                else
                    buffer.Append(ch);
            }
            if (buffer.Length > 0 || separators.Contains(input[input.Length - 1]))
                yield return buffer.Flush();
        }
    }

    static class StringBuilderExtensions
    {
        public static string Flush(this StringBuilder stringBuilder)
        {
            string result = Convert.ToString(stringBuilder, CultureInfo.InvariantCulture);
            stringBuilder.Clear();
            return result;
        }
    }

    public class Serializer : ISerializer
    {
        public T Deserialize<T>(string dbAnswer)
            where T : DbEntity
        {
            var type = typeof(T);
            T result;
            try
            {
                result = (T)type.GetConstructor(Type.EmptyTypes).Invoke(new object[0]);
            }
            catch
            {
                return null;
            }

            var fields = dbAnswer
                .Tokenize(new char[] { ',', ';' }, '\\')
                .Where(field => string.Compare(field, "", StringComparison.InvariantCulture) != 0);

            foreach (var field in fields)
            {
                var tokens = field.Tokenize('=', '\\').ToList();
                var propertyName = tokens[0];
                var propertyValue = tokens[1];
                propertyValue = RemoveEscapeSymbols(propertyValue);
                var property = type.GetProperty(propertyName);
                var propertyType = property.PropertyType;
                var converter = TypeDescriptor.GetConverter(propertyType);
                property.SetValue(result, converter.ConvertFromInvariantString(propertyValue),
                    BindingFlags.SetProperty, null, null,
                    CultureInfo.InvariantCulture); ;
            }
            return result;
        }

        public string RemoveEscapeSymbols(string str)
        {
            return Regex.Replace(str, @"\\(.)", "$1", RegexOptions.CultureInvariant);
        }
        public string AddEscapeSymbols(string str)
        {
            return Regex.Replace(str, @"([\\;,=])", @"\$1", RegexOptions.CultureInvariant);
        }

        public string Serialize<T>(T currentObj, T originalObj = null)
            where T : DbEntity
        {
            var type = currentObj.GetType();
            if (originalObj is null)
            {
                try
                {
                    originalObj = (T)type.GetConstructor(Type.EmptyTypes).Invoke(new object[0]);
                }
                catch
                {
                    return null;
                }
            }

            var result = new StringBuilder();
            result.Append($"Id={currentObj.Id}");
            foreach (var property in type.GetProperties())
            {
                var currentValue = property.GetValue(currentObj, BindingFlags.GetProperty, null, null, CultureInfo.InvariantCulture);
                var originalValue = property.GetValue(originalObj, BindingFlags.GetProperty, null, null, CultureInfo.InvariantCulture);
                if (string.Compare(property.Name, "Id", StringComparison.InvariantCulture) == 0)
                    continue;

                if (currentValue is null && !(originalValue is null))
                    result.Append($",{property.Name}={AddEscapeSymbols(Convert.ToString(currentValue, CultureInfo.InvariantCulture))}");
                else if (!(currentValue is null) && !currentValue.Equals(originalValue))
                    result.Append($",{property.Name}={AddEscapeSymbols(Convert.ToString(currentValue, CultureInfo.InvariantCulture))}");
            }
            result.Append(";");
            return Convert.ToString(result, CultureInfo.InvariantCulture);
        }
    }

    public interface IQueryGenerator
    {
        StringBuilder GetAddQuery(Cash insertCash, ISerializer serializer);
        StringBuilder GetUpdateQuery(Cash updateCash, ISerializer serializer);
    }

    public class QueryGenerator : IQueryGenerator
    {
        public StringBuilder GetAddQuery(Cash insertCash, ISerializer serializer)
        {
            var query = new StringBuilder();
            foreach (var key in insertCash.Keys)
            {
                var currentObj = insertCash[key];
                query.Append("add ");
                query.Append(serializer.Serialize(currentObj));
            }
            return query;
        }

        public StringBuilder GetUpdateQuery(Cash updateCash, ISerializer serializer)
        {
            var query = new StringBuilder();
            foreach (var key in updateCash.Keys)
            {
                query.Append("upd ");
                var currentObj = updateCash[key];
                var originalObj = updateCash.tracker[currentObj];
                query.Append(serializer.Serialize(currentObj, originalObj));
            }
            return query;
        }
    }

    public class DataContext : IDataContext
    {
        private readonly IDbEngine dbEngine;
        private Cash insertCash;
        private Cash updateCash;
        private readonly ISerializer serializer;
        private readonly string notFound = ";";
        private readonly string[] dbErrors = { "err already_exists", "err doenst_exists", "err syntax" };
        private readonly IQueryGenerator queryGenerator;
        public DataContext(IDbEngine dbEngine)
        {
            this.dbEngine = dbEngine;
            insertCash = new Cash();
            updateCash = new Cash();
            serializer = new Serializer();
            queryGenerator = new QueryGenerator();
        }

        public T Find<T>(string id) where T : DbEntity
        {
            if (updateCash.Contains(id))
                return (T)updateCash[id];
            var dbAnswer = dbEngine.Execute($"get Id={id};");
            if (string.Compare(dbAnswer, notFound, StringComparison.InvariantCulture) == 0)
                return null;
            var obj = serializer.Deserialize<T>(dbAnswer);
            updateCash.Add(obj.Id, obj);

            return obj;
        }

        public T Read<T>(string id) where T : DbEntity
        {
            if (updateCash.Contains(id))
                return (T)updateCash[id];
            var dbAnswer = dbEngine.Execute($"get Id={id};");
            if (string.Compare(dbAnswer, notFound, StringComparison.InvariantCulture) == 0)
                throw new Exception($"Id={id} not found");
            var obj = serializer.Deserialize<T>(dbAnswer);
            updateCash.Add(obj.Id, obj);
            return obj;
        }

        public void Insert<T>(T entity) where T : DbEntity
        {
            if (entity is null)
                throw new NullReferenceException("Entity is null");
            insertCash.Add(entity.Id, entity);
        }

        public void SubmitChanges()
        {
            var addQuery = queryGenerator.GetAddQuery(insertCash, serializer);
            var updateQuery = queryGenerator.GetUpdateQuery(updateCash, serializer);
            var query = new StringBuilder();
            query.Append(addQuery);
            query.Append(updateQuery);
            updateCash.Add(insertCash);
            insertCash = new Cash();
            var dbAnswer = dbEngine.Execute(Convert.ToString(query, CultureInfo.InvariantCulture));
            if (dbErrors.Any(error => dbAnswer.Contains(error)))
                throw new Exception("Data base error");
        }
    }
}