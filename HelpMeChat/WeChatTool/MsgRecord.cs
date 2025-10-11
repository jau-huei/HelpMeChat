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
        public int CreateTime { get; set; }

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
    }
}
