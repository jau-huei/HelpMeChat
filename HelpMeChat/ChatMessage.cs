namespace HelpMeChat
{
    /// <summary>
    /// 聊天消息类
    /// </summary>
    public class ChatMessage
    {
        /// <summary>
        /// 发送者
        /// </summary>
        public string Sender { get; set; }

        /// <summary>
        /// 消息内容
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 消息产生时间
        /// </summary>
        public DateTime? Time { get; set; }

        /// <summary>
        /// 初始化一个新的 <see cref="ChatMessage"/> 实例。
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="message">消息内容</param>
        public ChatMessage(string sender, string message)
        {
            Sender = sender;
            Message = message;
            Time = null;
        }

        /// <summary>
        /// 初始化一个新的 <see cref="ChatMessage"/> 实例，并指定时间。
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="message">消息内容</param>
        /// <param name="time">消息产生时间</param>
        public ChatMessage(string sender, string message, DateTime time)
        {
            Sender = sender;
            Message = message;
            Time = time;
        }

        /// <summary>
        /// 初始化一个新的 <see cref="ChatMessage"/> 实例，时间戳格式。
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="message">消息内容</param>
        /// <param name="unixTimestamp">Unix 时间戳（秒）</param>
        public ChatMessage(string sender, string message, long unixTimestamp)
        {
            Sender = sender;
            Message = message;
            Time = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).DateTime;
        }

        /// <summary>
        /// 返回聊天消息的字符串表示，用于调试
        /// </summary>
        /// <returns>包含关键信息的字符串</returns>
        public override string ToString()
        {
            return $"Sender: {Sender}, Message: {Message}, Time: {Time?.ToString() ?? "null"}";
        }

        /// <summary>
        /// 返回格式化的聊天消息字符串
        /// </summary>
        /// <returns>格式为 [YYYY/MM/dd HH:mm:ss] Sender\nMessage 的字符串</returns>
        public string ToFormattedString()
        {
            string timeStr = Time?.ToString("yyyy/MM/dd HH:mm:ss") ?? "未知时间";
            return $"[{timeStr}] {Sender}\n{Message}";
        }
    }
}