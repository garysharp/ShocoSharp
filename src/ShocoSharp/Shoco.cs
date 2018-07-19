using System;
using System.IO;
using System.Text;

namespace ShocoSharp
{
    /// <summary>
    /// Global helper methods for ShocoSharp
    /// </summary>
    public static class Shoco
    {
        private static ShocoModel defaultModel;

        /// <summary>
        /// The compression model used by the global helper methods
        /// </summary>
        public static ShocoModel DefaultModel
        {
            get => defaultModel;
            set => defaultModel = value ?? throw new ArgumentNullException(nameof(value));
        }

        static Shoco()
            => defaultModel = Models.ShocoCompatibleEnglishWordsModel.Instance;

        /// <summary>
        /// Compresses an ArraySegment
        /// </summary>
        /// <param name="Data">Data to be compressed</param>
        /// <param name="OutputStream">Output stream for the compressed data</param>
        public static void Compress(ArraySegment<byte> Data, Stream OutputStream)
            => defaultModel.Compress(Data, OutputStream);

        /// <summary>
        /// Compresses an ArraySegment
        /// </summary>
        /// <param name="Data">Data to be compressed</param>
        /// <returns>Compressed data as an ArraySegment</returns>
        public static ArraySegment<byte> Compress(ArraySegment<byte> Data)
            => defaultModel.Compress(Data);

        /// <summary>
        /// Compresses a byte array
        /// </summary>
        /// <param name="Data">Data to be compressed</param>
        /// <param name="OutputStream">Output stream for the compressed data</param>
        public static void Compress(byte[] Data, Stream OutputStream)
            => defaultModel.Compress(new ArraySegment<byte>(Data), OutputStream);

        /// <summary>
        /// Compresses a byte array
        /// </summary>
        /// <param name="Data">Data to be compressed</param>
        /// <returns>Compressed data as a byte array</returns>
        public static byte[] Compress(byte[] Data)
            => ResolveArraySegment(defaultModel.Compress(new ArraySegment<byte>(Data)));

        /// <summary>
        /// Compresses a string
        /// </summary>
        /// <param name="Data">Data to be compressed</param>
        /// <param name="Encoding">Encoding used to convert the data to bytes</param>
        /// <param name="OutputStream">Output stream for the compressed data</param>
        public static void Compress(string Data, Encoding Encoding, Stream OutputStream)
            => defaultModel.Compress(Data, Encoding, OutputStream);

        /// <summary>
        /// Compresses a string using UTF8 encoding
        /// </summary>
        /// <param name="Data">Data to be compressed</param>
        /// <param name="OutputStream">Output stream for the compressed data</param>
        public static void Compress(string Data, Stream OutputStream)
            => defaultModel.Compress(Data, OutputStream);

        /// <summary>
        /// Compresses a string using UTF8 encoding
        /// </summary>
        /// <param name="Data">Data to be compressed</param>
        /// <returns>Compressed data as a byte array</returns>
        public static byte[] Compress(string Data)
            => ResolveArraySegment(defaultModel.Compress(Data));

        /// <summary>
        /// Compresses a string
        /// </summary>
        /// <param name="Data">Data to be compressed</param>
        /// <param name="Encoding">Encoding used to convert the data to bytes</param>
        /// <returns>Compressed data as an ArraySegment</returns>
        public static ArraySegment<byte> Compress(string Data, Encoding Encoding)
            => defaultModel.Compress(Data, Encoding);

        /// <summary>
        /// Decompresses an ArraySegment
        /// </summary>
        /// <param name="Data">Data to be decompressed</param>
        /// <param name="OutputStream">Output stream for the decompressed data</param>
        public static void Decompress(ArraySegment<byte> Data, Stream OutputStream)
            => defaultModel.Decompress(Data, OutputStream);

        /// <summary>
        /// Decompresses an ArraySegment
        /// </summary>
        /// <param name="Data">Data to be decompressed</param>
        /// <returns>Decompressed data as an ArraySegment</returns>
        public static ArraySegment<byte> Decompress(ArraySegment<byte> Data)
            => defaultModel.Decompress(Data);

        /// <summary>
        /// Decompresses a byte array
        /// </summary>
        /// <param name="Data">Data to be decompressed</param>
        /// <param name="OutputStream">Output stream for the decompressed data</param>
        public static void Decompress(byte[] Data, Stream OutputStream)
            => defaultModel.Decompress(new ArraySegment<byte>(Data), OutputStream);

        /// <summary>
        /// Decompresses a byte array
        /// </summary>
        /// <param name="Data">Data to be decompressed</param>
        /// <returns>Decompressed data as a byte array</returns>
        public static byte[] Decompress(byte[] Data)
            => ResolveArraySegment(defaultModel.Decompress(new ArraySegment<byte>(Data)));

        /// <summary>
        /// Decompresses a string
        /// </summary>
        /// <param name="Data">Data to be decompressed</param>
        /// <param name="Encoding">Encoding to convert the data to a string</param>
        /// <returns>Decompressed data as a string</returns>
        public static string DecompressString(ArraySegment<byte> Data, Encoding Encoding)
            => defaultModel.DecompressString(Data, Encoding);

        /// <summary>
        /// Decompresses a string
        /// </summary>
        /// <param name="Data">Data to be decompressed</param>
        /// <param name="Encoding">Encoding to convert the data to a string</param>
        /// <returns>Decompressed data as a string</returns>
        public static string DecompressString(byte[] Data, Encoding Encoding)
            => defaultModel.DecompressString(new ArraySegment<byte>(Data), Encoding);

        /// <summary>
        /// Decompresses a string
        /// </summary>
        /// <param name="Data">Data to be decompressed</param>
        /// <returns>Decompressed data as a UTF8 decoded string</returns>
        public static string DecompressString(ArraySegment<byte> Data)
            => defaultModel.DecompressString(Data);

        /// <summary>
        /// Decompresses a string
        /// </summary>
        /// <param name="Data">Data to be decompressed</param>
        /// <returns>Decompressed data as a UTF8 decoded string</returns>
        public static string DecompressString(byte[] Data)
            => defaultModel.DecompressString(new ArraySegment<byte>(Data));

        private static byte[] ResolveArraySegment(ArraySegment<byte> Segment)
        {
            if (Segment.Count == 0)
                return Array.Empty<byte>();

            if (Segment.Offset == 0 && Segment.Array.Length == Segment.Count)
                return Segment.Array;

            var array = new byte[Segment.Count];
            Buffer.BlockCopy(Segment.Array, Segment.Offset, array, 0, Segment.Count);
            return array;
        }

    }
}
