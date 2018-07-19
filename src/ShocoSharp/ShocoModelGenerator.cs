using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static System.Diagnostics.Debug;

namespace ShocoSharp
{
    public static class ShocoModelGenerator
    {
        private const int MAX_CONSECUTIVES = 8;

        private static readonly char[] NewLineChars;
        private static readonly char[] WhitespaceChars;
        private static readonly char[] PunctuationChars;
        private static readonly char[] WhitespacePunctuationChars;
        private static readonly bool[] FalseCharLookup;
        private static readonly bool[] NewLineCharLookup;
        private static readonly bool[] WhitespaceCharLookup;
        private static readonly bool[] PunctuationCharLookup;
        private static readonly bool[] WhitespacePunctuationCharLookup;

        static ShocoModelGenerator()
        {
            NewLineChars = new char[] { '\n', '\r' };
            WhitespaceChars = new char[] { ' ', '\t', '\n', '\r', '\v', '\f', (char)0xC2, (char)0xAD };
            PunctuationChars = "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~".ToArray();
            WhitespacePunctuationChars = WhitespaceChars.Concat(PunctuationChars).ToArray();
            FalseCharLookup = new bool[256];
            NewLineCharLookup = new bool[256];
            WhitespaceCharLookup = new bool[256];
            PunctuationCharLookup = new bool[256];
            WhitespacePunctuationCharLookup = new bool[256];

            foreach (var c in NewLineChars)
                NewLineCharLookup[(byte)c] = true;
            foreach (var c in WhitespaceChars)
                WhitespaceCharLookup[(byte)c] = true;
            foreach (var c in PunctuationChars)
                PunctuationCharLookup[(byte)c] = true;
            foreach (var c in WhitespacePunctuationChars)
                WhitespacePunctuationCharLookup[(byte)c] = true;
        }

        /// <summary>
        /// Generates a ShocoModel from the contents of a file
        /// </summary>
        /// <param name="FilePath">Location to the training data</param>
        /// <param name="Options">Input processing (split/strip) options</param>
        /// <param name="MaximumLeadingBits">Maximum amount of bits that may be used for representing a leading character</param>
        /// <param name="MaxSuccessorBits">Maximum amount of bits that may be used for representing a successor character</param>
        /// <param name="EncodingTypes">Number of different encoding schemes (1-3)</param>
        /// <param name="OptimizeEncoding">Find the optimal packing structure for the training data</param>
        /// <returns>A ShocoModel trained with the input data</returns>
        public static ShocoModel GenerateModelFromFile(string FilePath, InputOptions Options = InputOptions.Default, int MaximumLeadingBits = 5, int MaxSuccessorBits = 4, int EncodingTypes = 3, bool OptimizeEncoding = false)
        {
            return GenerateModelFromFiles(new string[] { FilePath }, Options, MaximumLeadingBits, MaxSuccessorBits, EncodingTypes, OptimizeEncoding);
        }

        /// <summary>
        /// Generates a ShocoModel from the contents of file(s)
        /// </summary>
        /// <param name="FilePaths">Location of the training data</param>
        /// <param name="Options">Input processing (split/strip) options</param>
        /// <param name="MaximumLeadingBits">Maximum amount of bits that may be used for representing a leading character</param>
        /// <param name="MaxSuccessorBits">Maximum amount of bits that may be used for representing a successor character</param>
        /// <param name="EncodingTypes">Number of different encoding schemes (1-3)</param>
        /// <param name="OptimizeEncoding">Find the optimal packing structure for the training data</param>
        /// <returns>A ShocoModel trained with the input data</returns>
        public static ShocoModel GenerateModelFromFiles(IEnumerable<string> FilePaths, InputOptions Options = InputOptions.Default, int MaximumLeadingBits = 5, int MaxSuccessorBits = 4, int EncodingTypes = 3, bool OptimizeEncoding = false)
        {
            IEnumerable<Stream> getStreams()
            {
                foreach (var filePath in FilePaths)
                {
                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        yield return stream;
                    }
                }
            }

            return GenerateModel(getStreams(), Options, MaximumLeadingBits, MaxSuccessorBits, EncodingTypes, OptimizeEncoding);
        }

