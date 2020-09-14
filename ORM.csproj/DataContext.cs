using NUnit.Framework.Constraints;
using ORM.Contracts;
using ORM.Db;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;

namespace ORM
{

    public class Cash<T> : IEnumerable<T>
        where T : DbEntity
    {
        private Dictionary<string, T> cash;
        public Cash()
        {
            cash = new Dictionary<string, T>();
        }
        public void Add(T item)
        {
            cash[item.Id] = item;
        }

        public void Add(Cash<T> items)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }
        public bool Contains(string key)
        {
            return cash.ContainsKey(key);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return cash.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public T this[string key]
        {
            get => cash[key];
        }
    }

    public class Serializer<T> where T : DbEntity, new()
    {
        public T DeSerialize(string dbAnswer)
        {
            var obj = new T();
            var type = typeof(Book);
            var fieldsData = Regex.Split(dbAnswer, @"\,\;");

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
        private Serializer<Book> serializer;
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
            if (dbAnswer == ";")
                return null;
            var obj = serializer.DeSerialize(dbAnswer);
            updateCash.Add(obj);
            return obj;
        }

        public Book Read(string id)
        {
            if (updateCash.Contains(id))
                return updateCash[id];
            var dbAnswer = dbEngine.Execute($"get Id={id};");
            if (dbAnswer == ";")
                throw new Exception($"ID={id} not found");
            var obj = serializer.DeSerialize(dbAnswer);
            updateCash.Add(obj);
            return obj;
        }

        public void Insert(Book entity)
        {
            if (entity is null)
                throw new Exception("Entity is null");
            insertCash.Add(entity);
        }

        public void SubmitChanges()
        {
            var query = new StringBuilder();
            foreach (var obj in insertCash)
            {
                query.Append("add ");
                query.Append($"Id={obj.Id}");
                var type = obj.GetType();
                foreach (var propery in type.GetProperties())
                {
                    query.Append(",");
                    query.Append($"{propery.Name}={propery.GetValue(obj)}");
                }
                query.Append(";");
            }

            foreach (var obj in updateCash)
            {
                query.Append("upd ");
                query.Append($"Id={obj.Id}");
                var type = obj.GetType();
                foreach (var propery in type.GetProperties())
                {
                    query.Append(",");
                    query.Append($"{propery.Name}={propery.GetValue(obj)}");
                }
                query.Append(";");
            }
            updateCash.Add(insertCash);
            insertCash = new Cash<Book>();
            var dbAnswer = dbEngine.Execute(query.ToString());
            if (dbAnswer.Contains("err already_exists") || dbAnswer.Contains("err doenst_exists"))
                throw new Exception();
        }
    }
}