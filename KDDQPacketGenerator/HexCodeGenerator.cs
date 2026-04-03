using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KDDQPacketGenerator
{
    public static class HexCodeGenerator
    {
        private static readonly Random _random = new Random();
        private static readonly HashSet<string> _generatedCodes = new HashSet<string>();

        // 生成一个唯一的8位十六进制字符串
        public static string GenerateUniqueHexCode()
        {
            string code;

            // 防止重复，一直生成直到不重复
            do
            {
                uint value = (uint)_random.Next(0, int.MaxValue); // 生成31位
                value |= (uint)_random.Next(0, 2) << 31; // 手动补1位（第32位）
                code = value.ToString("X8");
            } while (!_generatedCodes.Add(code)); // 如果添加失败说明重复了

            return code;
        }

        // 批量生成多个唯一的8位十六进制字符串
        public static List<string> GenerateUniqueHexCodes(int count)
        {
            if (count < 1) throw new ArgumentException("数量必须大于0");

            var result = new List<string>();
            for (int i = 0; i < count; i++)
            {
                result.Add(GenerateUniqueHexCode());
            }
            return result;
        }

        // 可选：重置已生成的记录
        public static void Reset()
        {
            _generatedCodes.Clear();
        }
        /// <summary>
        /// 将十六进制字符串转换为字节数组
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static byte[] HexStringToByteArray(string hex)
        {
            if (hex.Length % 2 != 0)
                throw new ArgumentException("十六进制字符串长度必须是偶数");

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }
    }
}
