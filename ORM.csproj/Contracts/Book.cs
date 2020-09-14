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
                if (!objProperty.Equals(thisProperty))
                    return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            int hashCode = -1315566232;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Title);
            hashCode = hashCode * -1521134295 + Price.GetHashCode();
            hashCode = hashCode * -1521134295 + Weight.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Author);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Skill);
            return hashCode;
        }
    }
}