        /// <summary>
        /// Generates a ShocoModel from a string using UTF8 encoding
        /// </summary>
        /// <param name="Input">Training data</param>
        /// <param name="Options">Input processing (split/strip) options</param>
        /// <param name="MaximumLeadingBits">Maximum amount of bits that may be used for representing a leading character</param>
        /// <param name="MaxSuccessorBits">Maximum amount of bits that may be used for representing a successor character</param>
        /// <param name="EncodingTypes">Number of different encoding schemes (1-3)</param>
        /// <param name="OptimizeEncoding">Find the optimal packing structure for the training data</param>
        /// <returns>A ShocoModel trained with the input data</returns>
        public static ShocoModel GenerateModel(string Input, InputOptions Options = InputOptions.Default, int MaximumLeadingBits = 5, int MaxSuccessorBits = 4, int EncodingTypes = 3, bool OptimizeEncoding = false)
        {
            return GenerateModel(new string[] { Input }, Encoding.UTF8, Options, MaximumLeadingBits, MaxSuccessorBits, EncodingTypes, OptimizeEncoding);
        }

        /// <summary>
        /// Generates a ShocoModel from strings using UTF8 encoding
        /// </summary>
        /// <param name="Input">Training data</param>
        /// <param name="Options">Input processing (split/strip) options</param>
        /// <param name="MaximumLeadingBits">Maximum amount of bits that may be used for representing a leading character</param>
        /// <param name="MaxSuccessorBits">Maximum amount of bits that may be used for representing a successor character</param>
        /// <param name="EncodingTypes">Number of different encoding schemes (1-3)</param>
        /// <param name="OptimizeEncoding">Find the optimal packing structure for the training data</param>
        /// <returns>A ShocoModel trained with the input data</returns>
        public static ShocoModel GenerateModel(IEnumerable<string> Input, InputOptions Options = InputOptions.Default, int MaximumLeadingBits = 5, int MaxSuccessorBits = 4, int EncodingTypes = 3, bool OptimizeEncoding = false)
        {
            return GenerateModel(Input, Encoding.UTF8, Options, MaximumLeadingBits, MaxSuccessorBits, EncodingTypes, OptimizeEncoding);
        }

        /// <summary>
        /// Generates a ShocoModel from a string
        /// </summary>
        /// <param name="Input">Training data</param>
        /// <param name="InputEncoding">Encoding used to convert data to bytes</param>
        /// <param name="Options">Input processing (split/strip) options</param>
        /// <param name="MaximumLeadingBits">Maximum amount of bits that may be used for representing a leading character</param>
        /// <param name="MaxSuccessorBits">Maximum amount of bits that may be used for representing a successor character</param>
        /// <param name="EncodingTypes">Number of different encoding schemes (1-3)</param>
        /// <param name="OptimizeEncoding">Find the optimal packing structure for the training data</param>
        /// <returns>A ShocoModel trained with the input data</returns>
        public static ShocoModel GenerateModel(string Input, Encoding InputEncoding, InputOptions Options = InputOptions.Default, int MaximumLeadingBits = 5, int MaxSuccessorBits = 4, int EncodingTypes = 3, bool OptimizeEncoding = false)
        {
            return GenerateModel(new string[] { Input }, InputEncoding, Options, MaximumLeadingBits, MaxSuccessorBits, EncodingTypes, OptimizeEncoding);
        }

        /// <summary>
        /// Generates a ShocoModel from strings
        /// </summary>
        /// <param name="Input">Training data</param>
        /// <param name="InputEncoding">Encoding used to convert data to bytes</param>
        /// <param name="Options">Input processing (split/strip) options</param>
        /// <param name="MaximumLeadingBits">Maximum amount of bits that may be used for representing a leading character</param>
        /// <param name="MaxSuccessorBits">Maximum amount of bits that may be used for representing a successor character</param>
        /// <param name="EncodingTypes">Number of different encoding schemes (1-3)</param>
        /// <param name="OptimizeEncoding">Find the optimal packing structure for the training data</param>
        /// <returns>A ShocoModel trained with the input data</returns>
        public static ShocoModel GenerateModel(IEnumerable<string> Input, Encoding InputEncoding, InputOptions Options = InputOptions.Default, int MaximumLeadingBits = 5, int MaxSuccessorBits = 4, int EncodingTypes = 3, bool OptimizeEncoding = false)
        {
            var segments = CalculateSegments(Input, InputEncoding, Options);
            return GenerateModelFromSegments(segments, MaximumLeadingBits, MaxSuccessorBits, EncodingTypes, OptimizeEncoding);
        }

