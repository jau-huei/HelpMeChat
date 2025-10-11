using System.Collections.Generic;

namespace HelpMeChat
{
    /// <summary>
    /// 应用程序配置类
    /// </summary>
    public class AppConfig
    {
        /// <summary>
        /// 微信用户名，用于告诉 AI 我是谁
        /// </summary>
        public string? WeChatUserName { get; set; }

        /// <summary>
        /// Ollama 服务 IP 地址
        /// </summary>
        public string? OllamaIp { get; set; }

        /// <summary>
        /// Ollama 服务端口号
        /// </summary>
        public int OllamaPort { get; set; } = 11434;

        /// <summary>
        /// 模型名称
        /// </summary>
        public string? Model { get; set; }

        /// <summary>
        /// 微信数据库路径
        /// </summary>
        public string? WeChatDbPath { get; set; }

        /// <summary>
        /// 微信ID
        /// </summary>
        public string? WeChatId { get; set; }

        /// <summary>
        /// 预设回复字典：Key 为显示名称，Value 为实际回复内容
        /// </summary>
        public Dictionary<string, string>? PresetReplies { get; set; }

        /// <summary>
        /// AI 设定列表
        /// </summary>
        public List<AiConfig>? AiConfigs { get; set; }

        /// <summary>
        /// 用户记忆列表
        /// </summary>
        public List<UserMemory>? UserMemories { get; set; }

        // 其他配置属性可以后续添加
    }
}