using System.Collections.Generic;

namespace HelpMeChat
{
    /// <summary>
    /// 参数类，用于传递弹出窗口的参数
    /// </summary>
    public class ShowPopupArgs
    {
        /// <summary>
        /// 弹出窗口的左边距
        /// </summary>
        public double Left { get; set; }

        /// <summary>
        /// 弹出窗口的上边距
        /// </summary>
        public double Top { get; set; }

        /// <summary>
        /// 聊天历史记录列表
        /// </summary>
        public List<ChatMessage>? History { get; set; }

        /// <summary>
        /// 昵称
        /// </summary>
        public string? NickName { get; set; }
    }
}