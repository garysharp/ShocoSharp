using System;

namespace ShocoSharp
{
    internal static class ArrayHelpers
    {

        public static T[] MakeCopy<T>(this T[] Source, int NewLength)
        {
            var clone = new T[NewLength];
            Array.Copy(Source, clone, Math.Min(Source.Length, NewLength));
            return clone;
        }

        public static T[] MakeCopy<T>(this T[] Source)
        {
            var clone = new T[Source.Length];
            Array.Copy(Source, clone, Source.Length);
            return clone;
        }

        public static T[,] MakeCopy<T>(this T[,] Source)
        {
            var clone = new T[Source.GetLength(0), Source.GetLength(1)];
            Array.Copy(Source, clone, Source.Length);
            return clone;
        }

        public static int IndexOf(this byte[] Source, byte Value)
        {
            for (int i = 0; i < Source.Length; i++)
            {
                if (Source[i] == Value)
                    return i;
            }
            return -1;
        }

        public static bool Contains(this byte[] Source, byte Value)
        {
            for (int i = 0; i < Source.Length; i++)
            {
                if (Source[i] == Value)
                    return true;
            }
            return false;
        }

        public static void GetBigEndianBytes(this uint Value, byte[] Buffer)
        {
            if (Buffer.Length < 4)
                throw new ArgumentOutOfRangeException(nameof(Buffer));

            Buffer[0] = (byte)(Value >> 24);
            Buffer[1] = (byte)(Value >> 16);
            Buffer[2] = (byte)(Value >> 8);
            Buffer[3] = (byte)Value;
        }

        public static uint ToUInt32(this byte[] Buffer, int Index)
        {
            if (Index + 3 < Buffer.Length)
                return (uint)(
                    (Buffer[Index] << 24) |
                    (Buffer[Index + 1] << 16) |
                    (Buffer[Index + 2] << 8) |
                    (Buffer[Index + 3]));
            else if (Index + 2 < Buffer.Length)
                return (uint)(
                    (Buffer[Index] << 24) |
                    (Buffer[Index + 1] << 16) |
                    (Buffer[Index + 2] << 8));
            else if (Index + 1 < Buffer.Length)
                return (uint)(
                    (Buffer[Index] << 24) |
                    (Buffer[Index + 1] << 16));
            else if (Index < Buffer.Length)
                return (uint)(Buffer[Index] << 24);
            else
                throw new ArgumentOutOfRangeException(nameof(Buffer));
        }

    }
}
