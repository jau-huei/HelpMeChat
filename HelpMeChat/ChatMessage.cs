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
        /// 构造函数
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="message">消息内容</param>
        public ChatMessage(string sender, string message)
        {
            Sender = sender;
            Message = message;
        }
    }
}