        /// <summary>
        /// Generates a ShocoModel from a stream
        /// </summary>
        /// <param name="Input">Training data</param>
        /// <param name="Options">Input processing (split/strip) options</param>
        /// <param name="MaximumLeadingBits">Maximum amount of bits that may be used for representing a leading character</param>
        /// <param name="MaxSuccessorBits">Maximum amount of bits that may be used for representing a successor character</param>
        /// <param name="EncodingTypes">Number of different encoding schemes (1-3)</param>
        /// <param name="OptimizeEncoding">Find the optimal packing structure for the training data</param>
        /// <returns>A ShocoModel trained with the input data</returns>
        public static ShocoModel GenerateModel(Stream Input, InputOptions Options = InputOptions.Default, int MaximumLeadingBits = 5, int MaxSuccessorBits = 4, int EncodingTypes = 3, bool OptimizeEncoding = false)
        {
            return GenerateModel(new Stream[] { Input }, Options, MaximumLeadingBits, MaxSuccessorBits, EncodingTypes, OptimizeEncoding);
        }

        /// <summary>
        /// Generates a ShocoModel from streams
        /// </summary>
        /// <param name="Input">Training data</param>
        /// <param name="Options">Input processing (split/strip) options</param>
        /// <param name="MaximumLeadingBits">Maximum amount of bits that may be used for representing a leading character</param>
        /// <param name="MaxSuccessorBits">Maximum amount of bits that may be used for representing a successor character</param>
        /// <param name="EncodingTypes">Number of different encoding schemes (1-3)</param>
        /// <param name="OptimizeEncoding">Find the optimal packing structure for the training data</param>
        /// <returns>A ShocoModel trained with the input data</returns>
        public static ShocoModel GenerateModel(IEnumerable<Stream> Input, InputOptions Options = InputOptions.Default, int MaximumLeadingBits = 5, int MaxSuccessorBits = 4, int EncodingTypes = 3, bool OptimizeEncoding = false)
        {
            var segments = CalculateSegments(Input, Options);
            return GenerateModelFromSegments(segments, MaximumLeadingBits, MaxSuccessorBits, EncodingTypes, OptimizeEncoding);
        }

        /// <summary>
        /// Generates a ShocoModel from a byte array
        /// </summary>
        /// <param name="Input">Training data</param>
        /// <param name="Options">Input processing (split/strip) options</param>
        /// <param name="MaximumLeadingBits">Maximum amount of bits that may be used for representing a leading character</param>
        /// <param name="MaxSuccessorBits">Maximum amount of bits that may be used for representing a successor character</param>
        /// <param name="EncodingTypes">Number of different encoding schemes (1-3)</param>
        /// <param name="OptimizeEncoding">Find the optimal packing structure for the training data</param>
        /// <returns>A ShocoModel trained with the input data</returns>
        public static ShocoModel GenerateModel(byte[] Input, InputOptions Options = InputOptions.Default, int MaximumLeadingBits = 5, int MaxSuccessorBits = 4, int EncodingTypes = 3, bool OptimizeEncoding = false)
        {
            return GenerateModel(new ArraySegment<byte>(Input, 0, Input.Length), Options, MaximumLeadingBits, MaxSuccessorBits, EncodingTypes, OptimizeEncoding);
        }

        /// <summary>
        /// Generates a ShocoModel from byte arrays
        /// </summary>
        /// <param name="Input">Training data</param>
        /// <param name="Options">Input processing (split/strip) options</param>
        /// <param name="MaximumLeadingBits">Maximum amount of bits that may be used for representing a leading character</param>
        /// <param name="MaxSuccessorBits">Maximum amount of bits that may be used for representing a successor character</param>
        /// <param name="EncodingTypes">Number of different encoding schemes (1-3)</param>
        /// <param name="OptimizeEncoding">Find the optimal packing structure for the training data</param>
        /// <returns>A ShocoModel trained with the input data</returns>
        public static ShocoModel GenerateModel(IEnumerable<byte[]> Input, InputOptions Options = InputOptions.Default, int MaximumLeadingBits = 5, int MaxSuccessorBits = 4, int EncodingTypes = 3, bool OptimizeEncoding = false)
        {
            IEnumerable<ArraySegment<byte>> getSegmentInput()
            {
                foreach (var input in Input)
                    yield return new ArraySegment<byte>(input, 0, input.Length);
            }

            return GenerateModelFromSegments(getSegmentInput(), Options, MaximumLeadingBits, MaxSuccessorBits, EncodingTypes, OptimizeEncoding);
        }

