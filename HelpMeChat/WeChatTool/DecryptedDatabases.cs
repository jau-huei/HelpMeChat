using System;
using System.IO;

namespace HelpMeChat
{
    /// <summary>
    /// 解密后的数据库路径类，实现 IDisposable 以自动删除文件
    /// </summary>
    public class DecryptedDatabases : IDisposable
    {
        /// <summary>
        /// MicroMsg.db 解密后的路径
        /// </summary>
        public string? MicroMsgPath { get; set; }

        /// <summary>
        /// MSGXX.db 解密后的路径（XX 为最大数字）
        /// </summary>
        public string? MsgXXPath { get; set; }

        /// <summary>
        /// 释放资源，删除解密后的文件
        /// </summary>
        public void Dispose()
        {
            if (!string.IsNullOrEmpty(MicroMsgPath) && File.Exists(MicroMsgPath))
            {
                try
                {
                    File.Delete(MicroMsgPath);
                }
                catch
                {
                    // 忽略删除失败
                }
            }
            if (!string.IsNullOrEmpty(MsgXXPath) && File.Exists(MsgXXPath))
            {
                try
                {
                    File.Delete(MsgXXPath);
                }
                catch
                {
                    // 忽略删除失败
                }
            }
        }
    }
}