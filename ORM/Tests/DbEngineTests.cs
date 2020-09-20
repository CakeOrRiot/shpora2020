using NUnit.Framework;
using ORM.Db;

namespace ORM.Tests
{
    public class DbEngineTests
    {
        private IDbEngine sut;

        [SetUp]
        public void SetUp()
        {
            sut = new DbEngine();
        }

        [TestCase("smt Id=1;", "err syntax;")]
        public void UnknownCommand_SyntaxError(string query, string expected)
        {
            Assert.AreEqual(expected, sut.Execute(query));
        }

        [TestCase("get Id=1", "err syntax;")]
        [TestCase("add Id=1", "err syntax;")]
        [TestCase("upd Id=1", "err syntax;")]
        public void AnyCommand_WithoutCommandDelimiter_SyntaxError(string query, string expected)
        {
            Assert.AreEqual(expected, sut.Execute(query));
        }

        [TestCase("get F1=V1;", "err syntax;")]
        [TestCase("add F1=V1;", "err syntax;")]
        [TestCase("upd F1=V1;", "err syntax;")]
        public void AnyCommand_WithoutIdField_SyntaxError(string query, string expected)
        {
            Assert.AreEqual(expected, sut.Execute(query));
        }

        [TestCase("add Id=1,=V1;get Id=1;", "ok;Id=1,=V1;")]
        [TestCase("add Id=1,=V1;upd Id=1,=V2;get Id=1;", "ok;ok;Id=1,=V2;")]
        public void AnyCommand_FieldNameCanBeEmpty(string query, string expected)
        {
            Assert.AreEqual(expected, sut.Execute(query));
        }

        [TestCase("get Id=;", ";")]
        [TestCase("add Id=,F1=V1;get Id=;", "ok;Id=,F1=V1;")]
        [TestCase("add Id=,F1=V1;upd Id=,F1=V2;get Id=;", "ok;ok;Id=,F1=V2;")]
        public void AnyCommand_FieldValueCanBeEmpty(string query, string expected)
        {
            Assert.AreEqual(expected, sut.Execute(query));
        }

        [TestCase("add Id=1,F,1=V1;", "err syntax;")]
        [TestCase("add Id=1,F;1=V1;", "err syntax;")]
        [TestCase("upd Id=1,F,1=V1;", "err syntax;")]
        [TestCase("upd Id=1,F;1=V1;", "err syntax;")]
        public void AnyCommand_FieldNameWithControlChars_SyntaxError(string query, string expected)
        {
            Assert.AreEqual(expected, sut.Execute(query));
        }

        [TestCase("get Id=1,1;", "err syntax;")]
        [TestCase("get Id=1;1;", "err syntax;")]
        [TestCase("get Id=1=1;", "err syntax;")]
        [TestCase("add Id=1,F1=V,1;", "err syntax;")]
        [TestCase("add Id=1,F1=V;1;", "err syntax;")]
        [TestCase("add Id=1,F1=V=1;", "err syntax;")]
        [TestCase("upd Id=1,F1=V,1;", "err syntax;")]
        [TestCase("upd Id=1,F1=V;1;", "err syntax;")]
        [TestCase("upd Id=1,F1=V=1;", "err syntax;")]
        public void AnyCommand_FieldValueWithControlChars_SyntaxError(string query, string expected)
        {
            Assert.AreEqual(expected, sut.Execute(query));
        }

        [TestCase("get Id=1,F1=V1;", "err syntax;")]
        public void Get_WithFieldsBesidesId_SyntaxError(string query, string expected)
        {
            Assert.AreEqual(expected, sut.Execute(query));
        }

        [TestCase("get Id=1;", ";")]
        public void Get_NotExistingId_EmptyString(string query, string expected)
        {
            Assert.AreEqual(expected, sut.Execute(query));
        }

        [TestCase("add Id=1;", "ok;")]
        public void Add_WithoutFieldsBesidesId_Ok(string query, string expected)
        {
            Assert.AreEqual(expected, sut.Execute(query));
        }

        [TestCase("add Id=1,F1=V1;", "ok;")]
        public void Add_NotExistingId_Ok(string query, string expected)
        {
            Assert.AreEqual(expected, sut.Execute(query));
        }

        [TestCase("add Id=1,F1=V1;add Id=1,F1=V1;", "ok;err already_exists;")]
        public void Add_ExistingId_AlreadyExists(string query, string expected)
        {
            Assert.AreEqual(expected, sut.Execute(query));
        }

        [TestCase("add F1=V1,Id=1;", "ok;")]
        public void Add_FieldsOrderIsNotSignificant(string query, string expected)
        {
            Assert.AreEqual(expected, sut.Execute(query));
        }

        [TestCase("add Id=1,F1=V1;get Id=1;", "ok;Id=1,F1=V1;")]
        public void Get_ExistingId_Entity(string query, string expected)
        {
            Assert.AreEqual(expected, sut.Execute(query));
        }

        [TestCase("upd Id=1;", "err syntax;")]
        public void Update_WithoutFieldsBesidesId_SyntaxError(string query, string expected)
        {
            Assert.AreEqual(expected, sut.Execute(query));
        }