        /// <summary>
        /// Generates a ShocoModel from an ArraySegment
        /// </summary>
        /// <param name="Input">Training data</param>
        /// <param name="Options">Input processing (split/strip) options</param>
        /// <param name="MaximumLeadingBits">Maximum amount of bits that may be used for representing a leading character</param>
        /// <param name="MaxSuccessorBits">Maximum amount of bits that may be used for representing a successor character</param>
        /// <param name="EncodingTypes">Number of different encoding schemes (1-3)</param>
        /// <param name="OptimizeEncoding">Find the optimal packing structure for the training data</param>
        /// <returns>A ShocoModel trained with the input data</returns>
        public static ShocoModel GenerateModel(ArraySegment<byte> Input, InputOptions Options = InputOptions.Default, int MaximumLeadingBits = 5, int MaxSuccessorBits = 4, int EncodingTypes = 3, bool OptimizeEncoding = false)
        {
            return GenerateModelFromSegments(new ArraySegment<byte>[] { Input }, Options, MaximumLeadingBits, MaxSuccessorBits, EncodingTypes, OptimizeEncoding);
        }

        /// <summary>
        /// Generates a ShocoModel from ArraySegments
        /// </summary>
        /// <param name="Input">Training data</param>
        /// <param name="Options">Input processing (split/strip) options</param>
        /// <param name="MaximumLeadingBits">Maximum amount of bits that may be used for representing a leading character</param>
        /// <param name="MaxSuccessorBits">Maximum amount of bits that may be used for representing a successor character</param>
        /// <param name="EncodingTypes">Number of different encoding schemes (1-3)</param>
        /// <param name="OptimizeEncoding">Find the optimal packing structure for the training data</param>
        /// <returns>A ShocoModel trained with the input data</returns>
        public static ShocoModel GenerateModel(IEnumerable<ArraySegment<byte>> Input, InputOptions Options = InputOptions.Default, int MaximumLeadingBits = 5, int MaxSuccessorBits = 4, int EncodingTypes = 3, bool OptimizeEncoding = false)
        {
            return GenerateModelFromSegments(Input, Options, MaximumLeadingBits, MaxSuccessorBits, EncodingTypes, OptimizeEncoding);
        }

        private static IEnumerable<ArraySegment<byte>> CalculateSegments(IEnumerable<Stream> Input, InputOptions Options)
        {
            var segment = new byte[128];
            var buffer = new byte[2048];
            var splitLookup = FalseCharLookup;
            var stripLookup = FalseCharLookup;

            if (Options.HasFlag(InputOptions.SplitWhitespaceAndNewLine))
                splitLookup = WhitespaceCharLookup;
            else if (Options.HasFlag(InputOptions.SplitNewLine))
                splitLookup = NewLineCharLookup;

            if (Options.HasFlag(InputOptions.StripWhitespaceAndPunctuation))
                stripLookup = WhitespacePunctuationCharLookup;
            else if (Options.HasFlag(InputOptions.StripWhitespace))
                stripLookup = WhitespaceCharLookup;
            else if (Options.HasFlag(InputOptions.StripPunctuation))
                stripLookup = PunctuationCharLookup;

            foreach (var stream in Input)
            {
                int segmentIndex = 0;
                var bufferLength = stream.Read(buffer, 0, buffer.Length);
                while (bufferLength > 0)
                {
                    for (int index = 0; index < bufferLength; index++)
                    {
                        var c = buffer[index];
                        if (splitLookup[c])
                        {
                            // trim end
                            for (segmentIndex--; segmentIndex > 0; segmentIndex--)
                                if (!stripLookup[segment[segmentIndex]])
                                    break;

                            if (segmentIndex++ > 0)
                                yield return new ArraySegment<byte>(segment, 0, segmentIndex);

                            segmentIndex = 0;
                            continue;
                        }
                        if (segmentIndex > 0)
                        {
                            if (segment.Length <= segmentIndex)
                                Array.Resize(ref segment, segment.Length * 2);

                            segment[segmentIndex++] = c;
                            continue;
                        }
                        if (stripLookup[c])
                            continue; // trim start
                        else
                            segment[segmentIndex++] = c;
                    }
                    bufferLength = stream.Read(buffer, 0, buffer.Length);
                }

                if (segmentIndex++ > 0)
                    yield return new ArraySegment<byte>(segment, 0, segmentIndex);
            }
        }

