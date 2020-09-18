using ORM.Contracts;
using ORM.Db;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;

namespace ORM
{
    public interface ISerializer<T> where T : DbEntity, new()
    {
        string Serialize(T obj, T originalObj = null);
        T Deserialize(string dbAnswer);
        string RemoveEscapeSymbols(string str);
        string AddEscapeSymbols(string str);
    }

    public interface ITracker<T>
        where T : DbEntity, new()
    {
        T this[T key] { get; set; }

        void Add(T key, T value);

        IEnumerable<T> Keys { get; }
    }

    public class Tracker<T> : ITracker<T>
        where T : DbEntity, new()
    {
        private Dictionary<T, T> CurrentToOriginal { get; set; }

        public Tracker()
        {
            CurrentToOriginal = new Dictionary<T, T>();
        }

        public void Add(T key, T value)
        {
            CurrentToOriginal.Add(key, value);
        }

        public IEnumerable<T> Keys { get => CurrentToOriginal.Keys; }

        public T this[T key]
        {
            get => CurrentToOriginal[key];
            set => CurrentToOriginal[key] = value;
        }
    }

    public interface ICopier<T>
        where T : DbEntity, new()
    {
        T GetCopy(T obj);
    }

    public class PropertiesCopier<T> : ICopier<T>
        where T : DbEntity, new()
    {
        public T GetCopy(T obj)
        {
            var type = typeof(T);
            var copy = new T();
            foreach (var property in type.GetProperties())
            {
                property.SetValue(copy, property.GetValue(obj));
            }
            return copy;
        }
    }
    public class Cash<T>
        where T : DbEntity, new()
    {
        private Dictionary<string, T> cash;
        public readonly ITracker<T> tracker;
        private readonly ICopier<T> copier;
        public Cash()
        {
            cash = new Dictionary<string, T>();
            tracker = new Tracker<T>();
            copier = new PropertiesCopier<T>();
        }

        public Cash(Cash<T> cash)
        {
            copier = new PropertiesCopier<T>();
            tracker = new Tracker<T>();
            this.cash = new Dictionary<string, T>();
            foreach (var key in cash.Keys)
            {
                this.cash.Add(key, cash[key]);
                tracker.Add(cash[key], copier.GetCopy(cash[key]));
            }
        }

        public void Add(string key, T item)
        {
            cash[key] = item;
            tracker.Add(item, copier.GetCopy(item));
        }

        public void Add(Cash<T> items)
        {
            foreach (var key in items.Keys)
            {
                Add(key, items[key]);
            }
        }

        public bool Contains(string key)
        {
            return cash.ContainsKey(key);
        }

        public IEnumerable<string> Keys { get => cash.Keys; }

        public T this[string key]
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
            if (input == null) yield break;
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
                {
                    yield return buffer.Flush();
                }
                else
                {
                    buffer.Append(ch);
                }
            }
            if (buffer.Length > 0 || separators.Contains(input[input.Length - 1]))
                yield return buffer.Flush();
        }


    }

    static class StringBuilderExtensions
    {
        public static string Flush(this StringBuilder stringBuilder)
        {
            string result = stringBuilder.ToString();
            stringBuilder.Clear();
            return result;
        }
    }

    public class Serializer<T> : ISerializer<T>
        where T : DbEntity, new()
    {
        //public readonly char[] escapeSymbols = { ';', ',', '\\', '=' };
        public T Deserialize(string dbAnswer)
        {
            var obj = new T();
            var type = typeof(Book);
            var fields = dbAnswer.Tokenize(new char[] { ',', ';' }, '\\')// Regex.Split(dbAnswer, @"(?<=[^\\(\\\\)](?:[,;]))")
                 .Where(field => field != "");
            foreach (var field in fields)
            {
                var tokens = field.Tokenize('=', '\\').ToList();
                var propertyName = tokens[0];
                var propertyValue = tokens[1];
                propertyValue = RemoveEscapeSymbols(propertyValue);
                var property = type.GetProperty(propertyName);
                property.SetValue(obj, Convert.ChangeType(propertyValue, property.PropertyType));
            }
            return obj;
        }

        public string RemoveEscapeSymbols(string str)
        {
            return Regex.Replace(str, @"\\(.)", "$1"); //Regex.Unescape(str); //
        }
        public string AddEscapeSymbols(string str)
        {
            return Regex.Replace(str, @"([\\;,=])", @"\$1");
        }

        public string Serialize(T currentObj, T originalObj = null)
        {
            if (originalObj is null)
                originalObj = new T();

            var result = new StringBuilder();
            result.Append($"Id={currentObj.Id}");
            var type = originalObj.GetType();
            foreach (var property in type.GetProperties())
            {
                if (property.Name != "Id" && property.GetValue(originalObj) != property.GetValue(currentObj))
                {
                    result.Append(",");
                    var currentValue = property.GetValue(currentObj);
                    if (currentValue is null)
                        result.Append($"{property.Name}=");
                    else
                        result.Append($"{property.Name}={AddEscapeSymbols(currentValue.ToString())}");
                }
            }
            result.Append(";");
            return result.ToString();
        }
    }
    public class DataContext : IDataContext
    {
        private readonly IDbEngine dbEngine;
        private Cash<Book> insertCash;
        private Cash<Book> updateCash;
        private readonly ISerializer<Book> serializer;
        private readonly string emptyAnswer = ";";
        private readonly string[] dbErrors = { "err already_exists", "err doenst_exists", "err syntax" };
        public DataContext(IDbEngine dbEngine)
        {
            this.dbEngine = dbEngine;
            insertCash = new Cash<Book>();
            updateCash = new Cash<Book>();
            serializer = new Serializer<Book>();
        }

        public Book Find(string id)
        {
            if (updateCash.Contains(id))
                return updateCash[id];
            var dbAnswer = dbEngine.Execute($"get Id={id};");
            //Console.WriteLine($"Find: {dbAnswer}");
            //dbAnswer = serializer.RemoveEscapeSymbols(dbAnswer);
            if (dbAnswer == emptyAnswer)
                return null;
            var obj = serializer.Deserialize(dbAnswer);
            updateCash.Add(obj.Id, obj);
            return obj;
        }

        public Book Read(string id)
        {
            if (updateCash.Contains(id))
                return updateCash[id];
            var dbAnswer = dbEngine.Execute($"get Id={id};");
            //Console.WriteLine($"Read: {dbAnswer}");
            //dbAnswer = serializer.RemoveEscapeSymbols(dbAnswer);
            if (dbAnswer == emptyAnswer)
                throw new Exception($"Id={id} not found");
            var obj = serializer.Deserialize(dbAnswer);
            updateCash.Add(obj.Id, obj);
            return obj;
        }

        public void Insert(Book entity)
        {
            if (entity is null)
                throw new NullReferenceException("Entity is null");
            //Console.WriteLine($"Insert: {entity}");
            insertCash.Add(entity.Id, entity);
        }

        private StringBuilder GetUpdateQuery()
        {
            var query = new StringBuilder();
            foreach (var key in updateCash.Keys)
            {
                query.Append("upd ");
                var currentObj = updateCash[key];
                query.Append(serializer.Serialize(currentObj));

            }
            return query;
        }

        private StringBuilder GetAddQuery()
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

        public void SubmitChanges()
        {
            //Console.WriteLine($"Submit");
            var query = GetAddQuery().Append(GetUpdateQuery());
            updateCash.Add(insertCash);
            insertCash = new Cash<Book>();
            var dbAnswer = dbEngine.Execute(query.ToString());
            if (dbErrors.Any(error => dbAnswer.Contains(error)))
                throw new Exception("Data base error");
        }
    }
}