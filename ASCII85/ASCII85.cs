using System;

namespace Disibio.Encoding
{
    public static class ASCII85
    {
        private const string beginDelimiter = "<~";
        private const string endDelimiter = "~>";

        /// <summary>
        /// Converts a byte array into an Adobe version ASCII85 encoded representation.
        /// </summary>
        /// <param name="inBytes"> The byte array to be converted. </param>
        /// <param name="includeDelimiters"> Whether optional delimiters should be included to denote that the string is ASCII85 encoded. </param>
        /// <returns> An ASCII85 encoded string. </returns>
        public static string Encode(byte[] inBytes, bool includeDelimiters = true)
        {
            int inSize = inBytes.Length;
            // We might have to add bytes to ensure we always get 4 byte chunks
            Array.Resize(ref inBytes, inBytes.Length + ((4 - (inSize % 4)) % 4));

            // We buffer outBytes to theoretical maximum, and reduce its size as necessary later
            byte[] outBytes = new byte[(int)Math.Ceiling(inBytes.Length * 1.25)];
            int writtenByteCount = 0;

            int s = sizeof(byte) * 8;
            for (int i = 0; i < inBytes.Length; i += 4)
            {
                uint byteGroup = (uint)(inBytes[i+0]) << (s*3)
                               | (uint)(inBytes[i+1]) << (s*2)
                               | (uint)(inBytes[i+2]) << (s*1)
                               | (uint)(inBytes[i+3]) << (s*0);
                if (byteGroup == 0)
                {
                    outBytes[writtenByteCount++] = 122;
                }
                else
                {
                    int x = (int)Math.Pow(85, 4);
                    for (int j = 0; j < 5; ++j)
                    {
                        outBytes[writtenByteCount++] = (byte)( ((byteGroup / x) % 85) + 33);
                        x /= 85;
                    }
                }
            }

            // Remove extra bytes
            Array.Resize(ref outBytes, writtenByteCount - ((4 - (inSize % 4)) % 4));

            string encodedString = System.Text.Encoding.ASCII.GetString(outBytes);
            if (includeDelimiters)
            {
                encodedString = beginDelimiter + encodedString + endDelimiter;
            }
            return encodedString;
        }

        /// <summary>
        /// Converts an Adobe version ASCII85 encoded string into the array of bytes it derives from.
        /// </summary>
        /// <param name="encodedString"> The ASCII85 encoded string to be decoded. </param>
        /// <returns> The byte array the encoded string derives from. </returns>
        public static byte[] Decode(string encodedString)
        {
            if (encodedString.StartsWith(beginDelimiter) && encodedString.EndsWith(endDelimiter))
            {
                encodedString = encodedString.Substring(beginDelimiter.Length, encodedString.Length - (beginDelimiter.Length + endDelimiter.Length));
            }
            encodedString = string.Join(string.Empty, encodedString.Split((char[])null, StringSplitOptions.RemoveEmptyEntries));

            int inSize = encodedString.Length;
            // Must be padded to get 5 byte chunks, using 'u' to preserve high order bits
            while (encodedString.Length < (inSize + ((5 - (inSize % 5)) % 5)) )
            {
                encodedString += 'u';
            }

            // We read more bytes than we write, so we can use the array for both input and output at the same time to save memory
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(encodedString);
            int writtenByteCount = 0;

            for (int i = 0; i < bytes.Length; i += 5)
            {
                uint byteGroup = 0;
                if (bytes[i] != 122)
                {
                    int x = (int)Math.Pow(85, 4);
                    long summedBytes = 0l;
                    for (int j = 0; j < 5; ++j)
                    {
                        byte currentByte = bytes[i + j];
                        if (currentByte == 122)
                        {
                            throw new Exception("Decode Error: Zero group cannot be inside of another group.");
                        }
                        if (currentByte < 33 || currentByte > 117)
                        {
                            throw new Exception("Decode Error: Found character outside of ASCII85 character set.");
                        }

                        summedBytes += (currentByte - 33) * x;
                        x /= 85;
                    }
                    
                    if (summedBytes > 4294967295) // 2^32 - 1
                    {
                        throw new Exception("Decode Error: Group is too large to decode.");
                    }
                    byteGroup = (uint)(summedBytes);
                }

                int s = sizeof(byte) * 8;
                for (int j = 3; j >= 0; --j)
                {
                    bytes[writtenByteCount++] = (byte)(byteGroup >> (s*j));
                }
            }

            // Remove extra bytes
            Array.Resize(ref bytes, writtenByteCount - ((5 - (inSize % 5)) % 5));

            return bytes;
        }
    }
}
