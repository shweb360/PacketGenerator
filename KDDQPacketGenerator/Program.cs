using System;
using System.Collections.Generic;
using System.Linq;
using System.IO.Ports;
using System.Threading;
using KDDQPacketGenerator.Utils;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Data;
using System.ComponentModel;
using System.Threading.Tasks;

namespace KDDQPacketGenerator
{
    class Program
    {
        private static SerialPort serialPort;
        private static System.Threading.Timer _timer;
        private static int sendIndex;
        private static Random random = new Random();
        private static CancellationTokenSource _cts;
        public delegate int Calc(int x, int y);
        private static int SendInterval;
        private static bool IsPrintMsg;
        static async Task Main(string[] args)
        {
           

            int.TryParse(System.Configuration.ConfigurationManager.AppSettings["SendInterval"], out int interval);
            SendInterval = interval;
            bool.TryParse(System.Configuration.ConfigurationManager.AppSettings["IsPrintMsg"], out bool flag);
            IsPrintMsg = flag;

           
            #region 同步码模拟生成
            //// 生成一个
            //string code = HexCodeGenerator.GenerateUniqueHexCode();
            //Console.WriteLine("单个生成：" + code);

            //// 批量生成5个
            //var codes = HexCodeGenerator.GenerateUniqueHexCodes(5);
            //Console.WriteLine("批量生成：");
            //foreach (var c in codes)
            //{
            //    Console.WriteLine(c);
            //}
            #endregion

            #region 2573 模拟报文生成
            Console.Title = $"KD2573B 模拟终端报文生成器 V1.0";

            Console.WriteLine("=== 串口模拟器启动 ===");

            // 初始化串口
            serialPort = new SerialPort
            {
                PortName = "COM1",
                BaudRate = 115200,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One
            };
            try
            {
                serialPort.Open();
                Console.WriteLine($"已打开串口 {serialPort.PortName}");

                Console.WriteLine("准备发送数据...");
                // 启动定时器，每秒发送一次数据
                //_timer = new Timer(SendMockData, null, 3000, 15000);  // 每1000ms发送一次

                _cts = new CancellationTokenSource();

                // 注册 Ctrl+C 取消信号
                Console.CancelKeyPress += (s, e) =>
                {
                    e.Cancel = true;
                    _cts.Cancel();
                    Console.WriteLine("收到取消信号，正在退出...");
                };

                var excelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Template", "Devices.xls");
                if (!File.Exists(excelPath))
                {
                    Console.WriteLine($"错误：模板文件不存在于 {excelPath}");
                }
                var dt = NPOIHelper.ExcelToDataTable(excelPath, true);
                if (dt == null || dt.Rows.Count == 0)
                {
                    Console.WriteLine($"提示：设备列表数据为空！");
                }

                await RunSendLoopAsyncB(dt,_cts.Token);

                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"串口打开失败: {ex.Message}");
                Console.ResetColor();
            }
            finally
            {
                _cts?.Cancel();
                _timer?.Dispose();
                if (serialPort.IsOpen)
                    serialPort.Close();
            }
            #endregion
            Console.ReadKey();
        }
        /// <summary>
        /// 发送2573B数据()
        /// </summary>
        /// <param name="dt"></param>
        static async Task RunSendLoopAsyncB(DataTable dt,CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    sendIndex = sendIndex > 255 ? 0 : sendIndex;

                    GenerateTimeUsingDictionary(dt);

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        var item = new Device();
                        item.imei = dt.Rows[i]["imei"].ToString();
                        item.code = dt.Rows[i]["code"].ToString();
                        item.phaseType = (PhaseEnum)Enum.Parse(typeof(PhaseEnum), dt.Rows[i]["phaseType"].ToString());
                        item.groupNo = int.Parse(dt.Rows[i]["groupNo"].ToString());
                        item.samplingTime = DateTime.Parse(dt.Rows[i]["sampleTime"].ToString());


                        byte[] byteArray = SendMockeDataB(item,true);

                        #region 发送报文
                        //发送0x0B
                        GetSendPacket(item, CtrlCodeEnum.MeasureB_0B, byteArray);
                        await Task.Delay(SendInterval, token);

                        //发送0x0c
                        GetSendPacket(item, CtrlCodeEnum.MeasureB_0C, byteArray);
                        await Task.Delay(SendInterval, token);

                        //发送0x00
                        byteArray = SendMockeDataB(item, false);
                        GetSendPacket(item, CtrlCodeEnum.MeasureB_00, byteArray);
                        #endregion

                        Console.WriteLine($"--------------------------------");
                    }

