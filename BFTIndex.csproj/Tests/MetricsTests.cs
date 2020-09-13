using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BFTIndex.Models;
using NUnit.Framework;

namespace BFTIndex.Tests
{
    class MetricsTests
    {

        private readonly double eps = 1e-5;
        [SetUp]
        public void SetUp()
        {

        }

        [Test]
        public void TFTest()
        {
            var doc = new Document(new List<string>() { "АААА", "АААА", "АААА", "БББ", "ЗАС" });

            TF tfEvaluator = new TF(doc);
            Assert.AreEqual(3.0 / doc.Length, tfEvaluator.Evaluate(doc[0]), eps);
            Assert.AreEqual(1.0 / doc.Length, tfEvaluator.Evaluate(doc[3]), eps);
            Assert.AreEqual(1.0 / doc.Length, tfEvaluator.Evaluate(doc[4]), eps);
        }

        [Test]
        public void IDFTest()
        {
            var doc1 = new Document(new List<string>() { "АААА", "АААА", "ББББ", "ЗАС" });
            var doc2 = new Document(new List<string>() { "АААА", "АААА", "ББББ", "ББББ", "ББББ" });
            var documents = new Dictionary<string, Document>
            {
                ["1"] = doc1,
                ["2"] = doc2
            };
            IDF idfEvaluator = new IDF(documents);
            Assert.AreEqual(Math.Log10((double)documents.Count / 2), idfEvaluator.Evaluate(doc1[0]), eps);
            Assert.AreEqual(Math.Log10((double)documents.Count / 2), idfEvaluator.Evaluate(doc1[2]), eps);
            Assert.AreEqual(Math.Log10((double)documents.Count / 1), idfEvaluator.Evaluate(doc1[3]), eps);
        }
    }
}
