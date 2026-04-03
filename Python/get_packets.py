import re
import time
from datetime import datetime
from pathlib import Path
import serial  # pip install pyserial

# ============================================================
# 配置区域
# ============================================================
base_dir = Path(__file__).parent
logs_dir = base_dir / "logs"         # 存放所有日志文件的目录
output_file = base_dir / "parsed_packets_filtered.txt"

# 串口配置
serial_port = "COM1"       # 串口号（根据实际修改）
baudrate = 9600            # 波特率（根据设备）
send_interval = 2           # 每条报文发送间隔（秒）

# ============================================================
# 正则表达式与过滤条件
# ============================================================
pattern = re.compile(r"(55 55(?: [0-9A-Fa-f]{2})+)")
valid_prefixes = [
    "55 55 72 01",
    "55 55 72 BC",
    "55 55 72 0B",
    "55 55 72 0C",
    "55 55 72 07"
]

results = []

# ============================================================
# 遍历 logs 文件夹下所有日志文件 (*.log, *.log.1, 等)
# ============================================================
log_files = sorted(logs_dir.glob("*.log*"))  # 匹配多种日志扩展
if not log_files:
    print(f"× 未找到任何日志文件，请确认路径：{logs_dir}")
else:
    print(f"√ 在 {logs_dir} 中找到 {len(log_files)} 个日志文件")

# ============================================================
# 从所有日志文件中提取报文
# ============================================================
for log_file in log_files:
    print(f"\n 正在读取日志文件: {log_file.name}")
    try:
        with open(log_file, "r", encoding="utf-8", errors="ignore") as f:
            for line in f:
                match = pattern.search(line)
                if match:
                    packet = match.group(1).strip()
                    # 判断是否属于目标前缀
                    if any(packet.startswith(prefix) for prefix in valid_prefixes):
                        results.append(packet)
    except Exception as e:
        print(f"× 读取文件 {log_file.name} 出错: {e}")

print(f"\n√ 共提取到 {len(results)} 条目标报文。")

# ============================================================
# 保存结果到文件
# ============================================================
with open(output_file, "w", encoding="utf-8") as f:
    for idx, packet in enumerate(results, 1):
        f.write(f"==== 报文 {idx} ====\n")
        f.write(packet + "\n\n")

print(f"√ 已保存提取结果到：{output_file}")

# ============================================================
# 串口发送逻辑
# ============================================================
try:
    with serial.Serial(serial_port, baudrate, timeout=1) as ser:
        print(f"\n 开始发送到串口 {serial_port} ...")
        for idx, packet in enumerate(results, 1):
            ts = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            print(f"【{ts}】 发送报文【{idx}/{len(results)}】: {packet}\n")

            # 转换为字节并发送
            packet_bytes = bytes(int(b, 16) for b in packet.split())
            ser.write(packet_bytes)

            # 记录到输出文件
            with open(output_file, "a", encoding="utf-8") as f:
                f.write(f"【{ts}】 发送报文 {idx}: {packet}\n\n")

            time.sleep(send_interval)

    print("√ 所有报文已发送完成。")

except serial.SerialException as e:
    print(f"× 串口错误: {e}")
except Exception as e:
    print(f"× 其他错误: {e}")
