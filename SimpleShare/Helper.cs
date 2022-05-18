using System;

namespace SimpleShare
{
    public class Helper
    {
        static Random rnd = new Random();

        public static string GenerateString(int nbrOfBytes, System.Text.Encoding encoding)
        {
            var byteArray = GetByteArray(nbrOfBytes);
            var result = encoding.GetString(byteArray);

            // var xx = encoding.GetBytes(result);

            return result;
        }

        public static byte[] GetByteArray(int bytes)
        {
            byte[] b = new byte[bytes];
            rnd.NextBytes(b);
            return b;
        }
    }
}
