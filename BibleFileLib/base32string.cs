using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;


namespace WordSend
{
    public class base32string
    {
        public char PaddingChar = '=';
        public bool UsePadding = false;
        public bool IsCaseSensitive = false;
        // public bool IgnoreWhiteSpaceWhenDecoding = false;
        private readonly string _alphabet = "0123456789abcdefghjkmnpqrtuvwxyz";
        private Dictionary<string, uint> _index;
        // alphabets may be used with varying case sensitivity, thus index must not ignore case
        private static Dictionary<string, Dictionary<string, uint>> _indexes = new Dictionary<string, Dictionary<string, uint>>(2, StringComparer.InvariantCulture);

        public base32string()
        {
            EnsureAlphabetIndexed();
        }

        /// <summary>
        /// Encode a string as Base32 using an alphabet of 10 digits and lower case letters except for i, l, o, and s.
        /// </summary>
        /// <param name="plaintext">UTF-8 string to encode</param>
        /// <returns>Base32 string encoding the input string</returns>
        public string Encode32(string plaintext)
        {
            return Encode(Encoding.UTF8.GetBytes(plaintext));
        }

        public string Encode(byte[] data)
        {
            StringBuilder result = new StringBuilder(Math.Max((int)Math.Ceiling(data.Length * 8 / 5.0), 1));
            byte[] emptyBuff = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };
            byte[] buff = new byte[8];
            // take input five bytes at a time to chunk it up for encoding
            for (int i = 0; i < data.Length; i += 5)
            {
                int bytes = Math.Min(data.Length - i, 5);
                // parse five bytes at a time using an 8 byte ulong
                Array.Copy(emptyBuff, buff, emptyBuff.Length);
                Array.Copy(data, i, buff, buff.Length - (bytes + 1), bytes);
                Array.Reverse(buff);
                ulong val = BitConverter.ToUInt64(buff, 0);
                for (int bitOffset = ((bytes + 1) * 8) - 5; bitOffset > 3; bitOffset -= 5)
                {
                    result.Append(_alphabet[(int)((val >> bitOffset) & 0x1f)]);
                }
            }
            /*
            if (UsePadding)
            {
                result.Append(string.Empty.PadRight((result.Length % 8) == 0 ? 0 : (8 - (result.Length % 8)), PaddingChar));
            }
             */
            return result.ToString();
        }

        /// <summary>
        /// Decode Base32 string to a UTF-8 string.
        /// </summary>
        /// <param name="encodedtext">Base32 encoded text</param>
        /// <returns>Plain UTF-8 string.</returns>
        public string Decode32(string encodedtext)
        {
            return Encoding.UTF8.GetString(Decode(encodedtext));
        }

        public byte[] Decode(string input)
        {
            input = Regex.Replace(input, "\\s+", "");
            input = input.TrimEnd(PaddingChar);
            /*
            if (IgnoreWhiteSpaceWhenDecoding)
            {
                input = Regex.Replace(input, "\\s+", "");
            }
            if (UsePadding)
            {
                if (input.Length % 8 != 0)
                {
                    throw new ArgumentException("Invalid length for a base32 string with padding.");
                }
                input = input.TrimEnd(PaddingChar);
            }
            */
            // index the alphabet for decoding only when needed
            // EnsureAlphabetIndexed();
            MemoryStream ms = new MemoryStream(Math.Max((int)Math.Ceiling(input.Length * 5 / 8.0), 1));
            // take input eight bytes at a time to chunk it up for encoding
            for (int i = 0; i < input.Length; i += 8)
            {
                int chars = Math.Min(input.Length - i, 8);
                ulong val = 0;
                int bytes = (int)Math.Floor(chars * (5 / 8.0));
                for (int charOffset = 0; charOffset < chars; charOffset++)
                {
                    uint cbyte;
                    if (!_index.TryGetValue(input.Substring(i + charOffset, 1), out cbyte))
                    {
                        throw new ArgumentException("Invalid character '" + input.Substring(i + charOffset, 1) + "' in base32 string, valid characters are: " + _alphabet);
                    }
                    val |= (((ulong)cbyte) << ((((bytes + 1) * 8) - (charOffset * 5)) - 5));
                }
                byte[] buff = BitConverter.GetBytes(val);
                Array.Reverse(buff);
                ms.Write(buff, buff.Length - (bytes + 1), bytes);
            }
            return ms.ToArray();
        }

        private void EnsureAlphabetIndexed()
        {
            if (_index == null)
            {
                Dictionary<string, uint> cidx;
                string indexKey = (IsCaseSensitive ? "S" : "I") + _alphabet;
                if (!_indexes.TryGetValue(indexKey, out cidx))
                {
                    lock (_indexes)
                    {
                        if (!_indexes.TryGetValue(indexKey, out cidx))
                        {
                            cidx = new Dictionary<string, uint>(_alphabet.Length, IsCaseSensitive ? StringComparer.InvariantCulture : StringComparer.InvariantCultureIgnoreCase);
                            for (int i = 0; i < _alphabet.Length; i++)
                            {
                                cidx[_alphabet.Substring(i, 1)] = (uint)i;
                            }
                            _indexes.Add(indexKey, cidx);
                        }
                    }
                }
                _index = cidx;
            }
        }
    }
}
