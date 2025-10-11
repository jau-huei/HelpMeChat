namespace HelpMeChat.WeChatTool
{
    /// <summary>
    /// 表示 MSG 表中的一条消息数据
    /// </summary>
    public class MsgRecord
    {
        /// <summary>
        /// 消息类型
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// 创建时间（Unix 时间戳）
        /// </summary>
        public int UnixTimestamp { get; set; }

        /// <summary>
        /// 创建时间（DateTime）
        /// </summary>
        public DateTime? CreateDateTime { get; set; }

        /// <summary>
        /// 消息内容
        /// </summary>
        public string? StrContent { get; set; }

        /// <summary>
        /// 附加字节内容
        /// </summary>
        public byte[]? BytesExtra { get; set; }

        /// <summary>
        /// 是否为发送者
        /// </summary>
        public int IsSender { get; set; }

        /// <summary>
        /// 发送者 ID
        /// </summary>
        public string? SenderId { get; set; }

        /// <summary>
        /// 发送者昵称
        /// </summary>
        public string? NickName { get; set; }

        /// <summary>
        /// 返回消息的字符串表示，用于调试
        /// </summary>
        /// <returns>包含关键信息的字符串</returns>
        public override string ToString()
        {
            return $"Type: {Type}, Time: {UnixTimestamp}, Content: {StrContent ?? "null"}, Sender: {NickName ?? "null"} ({SenderId ?? "null"})";
        }

        /// <summary>
        /// 转换为 ChatMessage 对象
        /// </summary>
        /// <returns>ChatMessage 实例</returns>
        public ChatMessage ToChatMessage()
        {
            return new ChatMessage(NickName ?? SenderId ?? "Unknown", StrContent ?? "", UnixTimestamp);
        }
    }
}
