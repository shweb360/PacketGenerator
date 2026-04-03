import re
import time
import socket
import serial  # pip install pyserial
from datetime import datetime
from pathlib import Path
import os
# ============================================================
# 配置区域
# ============================================================
base_dir = Path(__file__).parent
logs_dir = base_dir / "logs"
output_file = base_dir / "parsed_packets_filtered.txt"

# 发送模式: 可选 "serial" 或 "tcp"
send_mode = "tcp"           # ⚙️ 修改这里即可切换

# 串口配置
serial_port = "COM5"
baudrate = 9600

# TCP 配置
tcp_host = "127.0.0.1"  # ⚙️ 修改为你的服务器 IP
tcp_port = 50031

# 公共配置
send_interval = 3  # 每条报文发送间隔（秒）

# ============================================================
# 报文提取规则（仅提取 “接收” 报文）
# ============================================================
# 示例：INFO ... 接收【左岸腐蚀】0x2001 报文：A55A0100FFFF001D01FF0804042001...5AA5
# 提取所有报文
# recv_pattern = re.compile(r"接收.*?报文[:：]\s*([A-Fa-f0-9]+)", re.UNICODE)
# 提取指定报文
recv_pattern = re.compile(
    r"接收【集中器】0x1004\s*报文[:：]\s*([A-Fa-f0-9]+)",
    re.UNICODE
)

results = []

# ============================================================
# 遍历日志文件
# ============================================================
# log_files = sorted(logs_dir.glob("*.log*"))
log_files = sorted(
    logs_dir.glob("*.log*"),
    key=lambda p: os.path.getmtime(p)   # 按最后修改时间升序排序, getctime(p)是创建时间
)
if not log_files:
    print(f"× 未找到任何日志文件，请确认路径：{logs_dir}")
else:
    print(f"√ 在 {logs_dir} 中找到 {len(log_files)} 个日志文件")

for log_file in log_files:
    print(f"\n 正在读取日志文件: {log_file.name}")
    try:
        #因为日志里用了 全角空格 或编码为GBK，所以这里将utf-8修改为GBK
        with open(log_file, "r", encoding="gbk", errors="ignore") as f:  
            for line in f:
                match = recv_pattern.search(line)
                if match:
                    packet = match.group(1).strip()
                    # 校验报文完整性（A5开头5A结尾）
                    if packet.startswith("A5") and packet.endswith("5AA5"):
                        results.append(packet)
    except Exception as e:
        print(f"× 读取文件 {log_file.name} 出错: {e}")

print(f"\n√ 共提取到 {len(results)} 条【接收】报文。")

# ============================================================
# 保存结果
# ============================================================
with open(output_file, "w", encoding="utf-8") as f:
    for idx, packet in enumerate(results, 1):
        f.write(f"==== 接收报文 {idx} ====\n")
        f.write(packet + "\n\n")
print(f"√ 已保存提取结果到：{output_file}")

# ============================================================
# 发送逻辑（根据 send_mode 决定使用串口或TCP）
# ============================================================
def send_via_serial():
    try:
        with serial.Serial(serial_port, baudrate, timeout=1) as ser:
            print(f"\n√ 已打开串口 {serial_port}，开始发送报文...\n")
            for idx, packet in enumerate(results, 1):
                ts = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
                # 打印时空一行
                print(f"【{ts}】 串口发送【{idx}/{len(results)}】: {packet}\n")
                packet_bytes = bytes.fromhex(packet)
                ser.write(packet_bytes)
                with open(output_file, "a", encoding="utf-8") as f:
                    # 保存时空一行
                    f.write(f"【{ts}】 串口发送报文 {idx}: {packet}\n\n")
                time.sleep(send_interval)
        print("√ 串口发送完成。")
        # 保持程序运行
        while True:
            time.sleep(1)
    except serial.SerialException as e:
        print(f"× 串口错误: {e}")
    except Exception as e:
        print(f"× 串口发送异常: {e}")

def send_via_tcp():
    try:
        print(f"\n尝试连接 TCP 服务器 {tcp_host}:{tcp_port} ...")
        with socket.create_connection((tcp_host, tcp_port)) as client:
            print("√ 已成功连接到服务器，开始发送报文...\n")
            for idx, packet in enumerate(results, 1):
                ts = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
                # 打印时空一行
                print(f"【{ts}】 TCP发送【{idx}/{len(results)}】: {packet}\n")
                packet_bytes = bytes.fromhex(packet)
                client.sendall(packet_bytes)
                with open(output_file, "a", encoding="utf-8") as f:
                    # 保存时空一行
                    f.write(f"【{ts}】 TCP发送报文 {idx}: {packet}\n\n")
                time.sleep(send_interval)
        print("√ TCP 发送完成。")
        # 保持程序运行
        while True:
            time.sleep(1)
    except ConnectionRefusedError:
        print(f"× 无法连接到 TCP 服务器 {tcp_host}:{tcp_port}")
    except socket.error as e:
        print(f"× TCP 连接错误: {e}")
    except Exception as e:
        print(f"× TCP 发送异常: {e}")

# ============================================================
# 主流程控制
# ============================================================
if not results:
    print("× 未提取到任何【接收】报文，程序结束。")
else:
    if send_mode.lower() == "serial":
        send_via_serial()
    elif send_mode.lower() == "tcp":
        send_via_tcp()
    else:
        print("× send_mode 配置错误，请设置为 'serial' 或 'tcp'。")
