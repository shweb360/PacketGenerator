using System.Security.Cryptography;
using System.IO;
using System;
using System.Text;

namespace KDDQPacketGenerator.Utils
{
    public class AESUtil
    {

        private const string _key = "NjU4G/TxzlfoyISA";
        private const string _Iv = "9891QDDK_duyumao";

        /// <summary>
        ///AES 算法加密(CBC模式) 将明文加密，加密后进行Hex编码，返回密文字符串
        /// </summary>
        /// <param name="str">明文</param>
        /// <param name="key">密钥</param>
        /// <param name="lv">向量</param>
        /// <returns>加密后Hex编码的密文</returns>
        public static string AesEncryptor(string str, string key, string lv)
        {
            if (string.IsNullOrEmpty(str)) return null;

            Byte[] toEncryptArray = StrToHexByte(str);

            RijndaelManaged rm = new RijndaelManaged
            {
                Key = StrToHexByte(key),
                Mode = CipherMode.CBC,
                Padding = PaddingMode.Zeros,
                BlockSize = 128,
                IV = StrToHexByte(lv)
            };

            ICryptoTransform cTransform = rm.CreateEncryptor();
            Byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            return ToHexString(resultArray);
        }
        /// <summary>
        ///AES 算法解密(CBC模式) 将密文Hex解码后进行解密，返回明文字符串
        /// </summary>
        /// <param name="str">密文</param>
        /// <param name="lv">向量</param>
        /// <returns>明文</returns>
        public static string AesDecryptor(string str, string key, string Iv)
        {
            if (string.IsNullOrEmpty(str)) return null;
            Byte[] toDecryptorArray = StrToHexByte(str);

            RijndaelManaged rm = new RijndaelManaged
            {
                Key = StrToHexByte(key),
                Mode = CipherMode.CBC,
                Padding = PaddingMode.Zeros,
                BlockSize = 128,
                IV = StrToHexByte(Iv)
            };

            ICryptoTransform cTransform = rm.CreateDecryptor();
            Byte[] resultArray = cTransform.TransformFinalBlock(toDecryptorArray, 0, toDecryptorArray.Length);
            return ToHexString(resultArray);
        }
        /// <summary>
        /// AES 算法加密(CBC模式) 将明文字节数组加密，返回密文字节数组
        /// </summary>
        /// <param name="toEncryptArray">明文字节数组</param>
        /// <param name="key"></param>
        /// <param name="lv"></param>
        /// <returns></returns>
        public static Byte[] AesEncryptor(byte[] toEncryptArray, string key, string lv)
        {
            if (toEncryptArray == null) return null;

            RijndaelManaged rm = new RijndaelManaged
            {
                Key = StrToHexByte(key),
                Mode = CipherMode.CBC,
                Padding = PaddingMode.Zeros,
                BlockSize = 128,
                IV = StrToHexByte(lv)
            };

            ICryptoTransform cTransform = rm.CreateEncryptor();
            Byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            return resultArray;
        }

        /// <summary>
        ///AES 算法解密(CBC模式) 将密文进行解密，返回明文字节数组
        /// </summary>
        /// <param name="toDecryptorArray">密文字节数组</param>
        /// <param name="key">密钥</param>
        /// <param name="lv">向量</param>
        /// <returns>明文</returns>
        public static Byte[] AesDecryptor(byte[] toDecryptorArray, string key, string Iv)
        {
            if (toDecryptorArray == null) return null;

            RijndaelManaged rm = new RijndaelManaged
            {
                Key = StrToHexByte(key),
                Mode = CipherMode.CBC,
                Padding = PaddingMode.Zeros,
                BlockSize = 128,
                IV = StrToHexByte(Iv)
            };

            ICryptoTransform cTransform = rm.CreateDecryptor();
            Byte[] resultArray = cTransform.TransformFinalBlock(toDecryptorArray, 0, toDecryptorArray.Length);
            return resultArray;
        }

        /// <summary>
        /// AES 算法加密(CBC模式) 将明文字节数组加密，返回密文字节数组
        /// </summary>
        /// <param name="toEncryptArray">明文字节数组</param>
        /// <returns></returns>
        public static Byte[] AesEncryptor(byte[] toEncryptArray)
        {
            if (toEncryptArray == null) return null;

            RijndaelManaged rm = new RijndaelManaged
            {
                Key = Encoding.ASCII.GetBytes(_key),
                Mode = CipherMode.CBC,
                Padding = PaddingMode.Zeros,
                BlockSize = 128,
                IV = Encoding.ASCII.GetBytes(_Iv)
            };

            ICryptoTransform cTransform = rm.CreateEncryptor();
            Byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            return resultArray;
        }

        /// <summary>
        ///AES 算法解密(CBC模式) 将密文进行解密，返回明文字节数组
        /// </summary>
        /// <param name="toDecryptorArray">密文字节数组</param>      
        /// <returns>明文</returns>
        public static Byte[] AesDecryptor(byte[] toDecryptorArray)
        {
            if (toDecryptorArray == null) return null;

            RijndaelManaged rm = new RijndaelManaged
            {
                Key = Encoding.ASCII.GetBytes(_key),
                Mode = CipherMode.CBC,
                Padding = PaddingMode.Zeros,
                BlockSize = 128,
                IV = Encoding.ASCII.GetBytes(_Iv)
            };

            ICryptoTransform cTransform = rm.CreateDecryptor();
            Byte[] resultArray = cTransform.TransformFinalBlock(toDecryptorArray, 0, toDecryptorArray.Length);
            return resultArray;
        }

        /// <summary>
        /// byte数组Hex编码
        /// </summary>
        /// <param name="bytes">需要进行编码的byte[]</param>
        /// <returns></returns>
        public static string ToHexString(byte[] bytes)
        {
            string hexString = string.Empty;
            if (bytes != null)
            {
                StringBuilder strB = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    strB.Append(bytes[i].ToString("X2"));
                }
                hexString = strB.ToString();
            }
            return hexString;
        }
        /// <summary> 
        /// 字符串进行Hex解码(Hex.decodeHex())
        /// </summary> 
        /// <param name="hexString">需要进行解码的字符串</param> 
        /// <returns></returns> 
        public static byte[] StrToHexByte(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            if ((hexString.Length % 2) != 0)
                hexString += " ";
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;
        }

    }
}