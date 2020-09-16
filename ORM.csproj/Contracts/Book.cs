using System;
using System.Collections.Generic;

namespace ORM.Contracts
{
    public class Book : DbEntity
    {
        public string Title { get; set; }

        public int Price { get; set; }
        public decimal Weight { get; set; }

        public string Author { get; set; }

        public string Skill { get; set; }

        public override bool Equals(object obj)
        {
            var book = (Book)obj;

            var type = typeof(Book);
            foreach (var property in type.GetProperties())
            {
                var objProperty = Convert.ChangeType(property.GetValue(book), property.PropertyType);
                var thisProperty = Convert.ChangeType(property.GetValue(this), property.PropertyType);
                if (objProperty is null && thisProperty is null)
                    continue;
                if (objProperty is null || thisProperty is null || !objProperty.Equals(thisProperty))
                    return false;
            }
            return true;
        }
    }
}