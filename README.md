# ASCII85
A C# implementation of the Adobe version ASCII85 coder. Based on the description presented at https://en.wikipedia.org/wiki/Ascii85

# Usage
Example Usage
```C#
string testString = "This is a test string.";

byte[] bytesToEncode = Encoding.ASCII.GetBytes(testString);
string encodedString = ASCII85.Encode(bytesToEncode);
Console.WriteLine(encodedString);


byte[] decodedBytes = ASCII85.Decode(encodedString);
string originalString = Encoding.ASCII.GetString(decodedBytes);
Console.WriteLine(originalString);
```
