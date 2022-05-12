using System;

namespace SimpleShare
{
    public class StringGenerator
    {
        static Random rnd = new Random();

        public static string GenerateString(int nbrOfBytes)
        {
            var byteArray = GetByteArray(nbrOfBytes);
            var result = System.Text.Encoding.Default.GetString(byteArray);

            //var xx = System.Text.Encoding.Default.GetBytes(result);

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
