using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace KDDQPacketGenerator.Utils
{
	public class ConvertUtil
	{
		/// <summary>
		/// 高低对调
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static string ReverseHexString(string str)
		{
			char[] buff = new char[str.Length];
			for (int i = 0; i < str.Length; i += 2)
			{
				buff[i] = str[str.Length - i - 2];
				buff[i + 1] = str[str.Length - 1 - i];
			}
			string s = new string(buff);
			return s;
		}

		/// <summary>
		/// 16进制转10进制
		/// </summary>
		/// <param name="HEX"></param>
		/// <returns></returns>
		public static int HEXtoDEC(string HEX)
		{
			return Convert.ToInt32(HEX, 16);
		}

		/// <summary>
		/// 转化bytes成16进制的字符
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns></returns>
		public static string BytesToHexStr(byte[] bytes)
		{
			string returnStr = "";
			if (bytes != null)
			{
				for (int i = 0; i < bytes.Length; i++)
				{
					returnStr += bytes[i].ToString("X2");
				}
			}
			return returnStr;
		}
		/// <summary>
		/// 转化bytes成16进制的字符并格式
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns></returns>
		public static string BytesToHexStrFormat(byte[] bytes)
		{
			string returnStr = "";
			if (bytes != null)
			{
				for (int i = 0; i < bytes.Length; i++)
				{
					returnStr += bytes[i].ToString("X2")+ " ";
				}
			}
			return returnStr;
		}

		/// <summary>
		/// 返回处理后的十六进制字符串
		/// </summary>
		/// <param name="mStr"></param>
		/// <returns></returns>
		public static string StrToHex(string mStr) 
		{
			return BitConverter.ToString(
			ASCIIEncoding.Default.GetBytes(mStr)).Replace("-", " ");
		}
		/// <summary>
		/// 十六进制字符串转Byte数组
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static byte[] HexStringToByteArray(string s)
		{
			s = s.Replace(" ", "");
			byte[] buffer = new byte[s.Length / 2];
			for (int i = 0; i < s.Length; i += 2)
				buffer[i / 2] = (byte)Convert.ToByte(s.Substring(i, 2), 16);
			return buffer;
		}
		/// <summary>
		/// 校验位计算  
		/// </summary>
		/// <param name="ch"></param>
		/// <param name="len"></param>
		/// <returns></returns>
		public static UInt16 Crc16_Modbus(byte[] ch, UInt16 len)
		{
			UInt16 tmp = 0xffff;

			for (int n = 0; n < len; n++)      /*此处的len -- 要校验的位数为len个*/
			{
				tmp = (UInt16)(ch[n] ^ tmp);

				for (int i = 0; i < 8; i++)
				{  /*此处的8 -- 指每一个char类型有8bit，每bit都要处理*/
					if ((tmp & 0x01) > 0)
					{
						tmp = (UInt16)(tmp >> 1);
						tmp = (UInt16)(tmp ^ 0xa001);
					}
					else
					{
						tmp = (UInt16)(tmp >> 1);
					}
				}
			}
			/*返回CRC校验后的值*/
			return tmp;
		}

		public static string EncryptPassword(string password)
		{
			// 创建一个 SHA256 对象  
			SHA256 sha256 = SHA256.Create();

			// 计算密码的 SHA256 哈希  
			byte[] hashValue = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

			// 将字节转换为十六进制字符串  
			StringBuilder result = new StringBuilder();
			for (int i = 0; i < hashValue.Length; i++)
			{
				result.Append(hashValue[i].ToString("x2"));
			}

			// 返回哈希字符串  
			return result.ToString();
		}
        /// <summary>
        /// 十六进制字符串到双精度浮点数
        /// </summary>
        /// <param name="hexData">十六进制字符串</param>
        /// <param name="startIndex">起始索引</param>
        /// <returns>双精度浮点数</returns>
        public static double ConvertHexToDouble(string hexData, int startIndex,int length)
        {
            var byteArray = HexStringToByteArray(hexData.Substring(startIndex, length));
           // return BitConverter.ToDouble(byteArray.Reverse().ToArray(), 0);
            return BitConverter.ToDouble(byteArray.ToArray(), 0);
        }

        /// <summary>
        /// 十六进制字符串转换为ASCII字符
        /// </summary>
        /// <param name="hexString"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static string HexStringToAscii(string hexString)
        {
            if (hexString.Length % 2 != 0)
            {
                throw new ArgumentException("Input hexadecimal string must have an even number of characters.");
            }

            char[] asciiChars = new char[hexString.Length / 2];

            for (int i = 0; i < hexString.Length; i += 2)
            {
                string hexPair = hexString.Substring(i, 2);
                int decimalValue = Convert.ToInt32(hexPair, 16);
                asciiChars[i / 2] = (char)decimalValue;
            }

            return new string(asciiChars);
        }

        /// <summary>
		/// 用于读取并转换两个字符为整数
		/// </summary>
		/// <param name="data"></param>
		/// <param name="offset"></param>
		/// <param name="step"></param>
		/// <returns></returns>
        public static int ReadAndConvertHex(string data, ref int offset, int step)
        {
            string s1 = data.Substring(offset, 2);
            offset += 2;
            string s2 = data.Substring(offset, 2);
            offset += 2;
            return HEXtoDEC(s2 + s1);
        }

		/// <summary>
		/// 时间解析函数
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static DateTime ParseTimeData(byte[] data)
		{
			try
			{
				int year = data[0] + 2000; // 年从2000年开始
				int month = data[1];
				int day = data[2];
				int hour = data[3];
				int minute = data[4];
				int second = data[5];
				return new DateTime(year, month, day, hour, minute, second);
			}
			catch (Exception)
			{
				return new DateTime(1970,1,1,0,0,0);
			}
		}

		// 比较两个版本号
		public static int CompareVersions(string version1, string version2)
		{
			// 按点分割版本号
			var parts1 = version1.Split('.');
			var parts2 = version2.Split('.');

			// 比较每一部分
			for (int i = 0; i < Math.Min(parts1.Length, parts2.Length); i++)
			{
				int part1 = int.Parse(parts1[i]);
				int part2 = int.Parse(parts2[i]);

				// 如果当前部分不同，返回比较结果
				if (part1 < part2)
					return -1;
				if (part1 > part2)
					return 1;
			}

			// 如果前面的部分相等，比较长度（如果版本号包含更多部分）
			return parts1.Length.CompareTo(parts2.Length);
		}
	}
}