        private static IEnumerable<ArraySegment<byte>> CalculateSegments(IEnumerable<string> Input, Encoding InputEncoding, InputOptions Options)
        {
            var input = Input;

            if (Options.HasFlag(InputOptions.SplitWhitespaceAndNewLine))
                input = input.SelectMany(i => i.Split(WhitespaceChars, StringSplitOptions.RemoveEmptyEntries));
            else if (Options.HasFlag(InputOptions.SplitNewLine))
                input = input.SelectMany(i => i.Split(NewLineChars, StringSplitOptions.RemoveEmptyEntries));
            if (Options.HasFlag(InputOptions.StripWhitespaceAndPunctuation))
                input = input.Select(i => i.Trim(WhitespacePunctuationChars));
            else if (Options.HasFlag(InputOptions.StripWhitespace))
                input = input.Select(i => i.Trim(WhitespaceChars));
            else if (Options.HasFlag(InputOptions.StripPunctuation))
                input = input.Select(i => i.Trim(PunctuationChars));

            var buffer = new byte[2048];
            foreach (var segment in input)
            {
                if (buffer.Length < InputEncoding.GetMaxByteCount(segment.Length))
                    buffer = new byte[InputEncoding.GetMaxByteCount(segment.Length)];

                var length = InputEncoding.GetBytes(segment, 0, segment.Length, buffer, 0);

                if (length <= 1)
                    continue;

                yield return new ArraySegment<byte>(buffer, 0, length);
            }
        }

        private static IEnumerable<ArraySegment<byte>> CalculateSegmentSplit(IEnumerable<ArraySegment<byte>> Input, InputOptions Options)
        {
            if (Options.HasFlag(InputOptions.SplitWhitespaceAndNewLine))
                foreach (var segment in Input)
                {
                    var offset = segment.Offset;
                    var buffer = segment.Array;
                    var index = offset;
                    for (; index < segment.Count; index++)
                    {
                        if (WhitespaceCharLookup[buffer[index]])
                        {
                            if (offset < index)
                            {
                                yield return new ArraySegment<byte>(buffer, offset, offset - index);
                                offset = index + 1;
                            }
                        }
                    }
                    if (offset < index)
                        yield return new ArraySegment<byte>(buffer, offset, offset - index);
                }
            else if (Options.HasFlag(InputOptions.SplitNewLine))
                foreach (var segment in Input)
                {
                    var offset = segment.Offset;
                    var buffer = segment.Array;
                    var index = offset;
                    for (; index < segment.Count; index++)
                    {
                        if (buffer[index] == 10 || // '\n'
                            buffer[index] == 13)   // '\r'
                        {
                            if (offset < index)
                            {
                                yield return new ArraySegment<byte>(buffer, offset, offset - index);
                                offset = index + 1;
                            }
                        }
                    }
                    if (offset < index)
                        yield return new ArraySegment<byte>(buffer, offset, offset - index);
                }
            else
                foreach (var segment in Input)
                    yield return segment;
        }

        private static IEnumerable<ArraySegment<byte>> CalculateSegmentStrip(IEnumerable<ArraySegment<byte>> Input, InputOptions Options)
        {
            bool[] lookup;
            if (Options.HasFlag(InputOptions.StripWhitespaceAndPunctuation))
                lookup = WhitespacePunctuationCharLookup;
            else if (Options.HasFlag(InputOptions.StripWhitespace))
                lookup = WhitespaceCharLookup;
            else if (Options.HasFlag(InputOptions.StripPunctuation))
                lookup = PunctuationCharLookup;
            else
            {
                foreach (var segment in Input)
                    yield return segment;
                yield break;
            }

            if (lookup == null || lookup.Length < 256)
                throw new InvalidOperationException("Lookup should never be null");

            foreach (var segment in Input)
            {
                // trim start
                var offset = segment.Offset;
                var end = offset + segment.Count;
                var buffer = segment.Array;
                for (; offset < end; offset++)
                {
                    if (!lookup[buffer[offset]])
                        goto trimEnd;
                }
                continue;
                trimEnd:
                end--;
                for (; end > offset; end--)
                {
                    if (!lookup[buffer[end]])
                    {
                        yield return new ArraySegment<byte>(buffer, offset, end - offset);
                        continue;
                    }
                }
            }
        }

