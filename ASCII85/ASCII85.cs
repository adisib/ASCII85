using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASCII85
{
    public static class ASCII85
    {
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
            LinkedList<byte> outList = new LinkedList<byte>();

            int s = sizeof(byte) * 8;
            for (int i = 0; i < inBytes.Length; i += 4)
            {
                int chunk = (int)(inBytes[i+0]) << (s*3)
                          | (int)(inBytes[i+1]) << (s*2)
                          | (int)(inBytes[i+2]) << (s*1)
                          | (int)(inBytes[i+3]) << (s*0);
                if (chunk == 0)
                {
                    outList.AddLast(122);
                    continue;
                }

                for (int j = 4; j >= 0; --j)
                {
                    outList.AddLast((byte)((chunk / Math.Pow(85, j) % 85) + 33));
                }
            }

            // Remove any bytes that were added
            byte[] outBytes = outList.ToArray();
            Array.Resize(ref outBytes, outBytes.Length - ((4 - (inSize % 4)) % 4));

            string encodedString = Encoding.ASCII.GetString(outBytes);
            if (includeDelimiters)
            {
                encodedString = "<~" + encodedString + "~>";
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
            encodedString = encodedString.Replace("<~", string.Empty).Replace("~>", string.Empty);
            encodedString = new string(encodedString.ToCharArray().Where(c => !char.IsWhiteSpace(c)).ToArray());

            int inSize = encodedString.Length;
            // Must be padded to get 5 byte chunks, using 'u' to preserve high order bits
            while (encodedString.Length < (inSize + ((5 - (inSize % 5)) % 5)) )
            {
                encodedString += 'u';
            }

            byte[] bytes = Encoding.ASCII.GetBytes(encodedString);
            LinkedList<byte> outList = new LinkedList<byte>();

            for (int i = 0; i < bytes.Length; i += 5)
            {
                int chunk = 0;
                if (bytes[i] != 122)
                {
                    long summedBytes = 0;
                    for (int j = 0; j < 5; ++j)
                    {
                        if (bytes[i + j] == 122)
                        {
                            throw new Exception("Decode Error: Zero group cannot be inside of another group.");
                        }

                        summedBytes += (bytes[i + j] - 33) * (int)Math.Pow(85, 4 - j);
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
                    outList.AddLast( (byte)(chunk >> (s*j)) );
                }
            }

            byte[] outBytes = outList.ToArray();
            // Remove any bytes that were added
            Array.Resize(ref outBytes, outBytes.Length - ((5 - (inSize % 5)) % 5));

            return outBytes;
        }
    }
}
