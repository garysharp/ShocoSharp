using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ShocoSharp
{
    public static class ShocoModelExtensions
    {
        /// <summary>
        /// Default namespace used when writing a C# Class
        /// </summary>
        public const string DefaultCSharpNamespace = "ShocoSharp.Models";

        /// <summary>
        /// Writes a ShocoModel as a C# Class
        /// </summary>
        /// <param name="model">ShocoModel to generate the class from</param>
        /// <param name="Writer">Destination for the generated class</param>
        /// <param name="Name">Name of the C# Class</param>
        /// <param name="Namespace">Namespace for the C# Class</param>
        public static void WriteAsCSharpClass(this ShocoModel model, TextWriter Writer, string Name, string Namespace)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            if (Writer == null)
                throw new ArgumentNullException(nameof(Writer));
            if (string.IsNullOrWhiteSpace(Name))
                throw new ArgumentNullException(nameof(Name));
            if (string.IsNullOrWhiteSpace(Namespace))
                Namespace = DefaultCSharpNamespace;

            Writer.WriteLine($"namespace {Namespace}");
            Writer.WriteLine("{");
            if (Namespace != DefaultCSharpNamespace)
            {
                Writer.WriteLine($"    using {DefaultCSharpNamespace};");
                Writer.WriteLine();
            }
            Writer.WriteLine($"    public class {Name} : ShocoModel");
            Writer.WriteLine("    {");

            Writer.WriteLine($"        public static {Name} Instance {{ get; }} = new {Name}();");
            Writer.WriteLine();
            Writer.WriteLine($"        private {Name}()");
            Writer.WriteLine($"            : base(MinimumCharacter: {model.MinimumCharacter},");
            Writer.WriteLine($"                  MaximumCharacter: {model.MaximumCharacter},");
            Writer.WriteLine($"                  MaximumSuccessorLength: {model.MaximumSuccessorLength},");

            Writer.WriteLine($"                  CharactersById: new byte[{model.CharactersById.Length}] {{");
            for (int offset = 0; offset < model.CharactersById.Length; offset += 11)
            {
                if (offset != 0)
                    Writer.WriteLine(",");
                Writer.Write($"                      {string.Join(", ", model.CharactersById.Skip(offset).Take(11).Select(EscapeCSharp))}");
            }
            Writer.WriteLine(" },");
            var idsByCharacter = model.IdsByCharacter;
            Writer.Write($"                  IdsByCharacter: new byte[256] {{");
            for (int offset = 0; offset < idsByCharacter.Length; offset += 32)
            {
                if (offset != 0)
                    Writer.Write(",");
                Writer.WriteLine();
                Writer.Write($"                      {string.Join(", ", idsByCharacter.Skip(offset).Take(32).Select(FormatAsCSharpByte))}");
            }
            Writer.WriteLine(" },");

            var successorIdsByCharacterId = model.SuccessorIdsByCharacterId;
            Writer.Write($"                  SuccessorIdsByCharacterId: new byte[{successorIdsByCharacterId.GetLength(0)}, {successorIdsByCharacterId.GetLength(1)}] {{");
            for (int offset = 0; offset < successorIdsByCharacterId.GetLength(0); offset++)
            {
                if (offset != 0)
                    Writer.Write(",");
                Writer.WriteLine();
                Writer.Write($"                      {{{string.Join(", ", successorIdsByCharacterId.Cast<byte>().Skip(offset * successorIdsByCharacterId.GetLength(1)).Take(successorIdsByCharacterId.GetLength(1)).Select(FormatAsCSharpByte))}}}");
            }
            Writer.WriteLine(" },");

            var charactersBySuccessorId = model.CharactersBySuccessorId;
            Writer.Write($"                  CharactersBySuccessorId: new byte[{charactersBySuccessorId.GetLength(0)}, {charactersBySuccessorId.GetLength(1)}] {{");
            for (int offset = 0; offset < charactersBySuccessorId.GetLength(0); offset++)
            {
                if (offset != 0)
                    Writer.Write(",");
                Writer.WriteLine();
                Writer.Write($"                      {{{string.Join(", ", charactersBySuccessorId.Cast<byte>().Skip(offset * charactersBySuccessorId.GetLength(1)).Take(charactersBySuccessorId.GetLength(1)).Select(EscapeCSharp))}}}");
            }
            Writer.WriteLine(" },");

            var packs = model.Packs;
            Writer.Write($"                  Packs: new ShocoPack[] {{");
            for (int index = 0; index < packs.Length; index++)
            {
                var pack = packs[index];
                if (index != 0)
                    Writer.Write(",");
                Writer.WriteLine();
                Writer.Write($"                      new ShocoPack(0x{pack.Header:X2}, {pack.BytesPacked}, {pack.BytesUnpacked}, new int[] {{ {string.Join(", ", pack.offsets)} }}, new int[] {{ {string.Join(", ", pack.masks)} }})");
            }
            Writer.WriteLine(" })");
            Writer.WriteLine($"        {{ }}");
            Writer.WriteLine();
            Writer.WriteLine("    }");
            Writer.WriteLine("}");

        }

        /// <summary>
        /// Writes a ShocoModel as a C# Class
        /// </summary>
        /// <param name="model">ShocoModel to generate the class from</param>
        /// <param name="Filename">Destination for the generated class</param>
        /// <param name="Name">Name of the C# Class</param>
        /// <param name="Namespace">Namespace for the C# Class</param>
        public static void WriteAsCSharpClass(this ShocoModel model, string Filename, string Name, string Namespace)
        {
            using (var streamWriter = File.CreateText(Filename))
            {
                WriteAsCSharpClass(model, streamWriter, Name, Namespace);
            }
        }

        /// <summary>
        /// Writes a ShocoModel as a C# Class using the default namespace
        /// </summary>
        /// <param name="model">ShocoModel to generate the class from</param>
        /// <param name="Writer">Destination for the generated class</param>
        /// <param name="Name">Name of the C# Class</param>
        public static void WriteAsCSharpClass(this ShocoModel model, TextWriter Writer, string Name)
        {
            WriteAsCSharpClass(model, Writer, Name, Namespace: null);
        }

        /// <summary>
        /// Writes a ShocoModel as a C# Class using the default namespace
        /// </summary>
        /// <param name="model">ShocoModel to generate the class from</param>
        /// <param name="Filename">Destination for the generated class</param>
        /// <param name="Name">Name of the C# Class</param>
        public static void WriteAsCSharpClass(this ShocoModel model, string Filename, string Name)
        {
            WriteAsCSharpClass(model, Filename, Name, Namespace: null);
        }

        /// <summary>
        /// Writes a ShocoModel as a C Header
        /// </summary>
        /// <param name="model">ShocoModel to generate the header from</param>
        /// <param name="Writer">Destination for the generated header</param>
        public static void WriteAsCHeader(this ShocoModel model, TextWriter Writer)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            if (Writer == null)
                throw new ArgumentNullException(nameof(Writer));

            var chars_count = model.CharactersById.Length;

            Writer.WriteLine(@"#ifndef _SHOCO_INTERNAL
#error This header file is only to be included by 'shoco.c'.
#endif
#pragma once
/*
This file was generated by 'generate_compressor_model.py'
so don't edit this by hand. Also, do not include this file
anywhere. It is internal to 'shoco.c'. Include 'shoco.h'
if you want to use shoco in your project.
*/
");
            Writer.WriteLine($"#define MIN_CHR {model.MinimumCharacter}");
            Writer.WriteLine($"#define MAX_CHR {model.MaximumCharacter}");
            Writer.WriteLine();
            Writer.WriteLine($"static const char chrs_by_chr_id[{chars_count}] = {{");
            Writer.Write("  ");
            Writer.WriteLine(string.Join(", ", model.CharactersById.Select(EscapeC)));
            Writer.WriteLine("};");
            Writer.WriteLine();
            Writer.WriteLine($"static const int8_t chr_ids_by_chr[256] = {{");
            Writer.Write("  ");
            Writer.WriteLine(string.Join(", ", model.IdsByCharacter.Select(FormatAsCByte)));
            Writer.WriteLine("};");
            Writer.WriteLine();
            var successors = model.SuccessorIdsByCharacterId;
            Writer.WriteLine($"static const int8_t successor_ids_by_chr_id_and_chr_id[{chars_count}][{chars_count}] = {{");
            for (int x = 0; x < chars_count; x++)
            {
                if (x != 0)
                    Writer.WriteLine(",");
                Writer.Write("  {");
                for (int y = 0; y < chars_count; y++)
                {
                    if (y != 0)
                        Writer.Write(", ");
                    Writer.Write(FormatAsCByte(successors[x, y]));
                }
                Writer.Write("}");
            }
            Writer.WriteLine();
            Writer.WriteLine("};");
            Writer.WriteLine();
            var characters = model.CharactersBySuccessorId;
            Writer.WriteLine($"static const int8_t chrs_by_chr_and_successor_id[MAX_CHR - MIN_CHR][{characters.GetLength(1)}] = {{");
            for (int x = 0; x < characters.GetLength(0); x++)
            {
                if (x != 0)
                    Writer.WriteLine(",");
                Writer.Write("  {");
                for (int y = 0; y < characters.GetLength(1); y++)
                {
                    if (y != 0)
                        Writer.Write(", ");
                    Writer.Write(EscapeC(characters[x, y]));
                }
                Writer.Write("}");
            }
            Writer.WriteLine();
            Writer.WriteLine("};");
            Writer.WriteLine();
            Writer.WriteLine();
            Writer.WriteLine($@"#ifdef _MSC_VER
#pragma warning(push)
#pragma warning(disable: 4324)    // structure was padded due to __declspec(align())
#endif

typedef struct Pack {{
  const uint32_t word;
  const unsigned int bytes_packed;
  const unsigned int bytes_unpacked;
  const unsigned int offsets[{model.Packs[0].offsets.Length}];
  const int16_t _ALIGNED masks[{model.Packs[0].masks.Length}];
  const char header_mask;
  const char header;
}} Pack;

#ifdef _MSC_VER
#pragma warning(pop)
#endif");
            Writer.WriteLine();
            var packs = model.Packs;
            Writer.WriteLine($@"#define PACK_COUNT {packs.Length}
#define MAX_SUCCESSOR_N {8 - 1}");
            Writer.WriteLine();
            Writer.WriteLine("static const Pack packs[PACK_COUNT] = {");
            for (int i = 0; i < packs.Length; i++)
            {
                var pack = packs[i];
                if (i != 0)
                    Writer.WriteLine(",");
                Writer.Write($"  {{ 0x{pack.Header << 24:x8}, {pack.BytesPacked}, {pack.BytesUnpacked}, {{ {string.Join(", ", pack.offsets)} }}, {{ {string.Join(", ", pack.masks)} }}, 0x{(pack.Header >> 1) | pack.Header:x2}, 0x{pack.Header:x2} }}");
            }
            Writer.WriteLine();
            Writer.WriteLine("};");
        }

        /// <summary>
        /// Writes a ShocoModel as a C Header
        /// </summary>
        /// <param name="model">ShocoModel to generate the header from</param>
        /// <param name="Filename">Destination for the generated header</param>
        public static void WriteAsCHeader(this ShocoModel model, string Filename)
        {
            using (var stream = new FileStream(Filename, FileMode.Create, FileAccess.Write))
            {
                using (var writer = new StreamWriter(stream, Encoding.ASCII))
                {
                    WriteAsCHeader(model, writer);
                }
            }
        }

        /// <summary>
        /// Reads a C Header into a ShocoModel
        /// </summary>
        /// <param name="Filename">C Header location</param>
        /// <returns>ShocoModel representation from the C Header</returns>
        public static ShocoModel ReadFromCHeader(string Filename)
        {
            using (var stream = File.OpenRead(Filename))
            {
                return ReadFromCHeader(stream);
            }
        }

        /// <summary>
        /// Reads a C Header into a ShocoModel
        /// </summary>
        /// <param name="Stream">C Header source</param>
        /// <returns>ShocoModel representation from the C Header</returns>
        public static ShocoModel ReadFromCHeader(Stream Stream)
        {
            string content;
            using (var reader = new StreamReader(Stream))
                content = reader.ReadToEnd();
            return ReadFromCHeaderContent(content);
        }

        /// <summary>
        /// Reads a C Header into a ShocoModel
        /// </summary>
        /// <param name="FileContent">C Header source</param>
        /// <returns>ShocoModel representation from the C Header</returns>
        public static ShocoModel ReadFromCHeaderContent(string FileContent)
        {
            Match match;
            Match subMatch;
            CaptureCollection captures;
            int dimention1;
            int dimention2;

            int minimumCharacter;
            int maximumCharacter;
            int maximumSuccessorLength;
            byte[] charactersById;
            byte[] idsByCharacter;
            byte[,] successorIdsByCharacterId = null;
            byte[,] charactersBySuccessorId = null;
            ShocoPack[] packs;

            match = Regex.Match(FileContent, @"^#define\s+MIN_CHR\s+(\d+)", RegexOptions.Multiline);
            if (!match.Success)
                throw new ArgumentException("Could not extract MIN_CHR value", nameof(FileContent));
            if (!int.TryParse(match.Groups[1].Value, out minimumCharacter))
                throw new ArgumentException("Invalid MIN_CHR value present", nameof(FileContent));

            match = Regex.Match(FileContent, @"^#define\s+MAX_CHR\s+(\d+)", RegexOptions.Multiline);
            if (!match.Success)
                throw new ArgumentException("Could not extract MAX_CHR value", nameof(FileContent));
            if (!int.TryParse(match.Groups[1].Value, out maximumCharacter))
                throw new ArgumentException("Invalid MAX_CHR value present", nameof(FileContent));

            match = Regex.Match(FileContent, @"^#define\s+MAX_SUCCESSOR_N\s+(\d+)", RegexOptions.Multiline);
            if (!match.Success)
                throw new ArgumentException("Could not extract MAX_SUCCESSOR_N value", nameof(FileContent));
            if (!int.TryParse(match.Groups[1].Value, out maximumSuccessorLength))
                throw new ArgumentException("Invalid MAX_CHR value present", nameof(FileContent));

            match = Regex.Match(FileContent, @"chrs_by_chr_id\[[^{}]+?{(?:\s*'(\\[abfnrtv\\'""?e]|\\\d\d\d|\\x[\dA-Fa-f]{2}|.)'\s*,?\s*)+}");
            if (!match.Success)
                throw new ArgumentException("Could not extract chrs_by_chr_id values", nameof(FileContent));
            charactersById = match.Groups[1].Captures.Cast<Capture>().Select(c => c.Value).Select(UnescapeC).ToArray();

            match = Regex.Match(FileContent, @"chr_ids_by_chr\[[^{}]+?{(?:\s*([\d-]+)\s*,?\s*)+}");
            if (!match.Success)
                throw new ArgumentException("Could not extract chr_ids_by_chr values", nameof(FileContent));
            idsByCharacter = match.Groups[1].Captures.Cast<Capture>().Select(c => c.Value).Select(ParseCByte).ToArray();

            match = Regex.Match(FileContent, @"successor_ids_by_chr_id_and_chr_id\[[^{}]+?{(?:\s*{((?:\s*[\d-]+\s*,?\s*)+)}\s*,?)*}");
            if (!match.Success)
                throw new ArgumentException("Could not extract successor_ids_by_chr_id_and_chr_id values", nameof(FileContent));
            dimention1 = match.Groups[1].Captures.Count;
            dimention2 = 0;
            for (int d1 = 0; d1 < dimention1; d1++)
            {
                subMatch = Regex.Match(match.Groups[1].Captures[d1].Value, @"(?:\s*([\d-]+)\s*,?\s*)+");
                if (!subMatch.Success)
                    throw new ArgumentException("Could not extract successor_ids_by_chr_id_and_chr_id values", nameof(FileContent));
                captures = subMatch.Groups[1].Captures;
                if (successorIdsByCharacterId == null)
                {
                    dimention2 = captures.Count;
                    successorIdsByCharacterId = new byte[dimention1, dimention2];
                }
                for (int d2 = 0; d2 < dimention2; d2++)
                {
                    successorIdsByCharacterId[d1, d2] = ParseCByte(captures[d2].Value);
                }
            }

            match = Regex.Match(FileContent, @"chrs_by_chr_and_successor_id\[[^{}]+?{(\s*{(?:\s*'(?:\\[abfnrtv\\'""?e]|\\\d\d\d|\\x[\dA-Fa-f]{2}|.)'\s*,?\s*)+}\s*,?)*}");
            if (!match.Success)
                throw new ArgumentException("Could not extract chrs_by_chr_and_successor_id values", nameof(FileContent));
            dimention1 = match.Groups[1].Captures.Count;
            if (dimention1 != maximumCharacter-minimumCharacter)
                throw new ArgumentException("Could not extract chrs_by_chr_and_successor_id values; invalid length", nameof(FileContent));
            dimention2 = 0;
            for (int d1 = 0; d1 < dimention1; d1++)
            {
                subMatch = Regex.Match(match.Groups[1].Captures[d1].Value, @"(?:\s*'(\\[abfnrtv\\'""?e]|\\\d\d\d|\\x[\dA-Fa-f]{2}|.)'\s*,?\s*)+");
                if (!subMatch.Success)
                    throw new ArgumentException("Could not extract chrs_by_chr_and_successor_id values", nameof(FileContent));
                captures = subMatch.Groups[1].Captures;
                if (charactersBySuccessorId == null)
                {
                    dimention2 = captures.Count;
                    charactersBySuccessorId = new byte[dimention1, dimention2];
                }
                for (int d2 = 0; d2 < dimention2; d2++)
                {
                    charactersBySuccessorId[d1, d2] = UnescapeC(captures[d2].Value);
                }
            }

            match = Regex.Match(FileContent, @"Pack packs\[[^{}]+?{(\s*{\s*0x[^\s,]*\s*,\s*\d+\s*,\s*\d+\s*,\s*{[^}]*}\s*,\s*{[^}]*}\s*,\s*0x[0-9A-Fa-f]{2}\s*,\s*0x[0-9A-Fa-f]{2}\s*}\s*,?)+\s*}");
            if (!match.Success)
                throw new ArgumentException("Could not extract packs", nameof(FileContent));
            packs = new ShocoPack[match.Groups[1].Captures.Count];
            for (int i = 0; i < packs.Length; i++)
            {
                var capture = match.Groups[1].Captures[i];
                subMatch = Regex.Match(capture.Value, @"\s*{\s*0x([^\s,]*)\s*,\s*(\d+)\s*,\s*(\d+)\s*,\s*{(?:\s*(\d+)\s*,?)*}\s*,\s*{(?:\s*(\d+)\s*,?)*}\s*,\s*0x[0-9A-Fa-f]{2}\s*,\s*0x[0-9A-Fa-f]{2}\s*}");
                if (!subMatch.Success)
                    throw new ArgumentException("Could not extract pack", nameof(FileContent));
                if (!uint.TryParse(subMatch.Groups[1].Value, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var code_word))
                    throw new ArgumentException("Could not extract pack code word", nameof(FileContent));
                if (!int.TryParse(subMatch.Groups[2].Value, out var bytes_packed))
                    throw new ArgumentException("Could not extract pack bytes_packed", nameof(FileContent));
                if (!int.TryParse(subMatch.Groups[3].Value, out var bytes_unpacked))
                    throw new ArgumentException("Could not extract pack bytes_unpacked", nameof(FileContent));
                var offsets = subMatch.Groups[4].Captures.Cast<Capture>().Select(c => c.Value).Select(int.Parse).ToArray();
                var masks = subMatch.Groups[5].Captures.Cast<Capture>().Select(c => c.Value).Select(int.Parse).ToArray();

                packs[i] = new ShocoPack(
                    Header: (byte)(code_word >> 24),
                    BytesPacked: bytes_packed,
                    BytesUnpacked: bytes_unpacked,
                    Offsets: offsets,
                    Masks: masks);
            }

            return new ShocoModel(minimumCharacter, maximumCharacter, maximumSuccessorLength,
                charactersById, idsByCharacter, successorIdsByCharacterId, charactersBySuccessorId,
                packs);
        }

        private static string EscapeCSharp(byte Character)
        {
            if (Character == (byte)'\'')
                return @"(byte)'\''";
            if (Character == (byte)'\\')
                return @"(byte)'\\'";
            if (Character == (byte)'\t')
                return @"(byte)'\t'";
            if (Character == (byte)'\n')
                return @"(byte)'\n'";
            if (Character == (byte)'\v')
                return @"(byte)'\v'";
            if (Character == (byte)'\f')
                return @"(byte)'\f'";
            if (Character == (byte)'\r')
                return @"(byte)'\r'";
            if (Character >= (byte)' ' && Character <= (byte)'~')
                return $" (byte)'{(char)Character}'";

            return $"      0x{Character:X2}";
        }

        private static string FormatAsCSharpByte(byte Character)
            => $"{new string(' ', Character < 10 ? 2 : Character < 100 ? 1 : 0)}{Character}";

        private static string EscapeC(byte Character)
        {
            if (Character == (byte)'\a') // 0x07
                return @"'\a'";
            if (Character == (byte)'\b') // 0x08
                return @"'\b'";
            if (Character == (byte)'\f') // 0x0C
                return @"'\f'";
            if (Character == (byte)'\n') // 0x0A
                return @"'\n'";
            if (Character == (byte)'\r') // 0x0D
                return @"'\r'";
            if (Character == (byte)'\t') // 0x09
                return @"'\t'";
            if (Character == (byte)'\v') // 0x0B
                return @"'\v'";
            if (Character == (byte)'\\') // 0x5C
                return @"'\\'";
            if (Character == (byte)'\'') // 0x27
                return @"'\''";
            if (Character == (byte)'\"') // 0x22
                return @"'\""";
            if (Character >= (byte)' ' && Character <= (byte)'~') // printable
                return $"'{(char)Character}'";

            return $@"'\x{Character:x2}'"; // hex escape
        }

        private static byte UnescapeC(string EscapedSequence)
        {
            if (EscapedSequence.Length == 1)
                return (byte)EscapedSequence[0];

            if (EscapedSequence[0] == '\\')
            {
                if (EscapedSequence.Length == 2)
                {
                    switch (EscapedSequence[1])
                    {
                        case 'a':
                            return (byte)'\a';
                        case 'b':
                            return (byte)'\b';
                        case 'f':
                            return (byte)'\f';
                        case 'n':
                            return (byte)'\n';
                        case 'r':
                            return (byte)'\r';
                        case 't':
                            return (byte)'\t';
                        case 'v':
                            return (byte)'\v';
                        case '\\':
                            return (byte)'\\';
                        case '\'':
                            return (byte)'\'';
                        case '\"':
                            return (byte)'\"';
                        case '?':
                            return (byte)'?';
                        case 'e':
                            return 0x1B;
                    }
                }
                else if (EscapedSequence.Length == 4 && (EscapedSequence[1] == 'X' || EscapedSequence[1] == 'x')) // format: \xhh
                {
                    if (byte.TryParse(EscapedSequence.Substring(2, 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var result))
                        return result;
                }
                else if (EscapedSequence.Length == 4 && (char.IsDigit(EscapedSequence[1]))) // format: \nnn
                {
                    if (byte.TryParse(EscapedSequence.Substring(1), out var result))
                    {
                        return result;
                    }
                }
            }
            throw new ArgumentException("Invalid escaped sequence", nameof(EscapedSequence));
        }

        private static byte ParseCByte(string Sequence)
        {
            if (Sequence == "-1")
                return 0xFF;
            return byte.Parse(Sequence);
        }

        private static string FormatAsCByte(byte Character)
            => Character == 0xFF ? "-1" : Character.ToString();

    }
}
