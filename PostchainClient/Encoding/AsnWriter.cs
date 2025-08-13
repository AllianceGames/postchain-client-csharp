using System;
using System.Collections.Generic;
using System.Numerics;
using System.Formats.Asn1;
using AsnWriterInternal = System.Formats.Asn1.AsnWriter;

namespace Chromia.Encoding
{
    internal class AsnWriter
    {
        private readonly AsnWriterInternal writer = new AsnWriterInternal(AsnEncodingRules.DER);

        private sealed class Scope
        {
            public IDisposable ExplicitWrapper;
            public IDisposable InnerSequence;
            public Asn1Choice Choice;
        }

        private readonly Stack<Scope> scopes = new Stack<Scope>();

        private static int ChoiceIndex(Asn1Choice c)
        {
            if (c == Asn1Choice.None) return -1;
            return (int)c - 0xA0;
        }

        private static Asn1Tag CtxTag(Asn1Choice c)
        {
            int idx = ChoiceIndex(c);
            if (idx < 0) throw new InvalidOperationException("Invalid CHOICE for context tag.");
            return new Asn1Tag(TagClass.ContextSpecific, idx, isConstructed: true);
        }

        public void WriteNull()
        {
            using (var wrap = writer.PushSequence(CtxTag(Asn1Choice.Null)))
            {
                writer.WriteNull();
            }
        }

        public void WriteOctetString(Buffer buffer) => WriteOctetString(buffer.Bytes);

        public void WriteOctetString(byte[] octetString)
        {
            using (var wrap = writer.PushSequence(CtxTag(Asn1Choice.ByteArray)))
            {
                writer.WriteOctetString(octetString);
            }
        }

        public void WriteDictKey(string dictKey)
        {
            WriteUTF8String(dictKey, skipChoice: true);
        }

        public void WriteUTF8String(string s) => WriteUTF8String(s, skipChoice: false);

        private void WriteUTF8String(string s, bool skipChoice)
        {
            if (skipChoice)
            {
                writer.WriteCharacterString(UniversalTagNumber.UTF8String, s);
            }
            else
            {
                using (var wrap = writer.PushSequence(CtxTag(Asn1Choice.String)))
                {
                    writer.WriteCharacterString(UniversalTagNumber.UTF8String, s);
                }
            }
        }

        public void WriteInteger(long number)
        {
            using (var wrap = writer.PushSequence(CtxTag(Asn1Choice.Integer)))
            {
                writer.WriteInteger(number);
            }
        }

        public void WriteBigInteger(BigInteger number)
        {
            using (var wrap = writer.PushSequence(CtxTag(Asn1Choice.BigInteger)))
            {
                writer.WriteInteger(number);
            }
        }

        public void PushSequence(Asn1Choice choice)
        {
            var scope = new Scope { Choice = choice };

            if (choice != Asn1Choice.None)
            {
                scope.ExplicitWrapper = writer.PushSequence(CtxTag(choice));
            }

            scope.InnerSequence = writer.PushSequence();
            scopes.Push(scope);
        }

        public void PopSequence()
        {
            if (scopes.Count == 0) throw new InvalidOperationException("No open sequence to pop.");

            var s = scopes.Pop();

            s.InnerSequence.Dispose();

            s.ExplicitWrapper?.Dispose();
        }

        public void WriteEncodedValue(byte[] encodedValue)
        {
            writer.WriteEncodedValue(encodedValue);
        }

        public Buffer Encode()
        {
            if (scopes.Count != 0)
                throw new InvalidOperationException("Tried to encode with open Sequence.");

            return Buffer.From(writer.Encode());
        }
    }
}