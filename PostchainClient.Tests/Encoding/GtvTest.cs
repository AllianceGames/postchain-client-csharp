using Chromia.Encoding;
using System;
using System.Collections.Generic;
using System.Numerics;
using Xunit;
using Xunit.Abstractions;

namespace Chromia.Tests.Encoding
{
    public class GtvTest : PrintableTest
    {
        public GtvTest(ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData("")]
        [InlineData("AFFE")]
        [InlineData("E2BE5C617CE50AFD0882A753C6FDA9C4D925EEDAC50DB97E33F457826A856DE0")]
        public void OctetStringTest(string b)
        {
            var expected = new ByteArrayGtv(Buffer.From(b));
            var actual = Gtv.DecodeToGtv(expected.Encode());
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("Hello World!")]
        [InlineData("")]
        [InlineData("Swedish: Åå Ää Öö")]
        [InlineData("Danish/Norway: Ææ Øø Åå")]
        [InlineData("German/Finish: Ää Öö Üü")]
        [InlineData("Greek lower: αβγδϵζηθικλμνξοπρστυϕχψω")]
        [InlineData("Greek upper: ΑΒΓΔΕΖΗΘΙΚΛΜΝΞΟΠΡΣΤΥΦΧΨΩ")]
        [InlineData("Russian: АаБбВвГгДдЕеЁёЖжЗзИиЙйКкЛлМмНнОоПпСсТтУуФфХхЦцЧчШшЩщЪъЫыЬьЭэЮюЯя")]
        public void UTF8StringTest(string s)
        {
            var expected = new StringGtv(s);
            var actual = Gtv.DecodeToGtv(expected.Encode());
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("1")]
        [InlineData("-1")]
        [InlineData("42")]
        [InlineData("-42")]
        [InlineData("-256")]
        [InlineData("256")]
        [InlineData("-65535")]
        [InlineData("65535")]
        [InlineData("9223372036854775808")]
        [InlineData("-9223372036854775809")]
        public void BigIntegerTest(string n)
        {
            var i = BigInteger.Parse(n);
            var expected = new BigIntegerGtv(i);
            var actual = Gtv.DecodeToGtv(expected.Encode());
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(42424242)]
        [InlineData(-255)]
        [InlineData(-256)]
        [InlineData(-65535)]
        [InlineData(65535)]
        [InlineData(long.MinValue)]
        [InlineData(long.MaxValue)]
        public void IntegerTest(long i)
        {
            var expected = new IntegerGtv(i);
            var actual = Gtv.DecodeToGtv(expected.Encode());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void EmptyArrayTest()
        {
            var expected = new ArrayGtv(Array.Empty<IGtv>());
            var actual = Gtv.DecodeToGtv(expected.Encode());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SimpleArrayTest()
        {
            var expected = new ArrayGtv(new IGtv[]
            {
                new StringGtv("foo"),
                new StringGtv("bar")
            });
            var actual = Gtv.DecodeToGtv(expected.Encode());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void MixedArrayTest()
        {
            var expected = new ArrayGtv(new IGtv[]
            {
                new StringGtv("foo"),
                new IntegerGtv(1)
            });
            var actual = Gtv.DecodeToGtv(expected.Encode());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void NestedArrayTest()
        {
            var expected = new ArrayGtv(new IGtv[]
            {
                new ArrayGtv(new IGtv[] { new StringGtv("foo") }),
                new ArrayGtv(new IGtv[] { new StringGtv("bar") })
            });
            var actual = Gtv.DecodeToGtv(expected.Encode());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SimpleObjectTest()
        {
            var expected = new ArrayGtv(new IGtv[]
            {
                new StringGtv("queryname"),
                new DictGtv(new Dictionary<string, IGtv>()
                {
                    { "arg", new StringGtv("foo") }
                })
            });
            var actual = Gtv.DecodeToGtv(expected.Encode());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void NestedObjectTest()
        {
            var expected = new ArrayGtv(new IGtv[]
            {
                new StringGtv("queryname"),
                new DictGtv(new Dictionary<string, IGtv>()
                {
                    {
                        "arg",
                        new DictGtv(new Dictionary<string, IGtv>() {
                            { "arg1", new StringGtv("foo") }
                        })
                    }
                })
            });
            var actual = Gtv.DecodeToGtv(expected.Encode());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ListEqualToArrayGtvTest()
        {
            var list = new List<int>() { int.MinValue, -1, 0, 1, int.MaxValue };
            var hash = ChromiaClient.EncodeToGtv(list);
            var actual = ChromiaClient.EncodeToGtv(list.ToArray());
            Assert.Equal(hash, actual);
        }
    }
}
