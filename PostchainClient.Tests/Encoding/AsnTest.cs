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
        [InlineData("FF", "A1030401FF")]
        [InlineData("00", "A103040100")]
        [InlineData("0123456789ABCDEF", "A10A04080123456789ABCDEF")]
        [InlineData("00000000", "A106040400000000")]
        [InlineData("FFFFFFFF", "A1060404FFFFFFFF")]
        [InlineData("80", "A103040180")]
        [InlineData("7F", "A10304017F")]
        [InlineData("8000", "A10404028000")]
        [InlineData("007F", "A1040402007F")]
        [InlineData("0080", "A10404020080")]
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
        [InlineData("a", "A2030C0161")]
        [InlineData("abc", "A2050C03616263")]
        [InlineData("test@example.com", "A2120C1074657374406578616D706C652E636F6D")]
        [InlineData("Special chars: !@#$%^&*()", "A21B0C195370656369616C2063686172733A2021402324255E262A2829")]
        [InlineData("Numbers: 0123456789", "A2150C134E756D626572733A2030313233343536373839")]
        [InlineData("Mixed: aB3$", "A20D0C0B4D697865643A2061423324")]
        [InlineData("Tab\tand\nNewline", "A2110C0F54616209616E640A4E65776C696E65")]
        [InlineData("     spaces     ", "A2120C1020202020207370616365732020202020")]
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
        [InlineData(-283515829, "A3060204EF19E44B")]
        [InlineData(long.MinValue, "A30A02088000000000000000")]
        [InlineData(long.MaxValue, "A30A02087FFFFFFFFFFFFFFF")]
        [InlineData(1, "A303020101")]
        [InlineData(127, "A30302017F")]
        [InlineData(128, "A30402020080")]
        [InlineData(255, "A304020200FF")]
        [InlineData(256, "A30402020100")]
        [InlineData(32767, "A30402027FFF")]
        [InlineData(32768, "A3050203008000")]
        [InlineData(-128, "A303020180")]
        [InlineData(-129, "A3040202FF7F")]
        [InlineData(-32768, "A30402028000")]
        [InlineData(-32769, "A3050203FF7FFF")]
        [InlineData(1000000, "A30502030F4240")]
        [InlineData(-1000000, "A3050203F0BDC0")]
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
        [InlineData("127", "A60302017F")]
        [InlineData("128", "A60402020080")]
        [InlineData("255", "A604020200FF")]
        [InlineData("65536", "A6050203010000")]
        [InlineData("16777216", "A606020401000000")]
        [InlineData("4294967296", "A60702050100000000")]
        [InlineData("18446744073709551616", "A60B0209010000000000000000")]
        [InlineData("-128", "A603020180")]
        [InlineData("-129", "A6040202FF7F")]
        [InlineData("-65536", "A6050203FF0000")]
        [InlineData("-18446744073709551616", "A60B0209FF0000000000000000")]
        [InlineData("123456789012345678901234567890", "A60F020D018EE90FF6C373E0EE4E3F0AD2")]
        [InlineData("-123456789012345678901234567890", "A60F020DFE7116F0093C8C1F11B1C0F52E")]
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

        [Theory]
        [InlineData("", "A410300E300C0C00A2080C0676616C756531")]
        [InlineData("k", "A411300F300D0C016BA2080C0676616C756531")]
        [InlineData("key", "A4133011300F0C036B6579A2080C0676616C756531")]
        [InlineData("very_long_dictionary_key_name", "A42D302B30290C1D766572795F6C6F6E675F64696374696F6E6172795F6B65795F6E616D65A2080C0676616C756531")]
        [InlineData("key_with_numbers_123", "A424302230200C146B65795F776974685F6E756D626572735F313233A2080C0676616C756531")]
        [InlineData("key-with-dashes", "A41F301D301B0C0F6B65792D776974682D646173686573A2080C0676616C756531")]
        [InlineData("key.with.dots", "A41D301B30190C0D6B65792E776974682E646F7473A2080C0676616C756531")]
        [InlineData("KEY_UPPERCASE", "A41D301B30190C0D4B45595F555050455243415345A2080C0676616C756531")]
        public void DictKeyTest(string key, string expectedStr)
        {
            var writer = new AsnWriter();
            writer.PushSequence(Asn1Choice.Dict);
            writer.PushSequence(Asn1Choice.None);
            writer.WriteDictKey(key);
            writer.WriteUTF8String("value1");
            writer.PopSequence();
            writer.PopSequence();
            var content = writer.Encode();
            var expected = Buffer.From(expectedStr);
            Assert.Equal(expected.Parse(), content.Parse());
        }

        [Fact]
        public void EmptySequenceTest()
        {
            var writer = new AsnWriter();
            writer.PushSequence(Asn1Choice.Array);
            writer.PopSequence();
            var content = writer.Encode();
            var expected = Buffer.From("A5023000");
            Assert.Equal(expected.Parse(), content.Parse());
        }

        [Fact]
        public void NestedSequencesTest()
        {
            var writer = new AsnWriter();
            writer.PushSequence(Asn1Choice.Array);
            writer.PushSequence(Asn1Choice.Array);
            writer.PushSequence(Asn1Choice.Array);
            writer.WriteInteger(1);
            writer.PopSequence();
            writer.PopSequence();
            writer.PopSequence();
            var content = writer.Encode();
            var expected = Buffer.From("A50F300DA50B3009A5073005A303020101");
            Assert.Equal(expected.Parse(), content.Parse());
        }

        [Fact]
        public void MixedTypesSequenceTest()
        {
            var writer = new AsnWriter();
            writer.PushSequence(Asn1Choice.Array);
            writer.WriteNull();
            writer.WriteInteger(42);
            writer.WriteUTF8String("test");
            writer.WriteOctetString(Buffer.From("ABCD"));
            writer.WriteBigInteger(System.Numerics.BigInteger.Parse("123456789"));
            writer.PopSequence();
            var content = writer.Encode();
            var expected = Buffer.From("A521301FA0020500A30302012AA2060C0474657374A1040402ABCDA6060204075BCD15");
            Assert.Equal(expected.Parse(), content.Parse());
        }

        [Fact]
        public void SequenceWithMultipleNullsTest()
        {
            var writer = new AsnWriter();
            writer.PushSequence(Asn1Choice.Array);
            writer.WriteNull();
            writer.WriteNull();
            writer.WriteNull();
            writer.PopSequence();
            var content = writer.Encode();
            var expected = Buffer.From("A50E300CA0020500A0020500A0020500");
            Assert.Equal(expected.Parse(), content.Parse());
        }

        [Fact]
        public void LargeSequenceWithManyIntegersTest()
        {
            var writer = new AsnWriter();
            writer.PushSequence(Asn1Choice.Array);
            for (int i = 0; i < 20; i++)
            {
                writer.WriteInteger(i);
            }
            writer.PopSequence();
            var content = writer.Encode();
            var expected = Buffer.From("A5663064A303020100A303020101A303020102A303020103A303020104A303020105A303020106A303020107A303020108A303020109A30302010AA30302010BA30302010CA30302010DA30302010EA30302010FA303020110A303020111A303020112A303020113");
            Assert.Equal(expected.Parse(), content.Parse());
        }

        [Theory]
        [InlineData(10, "A20C0C0A41414141414141414141")]
        [InlineData(100, "A2660C6441414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141")]
        [InlineData(1000, "A28203EC0C8203E841414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141")]
        public void LongStringLengthTest(int stringLength, string expectedStr)
        {
            var writer = new AsnWriter();
            var longString = new string('A', stringLength);
            writer.WriteUTF8String(longString);
            var content = writer.Encode();
            var expected = Buffer.From(expectedStr);
            Assert.Equal(expected.Parse(), content.Parse());
        }

        [Fact]
        public void SequenceWithMixedChoicesTest()
        {
            var writer = new AsnWriter();
            writer.PushSequence(Asn1Choice.Array);
            
            writer.PushSequence(Asn1Choice.Dict);
            writer.PushSequence(Asn1Choice.None);
            writer.WriteDictKey("key1");
            writer.WriteUTF8String("value1");
            writer.PopSequence();
            writer.PopSequence();
            
            writer.WriteOctetString(Buffer.From("1234"));
            writer.WriteInteger(999);
            
            writer.PopSequence();
            var content = writer.Encode();
            var expected = Buffer.From("A5243022A414301230100C046B657931A2080C0676616C756531A10404021234A304020203E7");
            Assert.Equal(expected.Parse(), content.Parse());
        }
    }
}