                    sendIndex++;
                    Console.WriteLine($"--------------------------------");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[发送失败] : {ex.Message}");
                }
            }

        }

        static byte[] SendMockeDataB(Device item, bool IsHasData)
        {
            List<byte> list = new List<byte>();

            list.Add(0xA5); //数据起始
            list.Add(0x5A);

            list.Add(0x70); //版本号

            var timeByte = new byte[6]; //采样时间
            timeByte[0] = (Byte)(Convert.ToInt16(item.samplingTime.Year) % 2000);
            timeByte[1] = (Byte)(Convert.ToInt16(item.samplingTime.Month));
            timeByte[2] = (Byte)(Convert.ToInt16(item.samplingTime.Day));
            timeByte[3] = (Byte)(Convert.ToInt16(item.samplingTime.Hour));
            timeByte[4] = (Byte)(Convert.ToInt16(item.samplingTime.Minute));
            timeByte[5] = (Byte)(Convert.ToInt16(item.samplingTime.Second));
            list.AddRange(timeByte);

            byte[] imeiBytes = HexCodeGenerator.HexStringToByteArray(item.imei); //imei
            list.AddRange(imeiBytes);

            list.Add((byte)item.phaseType); //相位

            list.Add((byte)sendIndex); //发送序号

            ushort value = 600; //测量间隔
            byte[] tempbytes = BitConverter.GetBytes(value).Reverse().ToArray();
            list.AddRange(tempbytes);

            list.Add((byte)item.groupNo);  //延时序号

            float temperature = (float)GenerateRandomValue(25.1, 25.5);//温度
            temperature = (float)Math.Round(temperature, 1);
            tempbytes = BitConverter.GetBytes(temperature).Reverse().ToArray();
            list.AddRange(tempbytes);

            list.Add(0x64);//电量100

            list.Add((byte)sendIndex);//采集ID

            list.Add(0x00);   //雷击次数

            #region 数据域
            if (IsHasData)
            {
                byte simpleTimes = 14;
                list.Add(simpleTimes);//采样次数
                list.AddRange(new byte[] { 0x00, 0x00 });//同步状态

                float freq = 50; //最后一次基波信号频率
                tempbytes = BitConverter.GetBytes(freq).Reverse().ToArray();
                list.AddRange(tempbytes);

                float phaseAvg = (float)GenerateRandomValue(118.1, 119.0); //相位差均值
                tempbytes = BitConverter.GetBytes(phaseAvg).Reverse().ToArray();
                list.AddRange(tempbytes);

                double Ix1Avg = GenerateRandomValue(0.415, 0.419); //幅值均值
                tempbytes = BitConverter.GetBytes(Ix1Avg).Reverse().ToArray();
                list.AddRange(tempbytes);

                //14次相位
                for (byte j = 0; j < simpleTimes; j++)
                {
                    tempbytes = BitConverter.GetBytes(phaseAvg).Reverse().ToArray();
                    list.AddRange(tempbytes);
                }
            }
            #endregion

            return list.ToArray();
        }

        /// <summary>
        /// 发送2573B数据()
        /// </summary>
        /// <param name="dt"></param>
        static void SendMockDataB_01(DataTable dt)
        {
            try
            {
                sendIndex = sendIndex > 255 ? 0 : sendIndex;

                GenerateTimeUsingDictionary(dt);

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    var item = new Device();
                    item.imei = dt.Rows[i]["imei"].ToString();
                    item.code = dt.Rows[i]["code"].ToString();
                    item.phaseType = (PhaseEnum)Enum.Parse(typeof(PhaseEnum), dt.Rows[i]["phaseType"].ToString());
                    item.groupNo = int.Parse(dt.Rows[i]["groupNo"].ToString());

                    item.samplingTime = DateTime.Parse(dt.Rows[i]["sampleTime"].ToString());

                    List<byte> list = new List<byte>();
                    double tempValue = 0;

                    list.Add(0xA5); //数据起始
                    list.Add(0x5A);

                    list.Add(0x70); //版本号

                    var timeByte = new byte[6]; //采样时间
                    timeByte[0] = (Byte)(Convert.ToInt16(item.samplingTime.Year) % 2000);
                    timeByte[1] = (Byte)(Convert.ToInt16(item.samplingTime.Month));
                    timeByte[2] = (Byte)(Convert.ToInt16(item.samplingTime.Day));
                    timeByte[3] = (Byte)(Convert.ToInt16(item.samplingTime.Hour));
                    timeByte[4] = (Byte)(Convert.ToInt16(item.samplingTime.Minute));
                    timeByte[5] = (Byte)(Convert.ToInt16(item.samplingTime.Second));
                    list.AddRange(timeByte);

                    byte[] imeiBytes = HexCodeGenerator.HexStringToByteArray(item.imei); //imei
                    list.AddRange(imeiBytes);

                    list.Add((byte)item.phaseType); //相位

                    list.Add((byte)sendIndex); //发送序号

                    ushort value = 600; //测量间隔
                    byte[] tempbytes = BitConverter.GetBytes(value).Reverse().ToArray();
                    list.AddRange(tempbytes);

                    list.Add((byte)item.groupNo);  //延时序号

                    float temperature = (float)GenerateRandomValue(20.5, 25.5);//温度
                    temperature = (float)Math.Round(temperature, 1);
                    tempbytes = BitConverter.GetBytes(temperature).Reverse().ToArray();
                    list.AddRange(tempbytes);

                    list.Add(0x64);//电量100

                    list.Add((byte)sendIndex);//采集ID

                    list.Add(0x00);   //雷击次数

                    #region 数据域
                    tempValue = GenerateRandomValue(0.415, 0.452); //基波幅值
                    tempbytes = BitConverter.GetBytes(tempValue).Reverse().ToArray();
                    list.AddRange(tempbytes);

                    tempValue = GenerateRandomValue(238.043, 253.931); //基波相位
                    tempbytes = BitConverter.GetBytes(tempValue).Reverse().ToArray();
                    list.AddRange(tempbytes);

                    tempValue = 0; //三次谐波幅值
                    tempbytes = BitConverter.GetBytes(tempValue).Reverse().ToArray();
                    list.AddRange(tempbytes);

                    tempValue = 0; //三次谐波相位
                    tempbytes = BitConverter.GetBytes(tempValue).Reverse().ToArray();
                    list.AddRange(tempbytes);

                    tempValue = 0; //五次谐波幅值
                    tempbytes = BitConverter.GetBytes(tempValue).Reverse().ToArray();
                    list.AddRange(tempbytes);

                    tempValue = 0; //五次谐波相位
                    tempbytes = BitConverter.GetBytes(tempValue).Reverse().ToArray();
                    list.AddRange(tempbytes);

                    tempValue = 0; //七次谐波幅值
                    tempbytes = BitConverter.GetBytes(tempValue).Reverse().ToArray();
                    list.AddRange(tempbytes);

                    tempValue = 0; //七次谐波相位
                    tempbytes = BitConverter.GetBytes(tempValue).Reverse().ToArray();
                    list.AddRange(tempbytes);

                    tempValue = 0; //平均相位差
                    tempbytes = BitConverter.GetBytes(tempValue).Reverse().ToArray();
                    list.AddRange(tempbytes);

                    list.Add(0x00); //0成功，1失败，2等待
                    #endregion

                    byte[] byteArray = list.ToArray();
                    GetSendPacket(item,CtrlCodeEnum.MeasureB_01, byteArray);
                    Thread.Sleep(SendInterval);
                }
                Console.WriteLine($"--------------------------------");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送失败: {ex.Message}");
            }
            sendIndex++;
        }

        /// <summary>
        /// 发送2573T数据
        /// </summary>
        /// <param name="state"></param>
        static void SendMockDataT(object state)
        {
            if (serialPort == null || !serialPort.IsOpen)
                return;

            sendIndex = sendIndex > 255 ? 0 : sendIndex;

            List<Device> devices = new List<Device>();

            DateTime time = DateTime.Now;
            int delayIndex = 2;

            devices.Add(new Device
            {
                imei = "00147DAD",
                code = "T0001",
                phaseType = PhaseEnum.Undefined,
                samplingTime = time,
                groupNo = delayIndex,
            });

            try
            {
                foreach (var item in devices)
                {
                    List<byte> list = new List<byte>();
                    double tempValue = 0;

                    list.Add(0xA5); //数据起始
                    list.Add(0x5A);

                    list.Add(0x70); //版本号


                    var timeByte = new byte[6]; //采样时间
                    timeByte[0] = (Byte)(Convert.ToInt16(item.samplingTime.Year) % 2000);
                    timeByte[1] = (Byte)(Convert.ToInt16(item.samplingTime.Month));
                    timeByte[2] = (Byte)(Convert.ToInt16(item.samplingTime.Day));
                    timeByte[3] = (Byte)(Convert.ToInt16(item.samplingTime.Hour));
                    timeByte[4] = (Byte)(Convert.ToInt16(item.samplingTime.Minute));
                    timeByte[5] = (Byte)(Convert.ToInt16(item.samplingTime.Second));
                    list.AddRange(timeByte);

                    byte[] imeiBytes = HexCodeGenerator.HexStringToByteArray(item.imei); //imei
                    list.AddRange(imeiBytes);

                    list.Add((byte)item.phaseType); //相位

                    list.Add((byte)sendIndex); //发送序号

                    ushort value = 600; //测量间隔
                    byte[] tempbytes = BitConverter.GetBytes(value).Reverse().ToArray();
                    list.AddRange(tempbytes);

                    list.Add((byte)delayIndex); //延时序号

                    float temperature = (float)GenerateRandomValue(20.5, 28.5);//温度
                    temperature = (float)Math.Round(temperature, 1);
                    tempbytes = BitConverter.GetBytes(temperature).Reverse().ToArray();
                    list.AddRange(tempbytes);

                    list.Add(0x64);//电量100

                    list.Add((byte)sendIndex);//采集ID

                    list.Add(0x00);   //雷击次数

                    #region 数据域
                    tempValue = GenerateRandomValue(0.835, 1.089); //铁芯接地泄露电流
                    tempbytes = BitConverter.GetBytes(tempValue).Reverse().ToArray();
                    list.AddRange(tempbytes);

                    tempValue = GenerateRandomValue(0.219, 1.488); //夹件泄露电流
                    tempbytes = BitConverter.GetBytes(tempValue).Reverse().ToArray();
                    list.AddRange(tempbytes);
                    #endregion

                    byte[] byteArray = list.ToArray();
                    GetSendPacket(item, CtrlCodeEnum.MeasureT, byteArray);
                    Thread.Sleep(SendInterval);
                }
                Console.WriteLine($"--------------------------------");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送失败: {ex.Message}");
            }
            sendIndex++;
        }

        /// <summary>
        /// 发送2573W数据
        /// </summary>
        /// <param name="state"></param>
        static void SendMockDataW(object state)
        {
            if (serialPort == null || !serialPort.IsOpen)
                return;

            sendIndex = sendIndex > 255 ? 0 : sendIndex;

            List<Device> devices = new List<Device>();

            DateTime time = DateTime.Now;
            int delayIndex = 2;

            devices.Add(new Device
            {
                imei = "00147DAA",
                code = "W0001",
                phaseType = PhaseEnum.Undefined,
                samplingTime = time,
                groupNo = delayIndex,
            });

            try
            {
                foreach (var item in devices)
                {
                    List<byte> list = new List<byte>();
                    double tempValue = 0;

                    list.Add(0xA5); //数据起始
                    list.Add(0x5A);

                    list.Add(0x70); //版本号

                    var timeByte = new byte[6]; //采样时间
                    timeByte[0] = (Byte)(Convert.ToInt16(item.samplingTime.Year) % 2000);
                    timeByte[1] = (Byte)(Convert.ToInt16(item.samplingTime.Month));
                    timeByte[2] = (Byte)(Convert.ToInt16(item.samplingTime.Day));
                    timeByte[3] = (Byte)(Convert.ToInt16(item.samplingTime.Hour));
                    timeByte[4] = (Byte)(Convert.ToInt16(item.samplingTime.Minute));
                    timeByte[5] = (Byte)(Convert.ToInt16(item.samplingTime.Second));
                    list.AddRange(timeByte);

                    byte[] imeiBytes = HexCodeGenerator.HexStringToByteArray(item.imei); //imei
                    list.AddRange(imeiBytes);

                    list.Add((byte)item.phaseType); //相位

                    list.Add((byte)sendIndex); //发送序号

                    ushort value = 600; //测量间隔
                    byte[] tempbytes = BitConverter.GetBytes(value).Reverse().ToArray();
                    list.AddRange(tempbytes);

                    list.Add((byte)delayIndex); //延时序号

                    float temperature = (float)GenerateRandomValue(20.5, 25.5);//温度
                    temperature = (float)Math.Round(temperature, 1);
                    tempbytes = BitConverter.GetBytes(temperature).Reverse().ToArray();
                    list.AddRange(tempbytes);

                    list.Add(0x64);//电量100

                    list.Add((byte)sendIndex);//采集ID

                    list.Add(0x00);   //雷击次数


                    tempValue = GenerateRandomValue(0.415, 0.452); //A相电压幅值
                    tempbytes = BitConverter.GetBytes(tempValue).Reverse().ToArray();
                    list.AddRange(tempbytes);

                    tempValue = GenerateRandomValue(238.043, 253.931); //A相电压相位
                    tempbytes = BitConverter.GetBytes(tempValue).Reverse().ToArray();
                    list.AddRange(tempbytes);

                    tempValue = 0;
                    tempbytes = BitConverter.GetBytes(tempValue).Reverse().ToArray();//B相电压幅值
                    list.AddRange(tempbytes);

                    tempValue = 0;
                    tempbytes = BitConverter.GetBytes(tempValue).Reverse().ToArray();//C相电压幅值
                    list.AddRange(tempbytes);

                    tempValue = 0;
                    tempbytes = BitConverter.GetBytes(tempValue).Reverse().ToArray();//C相电压相位
                    list.AddRange(tempbytes);

                    tempValue = 0;
                    tempbytes = BitConverter.GetBytes(tempValue).Reverse().ToArray(); //温度
                    list.AddRange(tempbytes);

                    tempValue = 0;
                    tempbytes = BitConverter.GetBytes(tempValue).Reverse().ToArray(); //湿度
                    list.AddRange(tempbytes);

                    tempValue = 0;
                    tempbytes = BitConverter.GetBytes(tempValue).Reverse().ToArray(); //气压
                    list.AddRange(tempbytes);

                    list.Add(0x00); //0成功，1失败，2等待

                    byte[] byteArray = list.ToArray();
                    GetSendPacket(item, CtrlCodeEnum.MeasureW, byteArray);
                    Thread.Sleep(SendInterval);
                }
                Console.WriteLine($"--------------------------------");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送失败: {ex.Message}");
            }
            sendIndex++;
        }

        /// <summary>
        /// 生成指定范围的随机数的辅助方法
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static double GenerateRandomValue(double min, double max)
        {
            return random.NextDouble() * (max - min) + min;
        }
        /// <summary>
        /// 通过 Dictionary 记录遍历到的组别时间
        /// </summary>
        static void GenerateTimeUsingDictionary(DataTable table)
        {
            DateTime baseTime = DateTime.Now;
            int offsetSeconds = 0; // 定义一个递增的时间差

            var timeCache = new Dictionary<string, DateTime>(); // 如果明确知道是 int，也可以写 Dictionary<int, DateTime>
            foreach (DataRow row in table.Rows)
            {
                string groupNo = row["groupNo"].ToString();
                // 如果字典里面没有存过该分组的时间
                if (!timeCache.ContainsKey(groupNo))
                {
                    timeCache[groupNo] = baseTime.AddSeconds(offsetSeconds);
                    offsetSeconds += 5;
                }
                if (!table.Columns.Contains("sampleTime"))
                {
                    table.Columns.Add("sampleTime", typeof(DateTime)); 
                }
                // 从缓存字典中提取时间，写入当前行
                row["sampleTime"] = timeCache[groupNo];
            }
        }

        public struct Device
        {
            public string imei { get; set; }
            public string code { get; set; }
            public PhaseEnum phaseType { get; set; }
            public DateTime samplingTime { get; set; }
            public int groupNo { get; set; }
            public byte[] sendData { get; set; }
        }
        public enum PhaseEnum
        {
            Undefined,
            A,
            B,
            C,
            CT_A,
            CT_B,
            CT_C,
            CT,  //单独的旧设备
            CT_ABC,  // CT 关联ABC三相
            PT
        }
        /// <summary>
        /// 控制字
        /// </summary>
        public enum CtrlCodeEnum
        {
            [Description("2573B 0x00报文")]
            MeasureB_00 = 0x00,
            [Description("2573B 0x01报文")]
            MeasureB_01 = 0x01,
            [Description("2573T 0x07报文")]
            MeasureT = 0x07,
            [Description("2573W 0x08报文")]
            MeasureW = 0x08,
            [Description("2573B 0x0B报文")]
            MeasureB_0B = 0x0B,
            [Description("2573B 0x0C报文")]
            MeasureB_0C = 0x0C,
        }
        /// <summary>
        /// 生成发送数据包
        /// </summary>
        /// <param name="item"></param>
        /// <param name="time"></param>
        /// <param name="byteArray"></param>
        private static void  GetSendPacket(Device item, CtrlCodeEnum ctrlCode, byte[] byteArray)
        {
            byte[] byteArrayAES = AESUtil.AesEncryptor(byteArray);

            var sendByte = new List<byte>(byteArrayAES.Length + 5);

            sendByte.Add(0x55); //起始码
            sendByte.Add(0x55);

            sendByte.Add((byte)(byteArrayAES.Length + 2)); //数据域长度，即114

            sendByte.Add((byte)ctrlCode); //控制字

            byte checkSum = 0;
            for (int i = 0; i < byteArrayAES.Length; i++)
            {
                checkSum += byteArrayAES[i];
            }
            sendByte.Add(checkSum); //校验和

            sendByte.InsertRange(5, byteArrayAES); // 在索引2处插入

            var sendData = sendByte.ToArray();
            serialPort.Write(sendData, 0, sendData.Length);
            
            Console.WriteLine($"[{item.samplingTime:HH:mm:ss}] {item.code}【{item.phaseType}】分组编号={item.groupNo}  发送序号={sendIndex}  0x{(byte)ctrlCode:X2} 共 {sendData.Length} 字节");
            if (IsPrintMsg)
            {
                Console.WriteLine(BitConverter.ToString(sendData).Replace("-", "") + "\r\n");
            }
        }
    }
}