        [TestCase("upd Id=1,F1=V1;", "err doesnt_exist;")]
        public void Update_NotExistingId_DoesNotExist(string query, string expected)
        {
            Assert.AreEqual(expected, sut.Execute(query));
        }

        [TestCase("add Id=1,F1=V1;upd Id=1,F1=V2;", "ok;ok;")]
        public void Update_ExistingId_Ok(string query, string expected)
        {
            Assert.AreEqual(expected, sut.Execute(query));
        }

        [TestCase("add Id=1,F1=V1;upd F1=V1,Id=1;", "ok;ok;")]
        public void Update_FieldsOrderIsNotSignificant(string query, string expected)
        {
            Assert.AreEqual(expected, sut.Execute(query));
        }

        [TestCase("add Id=1,F1=V1;get Id=1;upd Id=1,F1=V2;get Id=1;", "ok;Id=1,F1=V1;ok;Id=1,F1=V2;")]
        public void Get_ReturnsTheNewestEntity(string query, string expected)
        {
            Assert.AreEqual(expected, sut.Execute(query));
        }

        [Test]
        public void Get_IdIsAlwaysFirst_OtherFieldsOrderIsNotGuaranteed()
        {
            sut.Execute("add Id=1,F1=V1,F2=V2,F3=V3;");
            var result = sut.Execute("get Id=1;");

            Assert.That(result, Is.AnyOf("Id=1,F1=V1,F2=V2,F3=V3;",
                                         "Id=1,F1=V1,F3=V3,F2=V2;",
                                         "Id=1,F2=V2,F1=V1,F3=V3;",
                                         "Id=1,F2=V2,F3=V3,F1=V1;",
                                         "Id=1,F3=V3,F1=V1,F2=V2;",
                                         "Id=1,F3=V3,F2=V2,F1=V1;"));
        }

        [TestCase("add Id=1;get Id=1;", "ok;Id=1;")]
        public void Get_EntityWithOnlyId_ReturnsCorrectEntity(string query, string expected)
        {
            Assert.AreEqual(expected, sut.Execute(query));
        }

        [Test]
        public void Update_NotExistingFields_AppendsThem()
        {
            sut.Execute("add Id=1,F1=V1;upd Id=1,F1=V1,F2=V2;");
            var result = sut.Execute("get Id=1;");

            Assert.That(result, Is.AnyOf("Id=1,F1=V1,F2=V2;",
                                         "Id=1,F2=V2,F1=V1;"));
        }

        [Test]
        public void Update_NotAllExistingFields_UpdatesOnlyTransmitted()
        {
            sut.Execute("add Id=1,F1=V1,F2=V2;upd Id=1,F1=V3;");
            var result = sut.Execute("get Id=1;");

            Assert.That(result, Is.AnyOf("Id=1,F1=V3,F2=V2;",
                                         "Id=1,F2=V2,F1=V3;"));
        }

        [TestCase(@"get Id=\=\;\,;", @";")]
        [TestCase(@"get Id=\=\,\;;", @";")]
        [TestCase(@"get Id=\;\=\,;", @";")]
        [TestCase(@"get Id=\;\,\=;", @";")]
        [TestCase(@"get Id=\,\=\;;", @";")]
        [TestCase(@"get Id=\,\;\=;", @";")]

        [TestCase(@"add Id=1,\=\;\,=V1;", @"ok;")]
        [TestCase(@"add Id=1,\=\,\;=V1;", @"ok;")]
        [TestCase(@"add Id=1,\;\=\,=V1;", @"ok;")]
        [TestCase(@"add Id=1,\;\,\==V1;", @"ok;")]
        [TestCase(@"add Id=1,\,\=\;=V1;", @"ok;")]
        [TestCase(@"add Id=1,\,\;\==V1;", @"ok;")]

        [TestCase(@"add Id=2;upd Id=2,F1=\=\;\,;get Id=2;", @"ok;ok;Id=2,F1=\=\;\,;")]
        [TestCase(@"add Id=2;upd Id=2,F1=\=\,\;;get Id=2;", @"ok;ok;Id=2,F1=\=\,\;;")]
        [TestCase(@"add Id=2;upd Id=2,F1=\;\=\,;get Id=2;", @"ok;ok;Id=2,F1=\;\=\,;")]
        [TestCase(@"add Id=2;upd Id=2,F1=\;\,\=;get Id=2;", @"ok;ok;Id=2,F1=\;\,\=;")]
        [TestCase(@"add Id=2;upd Id=2,F1=\,\;\=;get Id=2;", @"ok;ok;Id=2,F1=\,\;\=;")]
        [TestCase(@"add Id=2;upd Id=2,F1=\,\=\;;get Id=2;", @"ok;ok;Id=2,F1=\,\=\;;")]

        [TestCase(@"add Id=3,F1\\=V1;get Id=3;", @"ok;Id=3,F1\\=V1;")]
        [TestCase(@"add Id=3,F1=V1\\;get Id=3;", @"ok;Id=3,F1=V1\\;")]
        [TestCase(@"add Id=3,F1\\=V1\\;upd Id=3,F1\\=V2\\;get Id=3;", @"ok;ok;Id=3,F1\\=V2\\;")]
        public void AnyCommand_AnyControlCharCanBeEscaped(string query, string expected)
        {
            Assert.AreEqual(expected, sut.Execute(query));
        }
    }
}