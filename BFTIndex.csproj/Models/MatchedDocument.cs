namespace BFTIndex.Models
{
    public class MatchedDocument
    {
        public string Id { get; }
        
        /// <summary>
        /// Вес документа - это мера его соответствия поисковому запросу: чем он больше - тем лучше документ подходит под поисковый запрос
        /// Это всегда неотрицательное число.
        /// Расчёту данной величины посвящена задача 'BFT: Ранжирование' - до неё можешь игнорировать данный параметр.
        /// </summary>
        public double Weight { get; }

        public MatchedDocument(string id, double weight)
        {
            Id = id;
            Weight = weight;
        }
    }
}