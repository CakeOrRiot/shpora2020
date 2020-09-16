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
        string Serialize(T obj);
        T Deserialize(string dbAnswer);
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
            return copy;//----------------------------------------------------COPY
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

    public class Serializer<T> : ISerializer<T>
        where T : DbEntity, new()
    {
        public T Deserialize(string dbAnswer)
        {
            var obj = new T();
            var type = typeof(Book);
            //var fieldsData = Regex.Split(dbAnswer, @"([^\\])([,;])");
            //var fieldsData = Regex.Split(dbAnswer, @"[\;\,]").Where(field => field != "");
            var fieldsData = dbAnswer.Split(new char[] { ',', ';' },
                StringSplitOptions.RemoveEmptyEntries);
            foreach (var field in fieldsData)
            {
                var propertyName = field.Substring(0, field.IndexOf('='));
                var propertyValue = field.Substring(field.IndexOf('=') + 1);
                var property = type.GetProperty(propertyName);
                property.SetValue(obj, Convert.ChangeType(propertyValue, property.PropertyType));
            }
            return obj;
        }

        public string Serialize(T obj)
        {
            var result = new StringBuilder();
            result.Append($"Id={obj.Id}");
            var type = typeof(T);
            foreach (var property in type.GetProperties())
            {
                if (property.Name != "Id")
                    result.Append($",{property.Name}={property.GetValue(obj)}");
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
            insertCash.Add(entity.Id, entity);
        }

        private StringBuilder GetUpdateQuery()
        {
            var query = new StringBuilder();
            foreach (var key in updateCash.Keys)
            {
                query.Append("upd ");
                var currentObj = updateCash[key];
                var originalObj = updateCash.tracker[currentObj];
                var type = originalObj.GetType();
                query.Append($"Id={currentObj.Id}");
                foreach (var property in type.GetProperties())
                {
                    if (property.Name != "Id" && property.GetValue(originalObj) != property.GetValue(currentObj))
                    {
                        query.Append(",");
                        query.Append($"{property.Name}={property.GetValue(currentObj)}");
                    }
                }
                query.Append(";");
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
                query.Append($"Id={currentObj.Id}");
                var type = currentObj.GetType();
                var emptyObj = new Book();
                foreach (var property in type.GetProperties())
                {
                    if (property.Name != "Id" && property.GetValue(currentObj) != property.GetValue(emptyObj))
                    {
                        query.Append(",");
                        query.Append($"{property.Name}={property.GetValue(currentObj)}");
                    }
                }
                query.Append(";");
            }
            return query;
        }

        public void SubmitChanges()
        {
            var query = GetAddQuery().Append(GetUpdateQuery());
            updateCash.Add(insertCash); //= new Cash<Book>(insertCash);
            insertCash = new Cash<Book>();
            var dbAnswer = dbEngine.Execute(query.ToString());
            if (dbAnswer.Contains("err already_exists") || dbAnswer.Contains("err doenst_exists"))
                throw new Exception("Data base error");
        }
    }
}