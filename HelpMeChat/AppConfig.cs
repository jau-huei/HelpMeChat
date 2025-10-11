using System;
using System.Collections.Generic;

namespace HelpMeChat
{
    /// <summary>
    /// 应用程序配置类
    /// </summary>
    public class AppConfig
    {
        /// <summary>
        /// 静态共用配置实例
        /// </summary>
        public static AppConfig? Config { get; set; }

        /// <summary>
        /// 微信用户名，用于告诉 AI 我是谁
        /// </summary>
        public string? WeChatUserName { get; set; }

        /// <summary>
        /// Ollama 服务地址
        /// </summary>
        public string? OllamaService { get; set; } = "http://127.0.0.1:11434/api";

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
        /// AI 上下文消息数量：从数据库中捞多少数据过来喂给 AI 生成对话
        /// </summary>
        public int AiContextMessageCount { get; set; } = 100;

        /// <summary>
        /// 一般提示词，用于指导 AI 了解对话记录格式和回复格式
        /// </summary>
        public string? Prompt { get; set; }

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