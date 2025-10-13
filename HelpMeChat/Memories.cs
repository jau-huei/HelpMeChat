using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

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
    }
}