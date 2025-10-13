using Microsoft.Data.Sqlite;
using System.IO;
using ProtoBuf;
using System.Text;

namespace HelpMeChat.WeChatTool
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

        /// <summary>
        /// 根据 StrTalker 获取 MSG 表最新 n 条数据
        /// </summary>
        /// <param name="strTalker">StrTalker 字段值</param>
        /// <param name="count">返回条数</param>
        /// <returns>包含 Type、CreateTime、StrContent、BytesExtra、IsSender 的列表</returns>
        public List<MsgRecord> GetLatestMessagesByTalker(string strTalker, int count)
        {
            var result = new List<MsgRecord>();
            if (string.IsNullOrEmpty(MsgXXPath) || !File.Exists(MsgXXPath))
            {
                return result;
            }
            var senderIds = new HashSet<string>();
            using (var connection = new SqliteConnection($"Data Source={MsgXXPath}"))
            {
                connection.Open();
                string sql = @"SELECT Type, CreateTime, StrContent, BytesExtra, IsSender FROM MSG WHERE StrTalker = @strTalker ORDER BY CreateTime DESC LIMIT @count";
                using (var command = new SqliteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@strTalker", strTalker);
                    command.Parameters.AddWithValue("@count", count);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string senderId = strTalker;
                            if (strTalker.Contains("@chatroom") && !reader.IsDBNull(3))
                            {
                                byte[] bytesExtra = (byte[])reader[3];
                                try
                                {
                                    ProtoMsg protoMsg;
                                    using (MemoryStream stream = new MemoryStream(bytesExtra))
                                    {
                                        protoMsg = Serializer.Deserialize<ProtoMsg>(stream);
                                    }
                                    if (protoMsg.TVMsg != null)
                                    {
                                        foreach (TVType _tmp in protoMsg.TVMsg)
                                        {
                                            if (_tmp.Type == 1)
                                            {
                                                senderId = _tmp.TypeValue;
                                                break;
                                            }
                                        }
                                    }
                                }
                                catch
                                {
                                    // 如果解析失败，保持 senderId 为 strTalker
                                }
                            }
                            senderIds.Add(senderId);
                            var msg = new MsgRecord
                            {
                                Type = reader.GetInt32(0),
                                UnixTimestamp = reader.GetInt32(1),
                                CreateDateTime = DateTimeOffset.FromUnixTimeSeconds(reader.GetInt32(1)).DateTime,
                                StrContent = reader.IsDBNull(2) ? null : reader.GetString(2),
                                BytesExtra = reader.IsDBNull(3) ? null : (byte[])reader[3],
                                IsSender = reader.GetInt32(4),
                                SenderId = senderId
                            };
                            result.Add(msg);
                        }
                    }
                }
            }

            // 获取昵称
            var nickNames = GetNickNamesByUserNames(senderIds.ToArray());
            foreach (var msg in result)
            {
                string nickName = nickNames[msg.SenderId!];
                if (msg.IsSender == 1)
                {
                    msg.NickName = $"我({nickName})";
                }
                else
                {
                    msg.NickName = nickName;
                }
            }

            return result.OrderBy(r => r.UnixTimestamp).ToList();
        }

        /// <summary>
        /// 根据用户名的数组查询 Contact 表中的昵称
        /// </summary>
        /// <param name="userNames">用户名的数组</param>
        /// <returns>用户名到昵称的字典</returns>
        public Dictionary<string, string> GetNickNamesByUserNames(string[] userNames)
        {
            var result = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(MicroMsgPath) || !File.Exists(MicroMsgPath) || userNames.Length == 0)
            {
                foreach (var userName in userNames)
                {
                    result[userName] = userName;
                }
                return result;
            }

            using (var connection = new SqliteConnection($"Data Source={MicroMsgPath}"))
            {
                connection.Open();
                // 构建 IN 查询的参数
                var parameters = new List<SqliteParameter>();
                var inClause = new StringBuilder();
                for (int i = 0; i < userNames.Length; i++)
                {
                    var paramName = $"@userName{i}";
                    parameters.Add(new SqliteParameter(paramName, userNames[i]));
                    if (i > 0) inClause.Append(", ");
                    inClause.Append(paramName);
                }
                string sql = $"SELECT UserName, NickName, Remark, Alias FROM Contact WHERE UserName IN ({inClause})";
                using (var command = new SqliteCommand(sql, connection))
                {
                    command.Parameters.AddRange(parameters.ToArray());
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string userName = reader.GetString(0);
                            string nickName = reader.IsDBNull(1) ? "" : reader.GetString(1);
                            string remark = reader.IsDBNull(2) ? "" : reader.GetString(2);
                            string alias = reader.IsDBNull(3) ? "" : reader.GetString(3);

                            string displayName = nickName;
                            if (string.IsNullOrWhiteSpace(displayName))
                            {
                                displayName = remark;
                            }
                            if (string.IsNullOrWhiteSpace(displayName))
                            {
                                displayName = alias;
                            }
                            if (string.IsNullOrWhiteSpace(displayName))
                            {
                                displayName = userName;
                            }

                            result[userName] = displayName;
                        }
                    }
                }
            }

            // 确保所有传入的 userName 都有条目
            foreach (var userName in userNames)
            {
                if (!result.ContainsKey(userName))
                {
                    result[userName] = userName;
                }
            }

            return result;
        }
    }
}