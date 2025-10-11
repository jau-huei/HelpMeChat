using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HelpMeChat.WeChatTool
{
    /// <summary>
    /// 提供原生API辅助功能的类，用于进程内存操作。
    /// </summary>
    public class NativeAPIHelper
    {
        /// <summary>
        /// 从指定进程的内存中读取数据（私有方法）。
        /// </summary>
        /// <param name="hProcess">目标进程的句柄。</param>
        /// <param name="lpBaseAddress">要读取的内存起始地址。</param>
        /// <param name="lpBuffer">用于存储读取数据的缓冲区。</param>
        /// <param name="dwSize">要读取的字节数。</param>
        /// <param name="lpNumberOfBytesRead">实际读取的字节数。</param>
        /// <returns>如果读取成功，则返回true；否则返回false。</returns>
        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

        /// <summary>
        /// 搜索进程所有内存中的字符串，返回地址列表。
        /// </summary>
        /// <param name="process">要搜索的进程对象。</param>
        /// <param name="searchString">要搜索的字符串。</param>
        /// <returns>包含找到的地址的列表。</returns>
        public static List<long> SearchProcessAllMemory(Process process, string searchString)
        {
            List<long> addresses = new List<long>();
            byte[] searchBytes = System.Text.Encoding.UTF8.GetBytes(searchString);

            // 获取进程内存信息（简化版，实际需枚举内存区域）
            // 这里简化：搜索主要模块内存
            foreach (ProcessModule module in process.Modules)
            {
                byte[] buffer = new byte[module.ModuleMemorySize];
                if (ReadProcessMemory(process.Handle, module.BaseAddress, buffer, buffer.Length, out int bytesRead))
                {
                    for (int i = 0; i < buffer.Length - searchBytes.Length; i++)
                    {
                        bool found = true;
                        for (int j = 0; j < searchBytes.Length; j++)
                        {
                            if (buffer[i + j] != searchBytes[j])
                            {
                                found = false;
                                break;
                            }
                        }
                        if (found)
                        {
                            addresses.Add((long)module.BaseAddress + i);
                        }
                    }
                }
            }
            return addresses;
        }
    }
}