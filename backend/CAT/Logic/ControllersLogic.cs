using System.Security.Cryptography;
using System.Text;

namespace CAT.Logic
{
    public static class ControllersLogic
    {
        public static bool IsMobileDevice(string? userAgent)
        {
            return userAgent != null
                && (userAgent.Contains("iPhone")
                || userAgent.Contains("Android")
                || userAgent.Contains("Windows Phone"));
        }

        public static (int skip, int take) ComputePagination(bool isMobile, int page)
        {
            var take = isMobile ? 5 : 10;
            var skip = (page - 1) * take;
            return (skip, take);
        }

        public static string CalculateSHA256(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                var inputBytes = Encoding.UTF8.GetBytes(input);
                var hashBytes = sha256.ComputeHash(inputBytes);

                var sb = new StringBuilder();
                for (var i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }

                return sb.ToString();
            }
        }
    }

}