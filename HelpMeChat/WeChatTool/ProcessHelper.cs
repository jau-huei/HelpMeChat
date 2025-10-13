using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HelpMeChat.WeChatTool
{
    /// <summary>
    /// 提供进程相关辅助功能的类，包括模块查找和内存操作。
    /// </summary>
    public class ProcessHelper
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
        /// 查找进程中的模块。
        /// </summary>
        /// <param name="processId">进程ID。</param>
        /// <param name="moduleName">模块名称。</param>
        /// <returns>找到的进程模块，如果未找到则返回null。</returns>
        public static ProcessModule? FindProcessModule(int processId, string moduleName)
        {
            try
            {
                Process process = Process.GetProcessById(processId);
                foreach (ProcessModule module in process.Modules)
                {
                    if (module.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                    {
                        return module;
                    }
                }
            }
            catch
            {
                // 忽略异常
            }
            return null;
        }

        /// <summary>
        /// 从进程内存读取数据。
        /// </summary>
        /// <param name="processHandle">进程句柄。</param>
        /// <param name="address">内存地址。</param>
        /// <param name="size">要读取的数据大小。</param>
        /// <returns>读取的字节数组，如果失败则返回null。</returns>
        public static byte[]? ReadMemoryData(IntPtr processHandle, IntPtr address, int size)
        {
            byte[] buffer = new byte[size];
            if (ReadProcessMemory(processHandle, address, buffer, size, out int bytesRead) && bytesRead == size)
            {
                return buffer;
            }
            return null;
        }

        /// <summary>
        /// 在进程内存中搜索字符串，返回偏移列表。
        /// </summary>
        /// <param name="processHandle">进程句柄。</param>
        /// <param name="module">进程模块。</param>
        /// <param name="searchString">要搜索的字符串。</param>
        /// <returns>包含找到的偏移量的列表。</returns>
        public static List<int> FindProcessMemory(IntPtr processHandle, ProcessModule module, string searchString)
        {
            List<int> offsets = new List<int>();
            byte[] searchBytes = System.Text.Encoding.UTF8.GetBytes(searchString);
            byte[] buffer = new byte[module.ModuleMemorySize];

            if (ReadProcessMemory(processHandle, module.BaseAddress, buffer, buffer.Length, out int bytesRead))
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
                        offsets.Add(i);
                    }
                }
            }
            return offsets;
        }
    }
}