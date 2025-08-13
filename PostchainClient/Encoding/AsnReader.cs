using System;
using System.Numerics;
using System.Formats.Asn1;
using AsnReaderInternal = System.Formats.Asn1.AsnReader;

namespace Chromia.Encoding
{
    internal class AsnReader
    {
        public bool HasData => reader.HasData;

        private readonly AsnReaderInternal reader;

        public AsnReader(byte[] bytes)
        {
            if (bytes is null) throw new ArgumentNullException(nameof(bytes));
            reader = new AsnReaderInternal(bytes, AsnEncodingRules.DER);
        }

        private AsnReader(AsnReaderInternal inner)
        {
            reader = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        private static Asn1Tag Ctx(int n, bool constructed = true)
            => new Asn1Tag(TagClass.ContextSpecific, n, constructed);

        private static bool IsOurChoiceTag(Asn1Tag t, out Asn1Choice choice)
        {
            if (t.TagClass == TagClass.ContextSpecific && t.IsConstructed && t.TagValue <= 6)
            {
                choice = (Asn1Choice)(0xA0 + t.TagValue);
                return true;
            }
            choice = Asn1Choice.None;
            return false;
        }

        private AsnReaderInternal ReadExplicitWrapper(Asn1Choice choice)
        {
            int n = (int)choice - 0xA0;
            if (n < 0 || n > 31) throw new InvalidOperationException("Invalid CHOICE tag.");
            return reader.ReadSequence(Ctx(n, constructed: true));
        }

        public Asn1Choice PeekChoice()
        {
            var tag = reader.PeekTag();
            return IsOurChoiceTag(tag, out var c) ? c : Asn1Choice.None;
        }

        public void ReadNull()
        {
            var outer = ReadExplicitWrapper(Asn1Choice.Null);
            outer.ReadNull();
            outer.ThrowIfNotEmpty();
        }

        public AsnReader ReadSequence(Asn1Choice choice)
        {
            AsnReaderInternal innerContents;

            if (choice != Asn1Choice.None)
            {
                var outer = ReadExplicitWrapper(choice);
                innerContents = outer.ReadSequence();
                outer.ThrowIfNotEmpty();
            }
            else
            {
                innerContents = reader.ReadSequence();
            }

            return new AsnReader(innerContents);
        }

        public Buffer ReadOctetString()
        {
            var outer = ReadExplicitWrapper(Asn1Choice.ByteArray);
            var bytes = outer.ReadOctetString();
            outer.ThrowIfNotEmpty();
            return Buffer.From(bytes);
        }

        public string ReadUTF8String()
        {
            var outer = ReadExplicitWrapper(Asn1Choice.String);
            var s = outer.ReadCharacterString(UniversalTagNumber.UTF8String);
            outer.ThrowIfNotEmpty();
            return s;
        }

        public string ReadDictKey()
        {
            var s = reader.ReadCharacterString(UniversalTagNumber.UTF8String);
            return s;
        }

        public long ReadInteger()
        {
            var outer = ReadExplicitWrapper(Asn1Choice.Integer);
            BigInteger bi = outer.ReadInteger();
            outer.ThrowIfNotEmpty();

            if (bi < long.MinValue || bi > long.MaxValue)
                throw new OverflowException("INTEGER does not fit into Int64.");

            return (long)bi;
        }

        public BigInteger ReadBigInteger()
        {
            var outer = ReadExplicitWrapper(Asn1Choice.BigInteger);
            BigInteger bi = outer.ReadInteger();
            outer.ThrowIfNotEmpty();
            return bi;
        }
    }
}