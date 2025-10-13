namespace HelpMeChat
{
    /// <summary>
    /// 用户记忆管理类
    /// </summary>
    public class Memories
    {
        /// <summary>
        /// 用户记忆列表
        /// </summary>
        public List<UserMemory> UserMemories { get; set; } = new List<UserMemory>();

        /// <summary>
        /// 用户自定义提示词列表
        /// </summary>
        public List<string> CustomPrompts { get; set; } = new List<string>();
    }
}