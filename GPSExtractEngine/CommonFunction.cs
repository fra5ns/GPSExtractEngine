using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Net.Http;
using Newtonsoft.Json;

// http://stackoverflow.com/questions/4494664/binary-coded-decimal-bcd-to-hexadecimal-conversion
namespace GPSExtractEngine
{
    class CommonFunction
    {
        private const string hexDigits = "0123456789ABCDEF";

        public string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        public byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        public string DecToHex(Int32 sValue)
        {
            string functionReturnValue = null;
            functionReturnValue = Convert.ToString(sValue, 16).ToUpper();
            //atau bisa juga DectoHex = Hex (sValue)
            if ((functionReturnValue.Length % 2) != 0)
                functionReturnValue = "0" + functionReturnValue;
            return functionReturnValue;
            //ini convert digit per  digit , bukan convert satu bilangan utuh menjadi hex( kl ini pakai convert.to16 aja)
        }

        public string StringDecToHex(string sValue)
        {
            string sResult = "";
            if (sValue.Length % 2 == 0)
            {
                for (Int16 i = 0; i <= sValue.Length - 1; i += 2)
                {
                    if (sValue.Substring(i, 2) != "00") sResult = sResult + DecToHex(Convert.ToInt16(sValue.Substring(i, 2)));
                    else sResult += "00";
                }
            }
            return sResult;
        }

        public byte[] HexStringToBytes(string str)
        {
            // Determine the number of bytes
            byte[] bytes = new byte[(str.Length >> 1)];
            for (int i = 0; i <= str.Length - 1; i += 2)
            {
                int highDigit = hexDigits.IndexOf(char.ToUpperInvariant(str[i]));
                int lowDigit = hexDigits.IndexOf(char.ToUpperInvariant(str[i + 1]));
                if (highDigit == -1 || lowDigit == -1)
                {
                    throw new ArgumentException("The string contains an invalid digit.", "s");
                }
                bytes[i >> 1] = Convert.ToByte((highDigit << 4) | lowDigit);
            }
            return bytes;
        }

        public string StringToHex(string sValue)
        {

            string sResult = "";
            byte[] tmp = ASCIIEncoding.ASCII.GetBytes(sValue);

            for (Int16 i = 0; i < tmp.Length; i++)
            {
                sResult = sResult + DecToHex(Convert.ToInt32(tmp.GetValue(i)));
            }
            return sResult;
        }

        public string HexToString(string value)
        {
            string sResult = "";
            try
            {
                for (Int16 i = 0; i <= value.Length - 1; i += 2)
                {
                    if (value.Substring(i, 2) != "00") sResult = sResult + (char)Convert.ToInt32(HexToDec( value.Substring(i, 2)));
                }
            }
            catch { sResult = "Error"; }
            return sResult;
        }

   

        public string HexNumericStringToASCII(string sValue)
        {
            string functionReturnValue = null;
            try
            {
                string sResult = "";

                for (Int16 i = 0; i <= sValue.Length - 1; i += 2)
                {
                    if (sValue.Substring(i, 2) != "00") sResult = sResult + (char)Convert.ToInt32(sValue.Substring(i, 2), 16);
                }
                functionReturnValue = sResult;
            }
            catch( Exception)
            {
                functionReturnValue = "Error";
            }
            return functionReturnValue;


        }

        public string HexToBin(string sValue)
        {
            string res = "";
            try
            {
                res = Convert.ToString(Convert.ToInt32(sValue, 16), 2);
                int i = sValue.Length;
                res = res.PadLeft(i * 4, '0');
            }
            catch { }
            return res;


        }


        public string HexToDec(string sValue)
        {
            int iRes = 0;
            int j = sValue.Length - 1;

            for (int i = 0; i < sValue.Length; i++)
            {
                iRes += Convert.ToInt32(sValue.Substring(i, 1), 16) * Convert.ToInt32(Math.Pow(16, j));
                j -= 1;

            }
            return iRes.ToString();

        }
          
        public string XOR_CheckSum(string strdata)
        {
            string res = "";
            try
            {
                int i = strdata.Length / 2;
                int tmp = Convert.ToInt16(HexToDec(strdata.Substring(0, 2)));
                for (int j = 1; j <= i - 1; j++)
                {
                    tmp = tmp ^ Convert.ToInt16(HexToDec(strdata.Substring(j * 2, 2)));
                }
                res = DecToHex(tmp);
            }
            catch { };
            return res;
        }

