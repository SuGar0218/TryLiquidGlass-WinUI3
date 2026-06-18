using Windows.Storage.Streams;

namespace TryLiquidGlass.Helpers;

public static class BufferToBytesExtensions
{
    public static byte[] ToBytes(this IBuffer buffer)
    {
        byte[] bytes = new byte[buffer.Length];
        using DataReader dataReader = DataReader.FromBuffer(buffer);
        dataReader.ReadBytes(bytes);
        return bytes;
    }
}
