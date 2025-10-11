using System.Diagnostics;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace HelpMeChat
{
    /// <summary>
    /// 提供微信数据库解密密钥获取和数据库解密功能的类。
    /// </summary>
    public class WeChatKeyHelper
    {
        /// <summary>
        /// SQLite数据库文件头常量。
        /// </summary>
        private const string SQLITE_HEADER = "SQLite format 3";

        /// <summary>
        /// 获取微信数据库解密密钥（固定使用账号搜索方法）。
        /// </summary>
        /// <param name="pid">微信进程ID。</param>
        /// <param name="account">微信账号。</param>
        /// <returns>32字节密钥的16进制字符串（小写），或null（失败）。</returns>
        public static string? GetWeChatKey(string pid, string account)
        {
            try
            {
                Process process = Process.GetProcessById(int.Parse(pid));
                ProcessModule? module = ProcessHelper.FindProcessModule(process.Id, "WeChatWin.dll");
                if (module == null)
                {
                    throw new Exception("未找到WeChatWin.dll模块");
                }

                string? version = module.FileVersionInfo.FileVersion;
                if (string.IsNullOrEmpty(version))
                {
                    throw new Exception("无法获取微信版本");
                }

                // 固定使用方法2：账号搜索
                List<int> read = ProcessHelper.FindProcessMemory(process.Handle, module, account);
                if (read.Count >= 2)
                {
                    byte[] buffer = new byte[8];
                    int keyOffset = read[1] - 64;
                    if (NativeAPI.ReadProcessMemory(process.Handle, module.BaseAddress + keyOffset, buffer, buffer.Length, out _))
                    {
                        ulong addr = BitConverter.ToUInt64(buffer, 0);
                        byte[] keyBytes = new byte[32];
                        if (NativeAPI.ReadProcessMemory(process.Handle, (IntPtr)addr, keyBytes, keyBytes.Length, out _))
                        {
                            return BitConverter.ToString(keyBytes).Replace("-", "").ToLower();
                        }
                    }
                }
                else
                {
                    throw new Exception("账号搜索失败，请确认账号正确");
                }

                return null;
            }
            catch
            {
                // 记录错误（可扩展为日志）
                return null;
            }
        }
    }
}