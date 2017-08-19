using System;

namespace Disibio.Encoding
{
    public static class ASCII85
    {
        private const string beginDelimiter = "<~";
        private const string endDelimiter = "~>";

        private static readonly int[] pow85 = {1, 85, 85*85, 85*85*85, 85*85*85*85};

        /// <summary>
        /// Converts a byte array into an Adobe version ASCII85 encoded representation.
        /// </summary>
        /// <param name="inBytes"> The byte array to be converted. </param>
        /// <param name="includeDelimiters"> Whether optional delimiters should be included to denote that the string is ASCII85 encoded. </param>
        /// <returns> An ASCII85 encoded string. </returns>
        public static string Encode(byte[] inBytes, bool includeDelimiters = true)
        {
            int paddedByteCount = ((4 - (inBytes.Length % 4)) % 4);
            // We might have to add bytes to ensure we always get 4 byte chunks
            Array.Resize(ref inBytes, inBytes.Length + paddedByteCount);

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
                    for (int j = 4; j >= 0; --j)
                    {
                        outBytes[writtenByteCount++] = (byte)( ((byteGroup / pow85[j]) % 85) + 33);
                    }
                }
            }

            // Remove extra bytes
            Array.Resize(ref outBytes, writtenByteCount - paddedByteCount);

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

            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(encodedString);

            byte[] outBytes = new byte[(int)Math.Ceiling(bytes.Length * 0.8) + 5];
            int writtenByteCount = 0, paddedByteCount = 0;

            long byteGroup = 0L;
            for (int i = 0, groupPos = 0; (i < bytes.Length) || (groupPos != 0); ++i, groupPos = (groupPos + 1) % 5)
            {
                byte currentByte;
                if (i < bytes.Length)
                {
                    currentByte = bytes[i];
                }
                else // Must be padded to get 5 byte chunks, using max value to preserve high order bits
                {
                    currentByte = 117;
                    ++paddedByteCount;
                }

                if (currentByte == 122 && groupPos == 0)
                {
                    Array.Resize(ref outBytes, outBytes.Length + 4);
                    groupPos = 4;
                }
                else
                {
                    if (currentByte == 122)
                    {
                        throw new Exception("Decode Error: Zero group cannot be inside of another group.");
                    }
                    if (currentByte < 33 || currentByte > 117)
                    {
                        throw new Exception("Decode Error: Found character outside of ASCII85 character set.");
                    }

                    byteGroup += (currentByte - 33) * pow85[4 - groupPos];
                }

                if (groupPos >= 4)
                {
                    if (byteGroup > 4294967295) // 2^32 - 1
                    {
                        throw new Exception("Decode Error: Group is too large to decode.");
                    }

                    int s = sizeof(byte) * 8;
                    for (int j = 3; j >= 0; --j)
                    {
                        outBytes[writtenByteCount++] = (byte)( ((uint)(byteGroup)) >> (s*j));
                    }

                    byteGroup = 0L;
                }
            }

            // Remove extra bytes
            Array.Resize(ref outBytes, writtenByteCount - paddedByteCount);

            return outBytes;
        }
    }
}
