using Chromia.Encoding;
using Xunit;
using Xunit.Abstractions;

namespace Chromia.Tests.Encoding
{
    public class Asn1Test : PrintableTest
    {
        public Asn1Test(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void NullTest()
        {
            var writer = new AsnWriter();

            writer.WriteNull();

            var content = writer.Encode();

            var expected = Buffer.From("A0020500");
            Assert.Equal(expected.Parse(), content.Parse());
        }

        [Fact]
        public void NullDecodeTest()
        {
            var expected = new NullGtv();
            var actual = Gtv.DecodeToGtv(expected.Encode());
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("", "A1020400")]
        [InlineData("AFFE", "A1040402AFFE")]
        [InlineData("E2BE5C617CE50AFD0882A753C6FDA9C4D925EEDAC50DB97E33F457826A856DE0", "A1220420E2BE5C617CE50AFD0882A753C6FDA9C4D925EEDAC50DB97E33F457826A856DE0")]
        public void OctetStringTest(string data, string expectedStr)
        {
            var writer = new AsnWriter();

            var dataBuffer = Buffer.From(data);
            writer.WriteOctetString(dataBuffer);

            var content = writer.Encode();

            var expected = Buffer.From(expectedStr);
            Assert.Equal(expected.Parse(), content.Parse());
        }

        [Theory]
        [InlineData("", "A2020C00")]
        [InlineData("Hello World!", "A20E0C0C48656C6C6F20576F726C6421")]
        [InlineData("Swedish: Åå Ää Öö", "A2190C17537765646973683A20C385C3A520C384C3A420C396C3B6")]
        [InlineData("Danish/Norway: Ææ Øø Åå", "A21F0C1D44616E6973682F4E6F727761793A20C386C3A620C398C3B820C385C3A5")]
        [InlineData("German/Finish: Ää Öö Üü", "A21F0C1D4765726D616E2F46696E6973683A20C384C3A420C396C3B620C39CC3BC")]
        [InlineData("Greek: αβγδϵζηθικλμνξοπρστυϕχψωΑΒΓΔΕΖΗΘΙΚΛΜΝΞΟΠΡΣΤΥΦΧΨΩ", "A2690C67477265656B3A20CEB1CEB2CEB3CEB4CFB5CEB6CEB7CEB8CEB9CEBACEBBCEBCCEBDCEBECEBFCF80CF81CF83CF84CF85CF95CF87CF88CF89CE91CE92CE93CE94CE95CE96CE97CE98CE99CE9ACE9BCE9CCE9DCE9ECE9FCEA0CEA1CEA3CEA4CEA5CEA6CEA7CEA8CEA9")]
        [InlineData("Russian: АаБбВвГгДдЕеЁёЖжЗзИиЙйКкЛлМмНнОоПпСсТтУуФфХхЦцЧчШшЩщЪъЫыЬьЭэЮюЯя", "A2818C0C81895275737369616E3A20D090D0B0D091D0B1D092D0B2D093D0B3D094D0B4D095D0B5D081D191D096D0B6D097D0B7D098D0B8D099D0B9D09AD0BAD09BD0BBD09CD0BCD09DD0BDD09ED0BED09FD0BFD0A1D181D0A2D182D0A3D183D0A4D184D0A5D185D0A6D186D0A7D187D0A8D188D0A9D189D0AAD18AD0ABD18BD0ACD18CD0ADD18DD0AED18ED0AFD18F")]
        public void UTF8StringTest(string data, string expectedStr)
        {
            var writer = new AsnWriter();

            writer.WriteUTF8String(data);

            var content = writer.Encode();

            var expected = Buffer.From(expectedStr);
            Assert.Equal(expected.Parse(), content.Parse());
        }


        [Theory]
        [InlineData(0, "A303020100")]
        [InlineData(-1, "A3030201FF")]
        [InlineData(42424242, "A3060204028757B2")]
        [InlineData(-255, "A3040202FF01")]
        [InlineData(-256, "A3040202FF00")]
        [InlineData(-65535, "A3050203FF0001")]
        [InlineData(65535, "A305020300FFFF")]
        [InlineData(long.MinValue + 1, "A30A02088000000000000001")]
        [InlineData(long.MaxValue, "A30A02087FFFFFFFFFFFFFFF")]
        public void IntegerTest(long data, string expectedStr)
        {
            var writer = new AsnWriter();

            writer.WriteInteger(data);

            var content = writer.Encode();

            var expected = Buffer.From(expectedStr);
            Assert.Equal(expected.Parse(), content.Parse());
        }


        [Theory]
        [InlineData("0", "A603020100")]
        [InlineData("1", "A603020101")]
        [InlineData("-1", "A6030201FF")]
        [InlineData("42", "A60302012A")]
        [InlineData("-42", "A6030201D6")]
        [InlineData("-256", "A6040202FF00")]
        [InlineData("256", "A60402020100")]
        [InlineData("-65535", "A6050203FF0001")]
        [InlineData("65535", "A605020300FFFF")]
        [InlineData("9223372036854775808", "A60B0209008000000000000000")]
        [InlineData("-9223372036854775809", "A60B0209FF7FFFFFFFFFFFFFFF")]
        public void BigIntegerTest(string data, string expectedStr)
        {
            var writer = new AsnWriter();

            writer.WriteBigInteger(System.Numerics.BigInteger.Parse(data));

            var content = writer.Encode();

            var expected = Buffer.From(expectedStr);
            Assert.Equal(expected.Parse(), content.Parse());
        }

        [Fact]
        public void SequenceTest()
        {
            var writer = new AsnWriter();

            writer.PushSequence(Asn1Choice.Array);

            writer.PushSequence(Asn1Choice.Array);
            writer.WriteOctetString(Buffer.From("E2BE5C617CE50AFD0882A753C6FDA9C4D925EEDAC50DB97E33F457826A856DE0"));
            writer.PopSequence();

            writer.PushSequence(Asn1Choice.Array);
            writer.PushSequence(Asn1Choice.Array);
            writer.WriteUTF8String("test_op1");
            writer.WriteUTF8String("arg1");
            writer.WriteInteger(42);
            writer.PopSequence();
            writer.PushSequence(Asn1Choice.Array);
            writer.WriteUTF8String("test_op2");
            writer.PopSequence();
            writer.PopSequence();

            writer.PopSequence();

            var content = writer.Encode();

            var expected = Buffer.From("A55B3059A5263024A1220420E2BE5C617CE50AFD0882A753C6FDA9C4D925EEDAC50DB97E33F457826A856DE0A52F302DA51B3019A20A0C08746573745F6F7031A2060C0461726731A30302012AA50E300CA20A0C08746573745F6F7032");
            Assert.Equal(expected.Parse(), content.Parse());
        }
    }
}
