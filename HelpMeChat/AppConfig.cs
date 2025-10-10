using System.Collections.Generic;

namespace HelpMeChat
{
    /// <summary>
    /// 应用程序配置类
    /// </summary>
    public class AppConfig
    {
        /// <summary>
        /// 预设回复字典：Key 为显示名称，Value 为实际回复内容
        /// </summary>
        public Dictionary<string, string>? PresetReplies { get; set; }

        /// <summary>
        /// 预设 Ollama 服务 IP 地址
        /// </summary>
        public string? DefaultOllamaIp { get; set; }

        /// <summary>
        /// 预设 Ollama 服务端口号
        /// </summary>
        public int DefaultOllamaPort { get; set; }

        /// <summary>
        /// 预设模型名称
        /// </summary>
        public string? DefaultModel { get; set; }

        /// <summary>
        /// AI 设定列表
        /// </summary>
        public List<AiConfig>? AiConfigs { get; set; }

        // 其他配置属性可以后续添加
    }
}