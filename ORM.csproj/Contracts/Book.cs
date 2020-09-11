namespace ORM.Contracts
{
    public class Book : DbEntity
    {
        public string Title { get; set; }

        public int Price { get; set; }
        public decimal Weight { get; set; }

        public string Author { get; set; }

        public string Skill { get; set; }
    }
}