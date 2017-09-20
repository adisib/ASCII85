using System;

namespace Disibio.Encoding
{
    public static class ASCII85
    {
        // Some ASCII85 encoders will compress a group of spaces into a 'y' character.
        // The Adobe version (which is implemented here) does not support this, but I will allow enabling it anyway if you set this to true.
        private const bool compressSpaceGroups = false;

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
            // We might have to add bytes to ensure we always get 4 byte chunks
            int paddedByteCount = ((4 - (inBytes.Length % 4)) % 4);
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
                else if (compressSpaceGroups && byteGroup == 538976288)
                {
                    outBytes[writtenByteCount++] = 121;
                }
                else
                {
                    for (int j = 4; j >= 0; --j)
                    {
                        outBytes[writtenByteCount++] = (byte)( ((byteGroup / pow85[j]) % 85) + 33 );
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
            encodedString = encodedString.Replace("z", "!!!!!");
            if (compressSpaceGroups)
            {
                encodedString = encodedString.Replace("y", "+<VdL");
            }

            // Must be padded to get 5 byte chunks, using 'u' to preserve high order bits
            int paddedByteCount = ((5 - (encodedString.Length % 5)) % 5);
            for (int i = 0; i < paddedByteCount; ++i)
            {
                encodedString += 'u';
            }

            // We read more bytes than we write, so we can use the array for both input and output at the same time to save memory
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(encodedString);
            int writtenByteCount = 0;

            for (int i = 0; i < bytes.Length; i += 5)
            {
                long byteGroup = 0L;
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

                    byteGroup += (currentByte - 33) * pow85[4 - j];
                }

                if (byteGroup > 4294967295) // 2^32 - 1
                {
                    throw new Exception("Decode Error: Group is too large to decode.");
                }

                int s = sizeof(byte) * 8;
                for (int j = 3; j >= 0; --j)
                {
                    bytes[writtenByteCount++] = (byte)( ((uint)(byteGroup)) >> (s * j) );
                }
            }

            // Remove extra bytes
            Array.Resize(ref bytes, writtenByteCount - paddedByteCount);

            return bytes;
        }
    }
}
