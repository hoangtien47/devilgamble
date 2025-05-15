using System;

namespace Sui.Utilities
{
    public static class ByteArrayExtensions
    {
        public static byte[] PadLeft(this byte[] bytes, int length)
        {
            if (bytes.Length >= length) return bytes;

            var padded = new byte[length];
            Array.Copy(bytes, 0, padded, length - bytes.Length, bytes.Length);
            return padded;
        }
    }
}