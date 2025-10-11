using Microsoft.Data.Sqlite;
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

        /// <summary>
        /// 根据 usrName 查询 ContactHeadImgUrl 表中的 smallHeadImgUrl
        /// </summary>
        /// <param name="userName">用户名</param>
        /// <returns>smallHeadImgUrl，如果未找到则返回 null</returns>
        public string? GetSmallHeadImgUrlByUserName(string userName)
        {
            if (string.IsNullOrEmpty(MicroMsgPath) || !File.Exists(MicroMsgPath))
            {
                return null;
            }
            using (var connection = new SqliteConnection($"Data Source={MicroMsgPath}"))
            {
                connection.Open();
                using (var command = new SqliteCommand("SELECT smallHeadImgUrl FROM ContactHeadImgUrl WHERE usrName = @userName", connection))
                {
                    command.Parameters.AddWithValue("@userName", userName);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return reader.IsDBNull(0) ? null : reader.GetString(0);
                        }
                    }
                }
            }
            return null;
        }
            
        /// <summary>
        /// 根据 strNickName 查询所有 strUsrName
        /// </summary>
        /// <param name="nickName">昵称</param>
        /// <returns>用户名的列表</returns>
        public List<string> GetUserNamesByNickName(string nickName)
        {
            var userNames = new List<string>();
            if (string.IsNullOrEmpty(MicroMsgPath) || !File.Exists(MicroMsgPath))
            {
                return userNames;
            }

            using (var connection = new SqliteConnection($"Data Source={MicroMsgPath}"))
            {
                connection.Open();
                using (var command = new SqliteCommand("SELECT strUsrName FROM Session WHERE strNickName = @nickName", connection))
                {
                    command.Parameters.AddWithValue("@nickName", nickName);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            userNames.Add(reader.GetString(0));
                        }
                    }
                }
            }
            return userNames;
        }
    }
}