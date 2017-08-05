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
                int chunk = (int)(inBytes[i+0]) << (s*3)
                          | (int)(inBytes[i+1]) << (s*2)
                          | (int)(inBytes[i+2]) << (s*1)
                          | (int)(inBytes[i+3]) << (s*0);
                if (chunk == 0)
                {
                    outBytes[writtenByteCount++] = 122;
                }
                else
                {
                    for (int j = 4; j >= 0; --j)
                    {
                        outBytes[writtenByteCount++] = (byte)((chunk / Math.Pow(85, j) % 85) + 33);
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
            encodedString = string.Join("", encodedString.Split((char[])null, StringSplitOptions.RemoveEmptyEntries));

            int inSize = encodedString.Length;
            // Must be padded to get 5 byte chunks, using 'u' to preserve high order bits
            while (encodedString.Length < (inSize + ((5 - (inSize % 5)) % 5)) )
            {
                encodedString += 'u';
            }
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(encodedString);

            // We buffer outBytes to theoretical maximum, and reduce its size as necessary later
            byte[] outBytes = new byte[(int)Math.Ceiling(encodedString.Length * 0.8)];
            int writtenByteCount = 0;

            for (int i = 0; i < bytes.Length; i += 5)
            {
                int chunk = 0;
                if (bytes[i] != 122)
                {
                    long summedBytes = 0;
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

                        summedBytes += (currentByte - 33) * (int)Math.Pow(85, 4 - j);
                        if (summedBytes > 4294967295)
                        {
                            throw new Exception("Decode Error: Group is too large to decode.");
                        }
                        chunk = (int)(summedBytes);
                    }
                }

                int s = sizeof(byte) * 8;
                for (int j = 3; j >= 0; --j)
                {
                    outBytes[writtenByteCount++] = (byte)(chunk >> (s*j));
                }
            }

            // Remove extra bytes
            Array.Resize(ref outBytes, outBytes.Length - ((5 - (inSize % 5)) % 5));

            return outBytes;
        }
    }
}
