public static class ByteExtensions
{
    public static int ToInt32(this byte[] bytes, int startIndex)
    {
        return bytes[startIndex]
            | (bytes[startIndex + 1] << 8)
            | (bytes[startIndex + 2] << 16)
            | (bytes[startIndex + 3] << 24);
    }

    public static uint ToUInt32(this byte[] bytes, int startIndex)
    {
        int convert = bytes.ToInt32(startIndex);
        if (convert < 0) return uint.MaxValue;
        return (uint)convert;
    }

    public static int ToInt16(this byte[] bytes, int startIndex)
    {
        return bytes[startIndex]
            | (bytes[startIndex + 1] << 8);
    }

    public static ushort ToUInt16(this byte[] bytes, int startIndex)
    {
        int convert = bytes.ToInt16(startIndex);
        if (convert < 0) return ushort.MaxValue;
        return (ushort)convert;
    }
}
