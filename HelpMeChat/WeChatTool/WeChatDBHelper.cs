using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace HelpMeChat.WeChatTool
{
    /// <summary>
    /// 提供微信数据库相关的辅助功能。
    /// </summary>
    public class WeChatDBHelper
    {
        /// <summary>
        /// 初始化向量大小
        /// </summary>
        const long IV_SIZE = 16;

        /// <summary>
        /// HMAC-SHA1哈希大小
        /// </summary>
        const int HMAC_SHA1_SIZE = 20;

        /// <summary>
        /// 密钥大小
        /// </summary>
        const int KEY_SIZE = 32;

        /// <summary>
        /// AES块大小
        /// </summary>
        const int AES_BLOCK_SIZE = 16;

        /// <summary>
        /// 默认页面大小
        /// </summary>
        const long DEFAULT_PAGESIZE = 4096;

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
            byte[]? keyBytes = GetWeChatKeyBytes(pid, account);
            if (keyBytes != null)
            {
                return BitConverter.ToString(keyBytes).Replace("-", "").ToLower();
            }
            return null;
        }

        /// <summary>
        /// 获取微信数据库解密密钥（字节数组形式）
        /// </summary>
        /// <param name="pid">微信进程ID</param>
        /// <param name="account">微信账号</param>
        /// <returns>32字节密钥字节数组，或null（失败）</returns>
        public static byte[]? GetWeChatKeyBytes(string pid, string account)
        {
            // 模仿给定的代码，使用方法2
            Process process = Process.GetProcessById(int.Parse(pid));
            ProcessModule? module = ProcessHelper.FindProcessModule(process.Id, "WeChatWin.dll");
            if (module == null)
            {
                return null;
            }
            string? version = module.FileVersionInfo.FileVersion;
            if (version == null)
            {
                return null;
            }

            List<int> read = ProcessHelper.FindProcessMemory(process.Handle, module, account);
            if (read.Count >= 2)
            {
                byte[] buffer = new byte[8];
                int key_offset = read[1] - 64;
                if (NativeAPI.ReadProcessMemory(process.Handle, module.BaseAddress + key_offset, buffer, buffer.Length, out _))
                {
                    ulong addr = BitConverter.ToUInt64(buffer, 0);

                    byte[] key_bytes = new byte[32];
                    if (NativeAPI.ReadProcessMemory(process.Handle, (IntPtr)addr, key_bytes, key_bytes.Length, out _))
                    {
                        return key_bytes;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 将十六进制字符串转换为字节数组。
        /// </summary>
        /// <param name="hex">要转换的十六进制字符串（仅包含 0-9, a-f, A-F）。</param>
        /// <returns>转换得到的字节数组。</returns>
        public static byte[]? HexToBytes(string hex)
        {
            if (string.IsNullOrEmpty(hex) || hex.Length % 2 != 0)
                return null;

            int length = hex.Length / 2;
            byte[] bytes = new byte[length];
            for (int i = 0; i < length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }

        /// <summary>
        /// 获取微信数据库 Msg 目录路径
        /// </summary>
        /// <param name="config">应用程序配置</param>
        /// <returns>Msg 目录路径</returns>
        private static string GetMsgDirectoryPath(AppConfig config)
        {
            string msgDir = config.WeChatDbPath!.TrimEnd(Path.DirectorySeparatorChar);
            if (Path.GetFileName(msgDir) != "Msg")
            {
                msgDir = Path.Combine(msgDir, "Msg");
            }
            return msgDir;
        }

        /// <summary>
        /// 解密数据库文件
        /// </summary>
        /// <param name="config">应用程序配置</param>
        /// <param name="dbName">要解密的数据库名称</param>
        /// <param name="password">解密密钥</param>
        /// <returns>解密后数据库的绝对路径，或null（失败）</returns>
        public static string? DecryptDB(AppConfig config, string dbName, string password)
        {
            if (string.IsNullOrEmpty(config.WeChatDbPath) || !Directory.Exists(config.WeChatDbPath))
                return null;

            if (string.IsNullOrEmpty(dbName))
                return null;

            var password_bytes = HexToBytes(password);
            if (password_bytes == null || password_bytes.Length != 32)
                return null;

            // 获取 Msg 目录路径并构建源文件路径
            string msgDir = GetMsgDirectoryPath(config);
            string source = Path.Combine(msgDir, dbName);

            if (!File.Exists(source))
                return null;

            string to = Path.GetTempFileName() + ".helpmechat.db";
            File.Delete(to); // 删除临时文件，我们要创建新的

            // 创建临时副本以避免文件锁定问题
            string tempSource = Path.GetTempFileName() + ".helpmechat.db";
            try
            {
                File.Copy(source, tempSource, true);
            }
            catch (Exception)
            {
                // 如果复制失败，返回null
                return null;
            }

            // 数据库头16字节是盐值
            byte[] salt_key = new byte[16];

            using (FileStream fileStream = new FileStream(tempSource, FileMode.Open, FileAccess.Read))
            {
                fileStream.Read(salt_key, 0, 16);

                // HMAC验证时用的盐值需要亦或0x3a
                byte[] hmac_salt = new byte[16];
                for (int i = 0; i < salt_key.Length; i++)
                {
                    hmac_salt[i] = (byte)(salt_key[i] ^ 0x3a);
                }
                // 计算保留段长度
                long reserved = IV_SIZE;
                reserved += HMAC_SHA1_SIZE;
                reserved = ((reserved % AES_BLOCK_SIZE) == 0) ? reserved : ((reserved / AES_BLOCK_SIZE) + 1) * AES_BLOCK_SIZE;

                // 密钥扩展，分别对应AES解密密钥和HMAC验证密钥
                byte[] key = new byte[KEY_SIZE];
                byte[] hmac_key = new byte[KEY_SIZE];
                // 注意：这里需要OpenSSLInterop，但项目中可能没有，我假设有类似的实现或简化
                // 为了简化，我将使用PBKDF2
                using (var pbkdf2 = new Rfc2898DeriveBytes(password_bytes, salt_key, 64000, HashAlgorithmName.SHA1))
                {
                    key = pbkdf2.GetBytes(KEY_SIZE);
                }
                using (var pbkdf2 = new Rfc2898DeriveBytes(key, hmac_salt, 2, HashAlgorithmName.SHA1))
                {
                    hmac_key = pbkdf2.GetBytes(KEY_SIZE);
                }

                long page_no = 0;
                long offset = 16;

                using (var hmac_sha1 = new HMACSHA1(hmac_key))
                using (FileStream tofileStream = new FileStream(to, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    try
                    {
                        while (page_no < fileStream.Length / DEFAULT_PAGESIZE)
                        {
                            byte[] decryped_page_bytes = new byte[DEFAULT_PAGESIZE];
                            byte[] going_to_hashed = new byte[DEFAULT_PAGESIZE - reserved - offset + IV_SIZE + 4];
                            fileStream.Seek((page_no * DEFAULT_PAGESIZE) + offset, SeekOrigin.Begin);
                            fileStream.Read(going_to_hashed, 0, (int)(DEFAULT_PAGESIZE - reserved - offset + IV_SIZE));

                            var page_bytes = BitConverter.GetBytes(page_no + 1);
                            page_bytes = page_bytes.Take(4).ToArray();
                            page_bytes.CopyTo(going_to_hashed, DEFAULT_PAGESIZE - reserved - offset + IV_SIZE);
                            var hash_mac_compute = hmac_sha1.ComputeHash(going_to_hashed, 0, going_to_hashed.Length);

                            byte[] hash_mac_cached = new byte[hash_mac_compute.Length];
                            fileStream.Seek((page_no * DEFAULT_PAGESIZE) + DEFAULT_PAGESIZE - reserved + IV_SIZE, SeekOrigin.Begin);
                            fileStream.Read(hash_mac_cached, 0, hash_mac_compute.Length);

                            if (!hash_mac_compute.SequenceEqual(hash_mac_cached) && page_no == 0)
                            {
                                // Hash错误
                                return null;
                            }
                            else
                            {
                                if (page_no == 0)
                                {
                                    var header_bytes = Encoding.ASCII.GetBytes(SQLITE_HEADER);
                                    header_bytes.CopyTo(decryped_page_bytes, 0);
                                }

                                byte[] page_content = new byte[DEFAULT_PAGESIZE - reserved - offset];
                                fileStream.Seek((page_no * DEFAULT_PAGESIZE) + offset, SeekOrigin.Begin);
                                fileStream.Read(page_content, 0, (int)(DEFAULT_PAGESIZE - reserved - offset));

                                byte[] iv = new byte[16];
                                fileStream.Seek((page_no * DEFAULT_PAGESIZE) + (DEFAULT_PAGESIZE - reserved), SeekOrigin.Begin);
                                fileStream.Read(iv, 0, 16);

                                var decrypted_content = AESDecrypt(page_content, key, iv);
                                decrypted_content.CopyTo(decryped_page_bytes, offset);

                                byte[] reserved_byte = new byte[reserved];
                                fileStream.Seek((page_no * DEFAULT_PAGESIZE) + DEFAULT_PAGESIZE - reserved, SeekOrigin.Begin);
                                fileStream.Read(reserved_byte, 0, (int)reserved);
                                reserved_byte.CopyTo(decryped_page_bytes, DEFAULT_PAGESIZE - reserved);

                                tofileStream.Write(decryped_page_bytes, 0, decryped_page_bytes.Length);
                            }
                            page_no++;
                            offset = 0;
                        }
                    }
                    catch (Exception)
                    {
                        // 记录错误
                        return null;
                    }
                }
            }

            // 清理临时源文件
            try
            {
                File.Delete(tempSource);
            }
            catch
            {
                // 忽略删除失败
            }

            return to;
        }

        /// <summary>
        /// 使用AES解密数据
        /// </summary>
        /// <param name="content">待解密的内容</param>
        /// <param name="key">解密密钥</param>
        /// <param name="iv">初始化向量</param>
        /// <returns>解密后的字节数组</returns>
        public static byte[] AESDecrypt(byte[] content, byte[] key, byte[] iv)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Key = key;
                aes.IV = iv;
                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    return decryptor.TransformFinalBlock(content, 0, content.Length);
                }
            }
        }

        /// <summary>
        /// 解密微信数据库文件，包括 MicroMsg.db 和最大的 MSGXX.db
        /// </summary>
        /// <param name="config">应用程序配置</param>
        /// <param name="password">解密密钥</param>
        /// <returns>包含解密路径的 DecryptedDatabases 对象，或 null（失败）</returns>
        public static DecryptedDatabases? DecryptWeChatDatabases(AppConfig config, string password)
        {
            // 解密 MicroMsg.db
            var microMsgPath = DecryptDB(config, "MicroMsg.db", password);
            if (microMsgPath == null)
            {
                return null;
            }

            // 获取 Msg 目录路径
            string msgDir = GetMsgDirectoryPath(config);
            if (!Directory.Exists(msgDir))
            {
                return null;
            }

            // 查找最大的 MSGXX.db 文件
            var msgFiles = Directory.GetFiles(Path.Combine(msgDir, "Multi"), "MSG*.db");
            int maxNum = -1;
            string? maxMsgDb = null;
            foreach (var file in msgFiles)
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (name.StartsWith("MSG") && name.Length > 3)
                {
                    var numStr = name.Substring(3);
                    if (int.TryParse(numStr, out int num) && num > maxNum)
                    {
                        maxNum = num;
                        maxMsgDb = Path.GetFileName(file);
                    }
                }
            }

            if (maxMsgDb == null)
            {
                return null;
            }

            // 解密最大的 MSGXX.db
            var msgXXPath = DecryptDB(config, Path.Combine("Multi", maxMsgDb), password);
            if (msgXXPath == null)
            {
                return null;
            }

            return new DecryptedDatabases
            {
                MicroMsgPath = microMsgPath,
                MsgXXPath = msgXXPath
            };
        }
    }
}