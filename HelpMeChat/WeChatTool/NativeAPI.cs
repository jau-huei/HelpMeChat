using System.Runtime.InteropServices;

namespace HelpMeChat.WeChatTool
{
    /// <summary>
    /// 提供与Windows内核API交互的原生方法封装类。
    /// </summary>
    public class NativeAPI
    {
        /// <summary>
        /// 从指定进程的内存中读取数据。
        /// </summary>
        /// <param name="hProcess">目标进程的句柄。</param>
        /// <param name="lpBaseAddress">要读取的内存起始地址。</param>
        /// <param name="lpBuffer">用于存储读取数据的缓冲区。</param>
        /// <param name="dwSize">要读取的字节数。</param>
        /// <param name="lpNumberOfBytesRead">实际读取的字节数。</param>
        /// <returns>如果读取成功，则返回true；否则返回false。</returns>
        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

        /// <summary>
        /// 将数据写入指定进程的内存中。
        /// </summary>
        /// <param name="hProcess">目标进程的句柄。</param>
        /// <param name="lpBaseAddress">要写入的内存起始地址。</param>
        /// <param name="lpBuffer">包含要写入数据的缓冲区。</param>
        /// <param name="dwSize">要写入的字节数。</param>
        /// <param name="lpNumberOfBytesWritten">实际写入的字节数。</param>
        /// <returns>如果写入成功，则返回true；否则返回false。</returns>
        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesWritten);

        /// <summary>
        /// 打开指定进程的句柄。
        /// </summary>
        /// <param name="dwDesiredAccess">所需的访问权限。</param>
        /// <param name="bInheritHandle">是否继承句柄。</param>
        /// <param name="dwProcessId">目标进程的ID。</param>
        /// <returns>如果成功，则返回进程句柄；否则返回IntPtr.Zero。</returns>
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        /// <summary>
        /// 关闭指定的句柄。
        /// </summary>
        /// <param name="hObject">要关闭的句柄。</param>
        /// <returns>如果关闭成功，则返回true；否则返回false。</returns>
        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// 进程内存读取权限常量。
        /// </summary>
        public const uint PROCESS_VM_READ = 0x0010;

        /// <summary>
        /// 进程内存写入权限常量。
        /// </summary>
        public const uint PROCESS_VM_WRITE = 0x0020;

        /// <summary>
        /// 进程内存操作权限常量。
        /// </summary>
        public const uint PROCESS_VM_OPERATION = 0x0008;
    }
}