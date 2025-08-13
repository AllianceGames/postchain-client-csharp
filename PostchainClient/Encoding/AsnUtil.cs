namespace Chromia.Encoding
{
    internal enum Asn1Choice
    {
        None = 0,
        Null = 0xa0,
        ByteArray = 0xa1,
        String = 0xa2,
        Integer = 0xa3,
        Dict = 0xa4,
        Array = 0xa5,
        BigInteger = 0xa6
    }
}