using System;

namespace HelpMeChat
{
    /// <summary>
    /// AI 设定类，用于存储和管理 AI 相关的配置信息。
    /// </summary>
    public class AiConfig
    {
        /// <summary>
        /// 设定名，用于标识不同的 AI 设定。
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// 是否为共用设定，表示该设定是否可以被多个用户或会话共享。
        /// </summary>
        public bool IsShared { get; set; }

        /// <summary>
        /// 提示词，用于指导 AI 生成响应的初始文本。
        /// </summary>
        public string? Prompt { get; set; }
    }
}