        private static ShocoModel GenerateModelFromSegments(IEnumerable<ArraySegment<byte>> Input, InputOptions Options, int MaximumLeadingBits, int MaxSuccessorBits, int EncodingTypes, bool OptimizeEncoding)
        {
            var input = Input;

            if (Options.HasFlag(InputOptions.SplitNewLine) || Options.HasFlag(InputOptions.SplitWhitespaceAndNewLine))
                input = CalculateSegmentSplit(input, Options);
            if (Options.HasFlag(InputOptions.StripPunctuation) || Options.HasFlag(InputOptions.StripWhitespace))
                input = CalculateSegmentStrip(input, Options);

            return GenerateModelFromSegments(input, MaximumLeadingBits, MaxSuccessorBits, EncodingTypes, OptimizeEncoding);
        }

        private static ShocoModel GenerateModelFromSegments(IEnumerable<ArraySegment<byte>> Input, int MaximumLeadingBits, int MaxSuccessorBits, int EncodingTypes, bool OptimizeEncoding)
        {
            if (EncodingTypes < 1 || EncodingTypes > 3)
                throw new ArgumentOutOfRangeException(nameof(EncodingTypes), "Expected 1, 2, or 3");

            var chars_count = 1 << MaximumLeadingBits;
            var successors_count = 1 << MaxSuccessorBits;

            var bigram_counters = new Dictionary<byte, Counter<byte>>(256);
            var first_char_counter = new Counter<byte>(256);

            Write("finding bigrams...");
            byte t1, t2;
            foreach (var segment in Input)
            {
                if (segment.Count <= 1)
                    continue;
                var segmentEnd = segment.Offset + segment.Count;
                t1 = segment.Array[segment.Offset];
                for (int i = segment.Offset + 1; i < segmentEnd; i++)
                {
                    t2 = segment.Array[i];
                    first_char_counter[t1]++;
                    if (!bigram_counters.TryGetValue(t1, out var counter))
                        bigram_counters[t1] = counter = new Counter<byte>();
                    counter[t2]++;
                    t1 = t2;
                }
            }
            WriteLine(" done.");

            Write("generating list of most common bytes...");
            var successors = new Dictionary<byte, byte[]>();
            foreach (var tup in first_char_counter.MostCommon(chars_count))
            {
                var s = bigram_counters[tup.Key]
                    .MostCommon(successors_count)
                    .Select(o => o.Key).ToArray();

                if (s.Length != successors_count)
                {
                    Array.Resize(ref s, successors_count);
                }
                successors[tup.Key] = s;
            }
            WriteLine(" done.");

            var successor_keys = successors.Keys.ToArray();

            var max_chr = successor_keys.Max() + 1;
            var min_chr = successor_keys.Min();

            var chrs_indices = successor_keys
                .Select((key, index) => new KeyValuePair<byte, int>(key, index))
                .ToDictionary(k => k.Key, k => k.Value);
            var chrs_reversed = Enumerable.Repeat((byte)0xFF, 256).ToArray();
            for (int i = 0; i < successor_keys.Length; i++)
            {
                chrs_reversed[successor_keys[i]] = (byte)i;
            }

            var successors_reversed = new byte[successors.Count, chars_count];
            for (int i = 0; i < successor_keys.Length; i++)
            {
                var successor_key = successor_keys[i];
                var successor = successors[successor_key];

                var s_indices = successor
                    .Select((key, index) => new KeyValuePair<byte, byte>(key, (byte)index))
                    .Where(k => k.Key != 0)
                    .ToDictionary(k => k.Key, k => k.Value);

                for (int v = 0; v < successor_keys.Length; v++)
                {
                    if (!s_indices.TryGetValue(successor_keys[v], out var c))
                        c = 0xFF;
                    successors_reversed[i, v] = c;
                }
            }

            var chrs_by_chr_and_successor_id = new byte[max_chr - min_chr, successors_count];
            for (int i = min_chr; i < max_chr; i++)
            {
                if (successors.TryGetValue((byte)i, out var values))
                {
                    Buffer.BlockCopy(values, 0, chrs_by_chr_and_successor_id, (i - min_chr) * successors_count, values.Length);
                }
            }

            int max_encoding_length;
            ShocoPacking[] best_encodings;

            if (OptimizeEncoding)
            {
                Write("finding best packing structures...");

                best_encodings = CalculateOptimizedEncoding(Input, MaximumLeadingBits, MaxSuccessorBits, EncodingTypes, chrs_indices, successors, out max_encoding_length);

                WriteLine(" done.");
            }
            else
            {
                max_encoding_length = 8;
                best_encodings = new ShocoPacking[]
                {
                    new ShocoPacking(new int[] { 2, 4, 2, 0, 0, 0, 0, 0, 0 }),
                    new ShocoPacking(new int[] { 3, 4, 3, 3, 3, 0, 0, 0, 0 }),
                    new ShocoPacking(new int[] { 4, 5, 4, 4, 4, 3, 3, 3, 2 })
                };
                if (EncodingTypes != best_encodings.Length)
                    Array.Resize(ref best_encodings, EncodingTypes);
            }

            return new ShocoModel(
                MinimumCharacter: min_chr,
                MaximumCharacter: max_chr,
                MaximumSuccessorLength: max_encoding_length - 1,
                CharactersById: successor_keys,
                IdsByCharacter: chrs_reversed,
                SuccessorIdsByCharacterId: successors_reversed,
                CharactersBySuccessorId: chrs_by_chr_and_successor_id,
                Packs: best_encodings.Select(e => e.ToPack()).ToArray());
        }

