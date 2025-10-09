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
        public Dictionary<string, string> PresetReplies { get; set; } = new Dictionary<string, string>();

        // 其他配置属性可以后续添加
    }
}