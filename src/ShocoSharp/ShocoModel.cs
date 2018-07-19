using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace ShocoSharp
{
    /// <summary>
    /// Compression model used to compress and decompress data
    /// </summary>
    public partial class ShocoModel
    {
        private readonly int minimumCharacter;
        private readonly int maximumCharacter;

        private readonly int maximumSuccessorLength;

        private readonly byte[] charactersById;

        private readonly byte[] idsByCharacter;

        private readonly byte[,] successorIdsByCharacterId;

        private readonly byte[,] charactersBySuccessorId;

        private readonly ShocoPack[] packs;

        /// <summary>
        /// Character code of the smallest indexed character
        /// </summary>
        public int MinimumCharacter { get { return minimumCharacter; } }
        /// <summary>
        /// Character code of the largest indexed character
        /// </summary>
        public int MaximumCharacter { get { return maximumCharacter; } }

        /// <summary>
        /// Maximum number of characters which can be packed together
        /// </summary>
        public int MaximumSuccessorLength { get { return maximumSuccessorLength; } }

        /// <summary>
        /// The indexed characters based on training
        /// </summary>
        public byte[] CharactersById => charactersById.MakeCopy();
        /// <summary>
        /// A lookup array matching character codes to their character id (or 0xFF if the character is not indexed)
        /// </summary>
        public byte[] IdsByCharacter => idsByCharacter.MakeCopy();

        /// <summary>
        /// A lookup array of successor ids of character ids based on training
        /// </summary>
        public byte[,] SuccessorIdsByCharacterId => successorIdsByCharacterId.MakeCopy();
        /// <summary>
        /// A lookup array of character ids from successor ids based on training
        /// </summary>
        public byte[,] CharactersBySuccessorId => charactersBySuccessorId.MakeCopy();

        /// <summary>
        /// Compression pack schemes
        /// </summary>
        public ShocoPack[] Packs => packs.MakeCopy();

        public ShocoModel(int MinimumCharacter, int MaximumCharacter, int MaximumSuccessorLength,
            byte[] CharactersById, byte[] IdsByCharacter,
            byte[,] SuccessorIdsByCharacterId, byte[,] CharactersBySuccessorId,
            ShocoPack[] Packs)
        {
            if (MinimumCharacter < 0)
                throw new ArgumentOutOfRangeException(nameof(MinimumCharacter), MinimumCharacter, "Expected >= 0");
            if (MaximumCharacter < MinimumCharacter)
                throw new ArgumentOutOfRangeException(nameof(MaximumCharacter), MaximumCharacter, $"Expected > {nameof(MinimumCharacter)}");
            if (MaximumSuccessorLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(MaximumSuccessorLength), MaximumSuccessorLength, "Expected > 0");
            if (CharactersById == null || CharactersById.Length == 0)
                throw new ArgumentNullException(nameof(CharactersById));
            if (IdsByCharacter == null || IdsByCharacter.Length == 0)
                throw new ArgumentNullException(nameof(IdsByCharacter));

            if (IdsByCharacter.Length != 256)
                throw new ArgumentNullException(nameof(IdsByCharacter), "Expected array length == 256");

            int charactersCount = CharactersById.Length;
            int successorCount = MaximumCharacter - MinimumCharacter;
            if (SuccessorIdsByCharacterId.GetLength(0) != charactersCount)
                throw new ArgumentNullException(nameof(SuccessorIdsByCharacterId), "Expected consistent array length");
            if (CharactersBySuccessorId.GetLength(0) != MaximumCharacter - MinimumCharacter)
                throw new ArgumentNullException(nameof(CharactersBySuccessorId), $"Expected {nameof(CharactersBySuccessorId)}.Length == ({nameof(MaximumCharacter)} - {nameof(MinimumCharacter)})");

            if (Packs == null || Packs.Length == 0)
                throw new ArgumentNullException(nameof(Packs));

            minimumCharacter = MinimumCharacter;
            maximumCharacter = MaximumCharacter;

            maximumSuccessorLength = MaximumSuccessorLength;

            charactersById = CharactersById;
            idsByCharacter = IdsByCharacter;

            successorIdsByCharacterId = SuccessorIdsByCharacterId;
            charactersBySuccessorId = CharactersBySuccessorId;

            packs = Packs;
        }

        /// <summary>
        /// Compress an ArraySegment
        /// </summary>
        /// <param name="Data">Data to be compressed</param>
        /// <param name="OutputStream">Output stream for the compressed data</param>
        public void Compress(ArraySegment<byte> Data, Stream OutputStream)
        {
            if (Data.Array == null)
                throw new ArgumentNullException(nameof(Data));
            if (OutputStream == null)
                throw new ArgumentNullException(nameof(OutputStream));

            if (Data.Count == 0)
                return;

            int[] indices = new int[maximumSuccessorLength + 1];
            byte last_chr_index;
            byte current_index;
            byte successor_index;
            int n_consecutive;
            uint code_word;
            byte[] code_bytes = new byte[4];
            ShocoPack pack;
            int rest;
            var array = Data.Array;
            int array_length = Data.Offset + Data.Count;

            for (int index = Data.Offset; index < array_length;)
            {
                var current = array[index];

                // cannot encode null character/byte
                // indicates the end of a string (0 is the sentinel byte)
                if (current == 0)
                    return;

                last_chr_index = idsByCharacter[current];
                indices[0] = last_chr_index;

                if (last_chr_index != 0xFF)
                {

                    rest = array_length - index;

                    for (n_consecutive = 1; n_consecutive <= maximumSuccessorLength; ++n_consecutive)
                    {
                        if (n_consecutive == rest)
                            break;

                        current_index = idsByCharacter[array[index + n_consecutive]];
                        if (current_index == 0xFF)
                            break;

                        successor_index = successorIdsByCharacterId[last_chr_index, current_index];

                        if (successor_index == 0xFF)
                            break;

                        indices[n_consecutive] = successor_index;
                        last_chr_index = current_index;
                    }

                    if (n_consecutive >= 2)
                    {
                        pack = FindBestEncoding(indices, n_consecutive);
                        if (pack != null)
                        {
                            code_word = (uint)pack.Header << 24;
                            for (int i = 0; i < pack.BytesUnpacked; ++i)
                                code_word |= (uint)indices[i] << pack.offsets[i];

                            code_word.GetBigEndianBytes(code_bytes);
                            OutputStream.Write(code_bytes, 0, pack.BytesPacked);

                            index += pack.BytesUnpacked;
                            continue;
                        }
                    }
                }

                if ((current & 0x80) != 0)
                {
                    // non-ascii case - put in a sentinel byte
                    OutputStream.WriteByte(0);
                }
                OutputStream.WriteByte(current);
                index++;

            }
        }

        /// <summary>
        /// Decompresses an ArraySegment
        /// </summary>
        /// <param name="Data">Data to be decompressed</param>
        /// <param name="OutputStream">Output stream for the decompressed data</param>
        public void Decompress(ArraySegment<byte> Data, Stream OutputStream)
        {
            if (Data.Array == null)
                throw new ArgumentNullException(nameof(Data));
            if (OutputStream == null)
                throw new ArgumentNullException(nameof(OutputStream));

            if (Data.Count == 0)
                return;

            byte last_chr;
            uint code_word;
            int offset;
            int mask;
            int mark;
            var array = Data.Array;
            var array_length = Data.Offset + Data.Count;

            for (int index = Data.Offset; index < array_length;)
            {
                var current = array[index];

                mark = DecodeHeader(current);

                if (mark < 0)
                {
                    if (current == 0x00 && ++index >= array_length)
                        throw new ArgumentException("SIZE_MAX", nameof(Data));

                    OutputStream.WriteByte(array[index]);
                    index++;
                }
                else if (mark < 3)
                {
                    var pack = packs[mark];

                    if (index + pack.BytesPacked > array_length)
                        throw new ArgumentException("SIZE_MAX", nameof(Data));

                    code_word = array.ToUInt32(index);

                    // unpack the leading char
                    offset = pack.offsets[0];
                    mask = pack.masks[0];
                    last_chr = charactersById[(code_word >> offset) & mask];
                    OutputStream.WriteByte(last_chr);

                    // unpack the successor chars
                    for (int i = 1; i < pack.BytesUnpacked; ++i)
                    {
                        offset = pack.offsets[i];
                        mask = pack.masks[i];
                        last_chr = charactersBySuccessorId[last_chr - minimumCharacter, (code_word >> offset) & mask];
                        OutputStream.WriteByte(last_chr);
                    }

                    index += pack.BytesPacked;
                }
                else
                {
                    // See:
                    //  CVE-2017-11367
                    //  https://github.com/Ed-von-Schleck/shoco/issues/28
                    throw new ArgumentException("Data not encoded correctly", nameof(Data));
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int DecodeHeader(byte val)
        {
            int i = -1;
            while (val >= 128)
            {
                val <<= 1;
                ++i;
            }
            return i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ShocoPack FindBestEncoding(int[] Indices, int n_consecutive)
        {
            for (int i = packs.Length - 1; i >= 0; --i)
            {
                var p = packs[i];
                if ((n_consecutive >= p.BytesUnpacked) && (CheckIndices(Indices, p)))
                    return p;
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CheckIndices(int[] Indices, ShocoPack Pack)
        {
            // TODO: Investigate hardware acceleration

            for (int i = 0; i < Pack.BytesUnpacked; ++i)
                if (Indices[i] > Pack.masks[i])
                    return false;

            return true;
        }

    }
}