        private static ShocoPacking[] CalculateOptimizedEncoding(IEnumerable<ArraySegment<byte>> Input, int MaximumLeadingBits, int MaxSuccessorBits, int EncodingTypes, Dictionary<byte, int> CharacterIndicies, Dictionary<byte, byte[]> Successors, out int max_encoding_length)
        {
            var packs = new List<Tuple<int, int[][]>>()
            {
                Tuple.Create(1, new int[][] {
                    new int[] { 2, 4, 2 },
                    new int[] { 2, 3, 3 },
                    new int[] { 2, 4, 1, 1 },
                    new int[] { 2, 3, 2, 1 },
                    new int[] { 2, 2, 2, 2 },
                    new int[] { 2, 3, 1, 1, 1 },
                    new int[] { 2, 2, 2, 1, 1 },
                    new int[] { 2, 2, 1, 1, 1, 1 },
                    new int[] { 2, 1, 1, 1, 1, 1, 1 } }),
                Tuple.Create(2, new int[][] {
                    new int[] { 3, 5, 4, 2, 2 },
                    new int[] { 3, 5, 3, 3, 2 },
                    new int[] { 3, 4, 4, 3, 2 },
                    new int[] { 3, 4, 3, 3, 3 },
                    new int[] { 3, 5, 3, 2, 2, 1 },
                    new int[] { 3, 5, 2, 2, 2, 2 },
                    new int[] { 3, 4, 4, 2, 2, 1 },
                    new int[] { 3, 4, 3, 2, 2, 2 },
                    new int[] { 3, 4, 3, 3, 2, 1 },
                    new int[] { 3, 4, 2, 2, 2, 2 },
                    new int[] { 3, 3, 3, 3, 2, 2 },
                    new int[] { 3, 4, 3, 2, 2, 1, 1 },
                    new int[] { 3, 4, 2, 2, 2, 2, 1 },
                    new int[] { 3, 3, 3, 2, 2, 2, 1 },
                    new int[] { 3, 3, 2, 2, 2, 2, 2 },
                    new int[] { 3, 2, 2, 2, 2, 2, 2 },
                    new int[] { 3, 3, 3, 2, 2, 1, 1, 1 },
                    new int[] { 3, 3, 2, 2, 2, 2, 1, 1 },
                    new int[] { 3, 2, 2, 2, 2, 2, 2, 1 } } ),
                Tuple.Create( 4, new int[][] {
                    new int[] { 4, 5, 4, 4, 4, 3, 3, 3, 2 },
                    new int[] { 4, 5, 5, 4, 4, 3, 3, 2, 2 },
                    new int[] { 4, 4, 4, 4, 4, 4, 3, 3, 2 },
                    new int[] { 4, 4, 4, 4, 4, 3, 3, 3, 3 } } ),
            };
            var candidates = packs.Take(EncodingTypes)
                .SelectMany(g => g.Item2.Select(p => new ShocoPacking(p)))
                .Where(p => p.lead <= MaximumLeadingBits && p.consecutiveMax <= MaxSuccessorBits)
                .ToArray();
            var counters = packs.Take(EncodingTypes).ToDictionary(g => g.Item1, g => new FloatCounter<ShocoPacking>());

            foreach (var segment in Input)
                for (int i = 0; i < segment.Count; i++)
                {
                    var subSegment = new ArraySegment<byte>(segment.Array, segment.Offset + i, segment.Count - i);
                    foreach (var candidate in candidates)
                        if (candidate.CanEncode(subSegment, Successors, CharacterIndicies))
                            counters[candidate.packed][candidate] += candidate.ratio;
                }

            var best_encodings_raw = counters.ToDictionary(g => g.Key, g => g.Value.MostCommon(1).First().Key);
            max_encoding_length = best_encodings_raw.Select(e => e.Value.unpacked).Max();
            var best_encodings = best_encodings_raw.Select(p =>
                new ShocoPacking(p.Value.encoding.MakeCopy(p.Value.encoding.Length + (MAX_CONSECUTIVES - p.Value.unpacked))))
                .ToArray();

            return best_encodings;
        }

