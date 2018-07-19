using System;
using System.IO;
using System.Text;

namespace ShocoSharp
{
    public partial class ShocoModel
    {
        /// <summary>
        /// Compresses a byte array
        /// </summary>
        /// <param name="Data">Data to be compressed</param>
        /// <param name="OutputStream">Output stream for the compressed data</param>
        public void Compress(byte[] Data, Stream OutputStream)
            => Compress(new ArraySegment<byte>(Data), OutputStream);

        /// <summary>
        /// Compresses a byte array
        /// </summary>
        /// <param name="Data">Data to be compressed</param>
        /// <returns>Compressed data as an ArraySegment</returns>
        public ArraySegment<byte> Compress(byte[] Data)
            => Compress(new ArraySegment<byte>(Data));

        /// <summary>
        /// Compresses a string using UTF8 encoding
        /// </summary>
        /// <param name="Data">Data to be compressed</param>
        /// <param name="OutputStream">Output stream for the compressed data</param>
        public void Compress(string Data, Stream OutputStream)
            => Compress(Data, Encoding.UTF8, OutputStream);

        /// <summary>
        /// Compresses a string using UTF8 encoding
        /// </summary>
        /// <param name="Data">Data to be compressed</param>
        /// <returns>Compressed data as an ArraySegment</returns>
        public ArraySegment<byte> Compress(string Data)
            => Compress(Data, Encoding.UTF8);

        /// <summary>
        /// Compresses an ArraySegment
        /// </summary>
        /// <param name="Data">Data to be compressed</param>
        /// <returns>Compressed data as an ArraySegment</returns>
        public ArraySegment<byte> Compress(ArraySegment<byte> Data)
        {
            if (Data.Array == null)
                throw new ArgumentNullException(nameof(Data));
            if (Data.Count == 0)
                return new ArraySegment<byte>();

            MemoryStream stream = new MemoryStream(Data.Count);

            Compress(Data, stream);

            stream.TryGetBuffer(out var result);

            return result;
        }

        /// <summary>
        /// Compresses a string
        /// </summary>
        /// <param name="Data">Data to be compressed</param>
        /// <param name="Encoding">Encoding used to convert the data to bytes</param>
        /// <returns>Compressed data as an ArraySegment</returns>
        public ArraySegment<byte> Compress(string Data, Encoding Encoding)
        {
            if (Data == null)
                throw new ArgumentNullException(nameof(Data));
            if (Data.Length == 0)
                return new ArraySegment<byte>();
            if (Encoding == null)
                throw new ArgumentNullException(nameof(Encoding));

            MemoryStream stream = new MemoryStream(Encoding.GetMaxByteCount(Data.Length));

            Compress(Data, Encoding, stream);

            stream.TryGetBuffer(out var result);

            return result;
        }

        /// <summary>
        /// Compresses a string
        /// </summary>
        /// <param name="Data">Data to be compressed</param>
        /// <param name="Encoding">Encoding used to convert the data to bytes</param>
        /// <param name="OutputStream">Output stream for the compressed data</param>
        public void Compress(string Data, Encoding Encoding, Stream OutputStream)
        {
            if (Data == null)
                throw new ArgumentNullException(nameof(Data));
            if (Data.Length == 0)
                return;
            if (Encoding == null)
                throw new ArgumentNullException(nameof(Encoding));

            var data = Encoding.GetBytes(Data);

            Compress(new ArraySegment<byte>(data), OutputStream);
        }

        /// <summary>
        /// Decompresses a byte array
        /// </summary>
        /// <param name="Data">Data to be decompressed</param>
        /// <param name="OutputStream">Output stream for the decompressed data</param>
        public void Decompress(byte[] Data, Stream OutputStream)
            => Decompress(new ArraySegment<byte>(Data), OutputStream);

        /// <summary>
        /// Decompresses a byte array
        /// </summary>
        /// <param name="Data">Data to be decompressed</param>
        /// <returns>Decompressed data as an ArraySegment</returns>
        public ArraySegment<byte> Decompress(byte[] Data)
            => Decompress(new ArraySegment<byte>(Data));

        /// <summary>
        /// Decompresses a byte array
        /// </summary>
        /// <param name="Data">Data to be decompressed</param>
        /// <param name="Encoding">Encoding to convert the data to a string</param>
        /// <returns></returns>
        public string DecompressString(byte[] Data, Encoding Encoding)
            => DecompressString(new ArraySegment<byte>(Data), Encoding);

        /// <summary>
        /// Decompresses an ArraySegment using UTF8 encoding
        /// </summary>
        /// <param name="Data">Data to be decompressed</param>
        /// <returns></returns>
        public string DecompressString(ArraySegment<byte> Data)
            => DecompressString(Data, Encoding.UTF8);

        /// <summary>
        /// Decompresses a byte array using UTF8 encoding
        /// </summary>
        /// <param name="Data">Data to be decompressed</param>
        /// <returns></returns>
        public string DecompressString(byte[] Data)
            => DecompressString(new ArraySegment<byte>(Data));

        /// <summary>
        /// Decompress an ArraySegement
        /// </summary>
        /// <param name="Data">Data to be decompressed</param>
        /// <returns>Decompressed data as an ArraySegment</returns>
        public ArraySegment<byte> Decompress(ArraySegment<byte> Data)
        {
            if (Data.Array == null)
                throw new ArgumentNullException(nameof(Data));
            if (Data.Count == 0)
                return new ArraySegment<byte>();

            MemoryStream stream = new MemoryStream(Data.Count * 2);

            Decompress(Data, stream);

            stream.TryGetBuffer(out var result);

            return result;
        }

        /// <summary>
        /// Decompress a string
        /// </summary>
        /// <param name="Data">Data to be decompressed</param>
        /// <param name="Encoding">Encoding to convert the data to a string</param>
        /// <returns></returns>
        public string DecompressString(ArraySegment<byte> Data, Encoding Encoding)
        {
            if (Encoding == null)
                throw new ArgumentNullException(nameof(Encoding));

            var result = Decompress(Data);

            return Encoding.GetString(result.Array, result.Offset, result.Count);
        }

        /// <summary>
        /// Reads a C Header into a ShocoModel
        /// </summary>
        /// <param name="Filename">C Header location</param>
        /// <returns>ShocoModel representation from the C Header</returns>
        public static ShocoModel ReadFromCHeader(string Filename)
            => ShocoModelExtensions.ReadFromCHeader(Filename);

        /// <summary>
        /// Reads a C Header into a ShocoModel
        /// </summary>
        /// <param name="Stream">C Header source</param>
        /// <returns>ShocoModel representation from the C Header</returns>
        public static ShocoModel ReadFromCHeader(Stream Stream)
            => ShocoModelExtensions.ReadFromCHeader(Stream);

        /// <summary>
        /// Reads a C Header into a ShocoModel
        /// </summary>
        /// <param name="FileContent">C Header source</param>
        /// <returns>ShocoModel representation from the C Header</returns>
        public static ShocoModel ReadFromCHeaderContent(string FileContent)
            => ShocoModelExtensions.ReadFromCHeaderContent(FileContent);

    }
}