        public string FormatStringLeftPad(string str, int len, char chr)
        {
            string res = "";
            int j = len - str.Length;
            res = res.PadLeft(j, chr);
            return res + str; 
        }
        public string Reverse_DeviceID(string sIMEI)
        {
            string res = "";
            Int32 str80H = Convert.ToInt32("80", 16);
            try
            {
                res =   FormatStringLeftPad( HexToDec(sIMEI.Substring(0, 2)),2,'0');
                res = res + (Convert.ToInt16(sIMEI.Substring(2, 2), 16) - str80H).ToString("00") + (Convert.ToInt16(sIMEI.Substring(4, 2), 16) - str80H).ToString("00") +FormatStringLeftPad( HexToDec( sIMEI.Substring(6, 2)),2,'0');
                
            }
            catch { };
            return res;

        }

        public string Convert_DeviceID(string sIMEI)
        {
            string res = "";
            Int32 str80H = Convert.ToInt32("80",16);
            try
            {
                string tmp = StringDecToHex(sIMEI);
                res = tmp.Substring(0, 2) + DecToHex(Convert.ToInt16(tmp.Substring(2, 2), 16) + str80H) + DecToHex(Convert.ToInt16(tmp.Substring(4, 2), 16) + str80H) + tmp.Substring(6,2);
            }
            catch (Exception ex){ Console.WriteLine(ex.Message); }
            return res;
        }
        public  string ConvertToUnixTime(DateTime datetime)
        {
            return ((DateTimeOffset)datetime).ToUnixTimeSeconds().ToString();
            // ini return unixtime GMT+0
        }
    }
          

    public static class StringExtensions
    {
        public static bool ContainsAny(this string input, IEnumerable<string> containsKeywords, StringComparison comparisonType)
        {
            return containsKeywords.Any(keyword => input.IndexOf(keyword, comparisonType) >= 0);
        }
    }
    class DistanceAlgorithm
    {

        const double PIx = 3.141592653589793;
        const double RADIUS = 6378.16;

        /// <summary>
        /// This class cannot be instantiated.
        /// </summary>
        private DistanceAlgorithm() { }

        /// <summary>
        /// Convert degrees to Radians
        /// </summary>
        /// <param name="x">Degrees</param>
        /// <returns>The equivalent in radians</returns>
        public static double Radians(double x)
        {
            return x * PIx / 180;
        }

        /// <summary>
        /// Calculate the distance between two places.
        /// </summary>
        /// <param name="lon1"></param>
        /// <param name="lat1"></param>
        /// <param name="lon2"></param>
        /// <param name="lat2"></param>
        /// <returns></returns>
        public static double DistanceBetweenPlaces(
            double lon1,
            double lat1,
            double lon2,
            double lat2)
        {
            double dlon = Radians(lon2 - lon1);
            double dlat = Radians(lat2 - lat1);

            double a = (Math.Sin(dlat / 2) * Math.Sin(dlat / 2)) + Math.Cos(Radians(lat1)) * Math.Cos(Radians(lat2)) * (Math.Sin(dlon / 2) * Math.Sin(dlon / 2));
            double angle = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return angle * RADIUS; 
        }

        //jika longtitude di khatulistiwa , 1 derajat = 111.32 km
        public static double MetersToDecimalDegrees(double meters, double latitude) // rumus utk latitude saja
        {
            return meters / (111.32 * 1000 * Math.Cos(latitude * (Math.PI / 180)));
            // kl lat nya minuus maka dikurang dgn hasil conversi ini ( sebab makin ke bawah).
        }

        public static bool isInRectangle(double lat, double lon, double lat1, double lon1, double lat2, double lon2)
        {  //lat1,lon1 = koordinat sebelah kiri atas , lat2,lon2 koordinat sebelah kanan bawah , latitude = bujur/horizontal/y, lon= lintang/vertikal/x
            if (lat <= lat1 && lat >= lat2 && lon >= lon1 && lon <= lon2) return true;
            else return false;
        }

        public static bool isInRectangle(double centerX, double centerY, double radius,   double x, double y)
        { // utk hitung lingkaran, pertama cek dulu apakah ada di dalam radius rectangle , http://stackoverflow.com/questions/481144/equation-for-testing-if-a-point-is-inside-a-circle
            return x >= centerX - radius && x <= centerX + radius &&
                y >= centerY - radius && y <= centerY + radius;
        }

        //test if coordinate (x, y) is within a radius from coordinate (center_x, center_y)
        public static bool isPointInCircle(double centerX, double centerY,   double radius, double x, double y)
        {
            if (isInRectangle(centerX, centerY, radius, x, y))
            {
                double dx = centerX - x;
                double dy = centerY - y;
                dx *= dx;
                dy *= dy;
                double distanceSquared = dx + dy;
                double radiusSquared = radius * radius;
                return distanceSquared <= radiusSquared;
            }
            return false;
        }

         
       
    }


}
