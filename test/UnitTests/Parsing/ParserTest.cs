using System;
using NUnit.Framework;

namespace UnitTests.Parsing
{
    [TestFixture]
    public class ParserTest
    {
        [Test]
        public void Pass()
        {
            Assert.True(true);
        }

        [Test]
        public void Fail()
        {
            Assert.False(true);
        }

        [Test]
        [Ignore("another time")]
        public void Ignore()
        {
            Assert.True(false);
        }
    }
}