        private class ShocoPacking
        {
            public readonly int[] encoding;

            public readonly int packed;
            public readonly int unpacked;
            public readonly int lead;
            public readonly int consecutiveMax;
            public readonly float ratio;
            public readonly int[] masks;
            public readonly int[] offsets;

            byte header_code => (byte)(((1 << encoding[0]) - 2) << (8 - encoding[0]));

            public ShocoPacking(int[] Encoding)
            {
                encoding = Encoding;

                packed = Encoding.Sum() / 8;
                unpacked = Encoding.Count(bits => bits != 0) - 1;

                lead = Encoding[1];
                consecutiveMax = Encoding.Skip(1).Max();

                ratio = packed / (float)unpacked;

                masks = Encoding.Skip(1).Select(bits => ((1 << bits) - 1)).ToArray();

                offsets = new int[Encoding.Length - 1];
                var total = Encoding[0];
                for (int i = 0; i < offsets.Length; i++)
                {
                    total += Encoding[i + 1];
                    offsets[i] = 32 - total;
                }
            }

            public bool CanEncode(ArraySegment<byte> Segment, Dictionary<byte, byte[]> Successors, Dictionary<byte, int> CharacterIndicies)
            {
                if (Segment.Count < unpacked)
                    return false;
                if (!CharacterIndicies.TryGetValue(Segment.Array[Segment.Offset], out var lead_index))
                    return false;
                if (lead_index > (1 << encoding[0]))
                    return false;
                var last_index = lead_index;
                var last_char = Segment.Array[Segment.Offset];

                for (int s = 1; s < unpacked; s++)
                {
                    var b = encoding[s];
                    var c = Segment.Array[Segment.Offset + s];
                    if (!Successors.ContainsKey(last_char) || !Successors[last_char].Contains(c))
                        return false;
                    var successor_index = Successors[last_char].IndexOf(c);
                    if (successor_index > (1 << b))
                        return false;
                    last_index = successor_index;
                    last_char = c;
                }

                return true;
            }

            public ShocoPack ToPack()
            {
                return new ShocoPack(
                    Header: header_code,
                    BytesPacked: packed,
                    BytesUnpacked: unpacked,
                    Offsets: offsets,
                    Masks: masks
                    );
            }
        }

        private class Counter<T> : IEnumerable<KeyValuePair<T, int>>
        {
            private readonly Dictionary<T, int> counter;

            public Counter() => counter = new Dictionary<T, int>();
            public Counter(int Capacity) => counter = new Dictionary<T, int>(Capacity);

            public int this[T item]
            {
                get => counter.TryGetValue(item, out var v) ? v : 0;
                set => counter[item] = value;
            }

            public int Count => counter.Count;

            public IEnumerable<KeyValuePair<T, int>> MostCommon(int Count)
                => counter.OrderByDescending(k => k.Value).Take(Count);

            public IEnumerator<KeyValuePair<T, int>> GetEnumerator()
                => counter.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();
        }

        private class FloatCounter<T> : IEnumerable<KeyValuePair<T, float>>
        {
            private readonly Dictionary<T, float> counter;

            public FloatCounter() => counter = new Dictionary<T, float>();
            public FloatCounter(int Capacity) => counter = new Dictionary<T, float>(Capacity);

            public float this[T item]
            {
                get => counter.TryGetValue(item, out var v) ? v : 0;
                set => counter[item] = value;
            }

            public int Count => counter.Count;

            public IEnumerable<KeyValuePair<T, float>> MostCommon(int Count)
                => counter.OrderByDescending(k => k.Value).Take(Count);

            public IEnumerator<KeyValuePair<T, float>> GetEnumerator()
                => counter.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();
        }

    }
}
