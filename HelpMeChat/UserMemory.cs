namespace HelpMeChat
{
    /// <summary>
    /// 用户记忆类
    /// </summary>
    public class UserMemory
    {
        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 最后选择的预设回复
        /// </summary>
        public string LastPresetReply { get; set; } = string.Empty;

        /// <summary>
        /// 最后选择的 AI 配置
        /// </summary>
        public string LastAiConfig { get; set; } = string.Empty;

        /// <summary>
        /// 最后使用的 Tab
        /// </summary>
        public string LastTab { get; set; } = string.Empty;
    }
}