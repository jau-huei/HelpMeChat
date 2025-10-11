using ProtoBuf;
using System.Collections.Generic;

namespace HelpMeChat.WeChatTool
{
    /// <summary>
    /// 表示类型-值对的类
    /// </summary>
    [ProtoContract]
    public class TVType
    {
        /// <summary>
        /// 类型标识
        /// </summary>
        [ProtoMember(1)]
        public int Type { get; set; }

        /// <summary>
        /// 类型对应的值
        /// </summary>
        [ProtoMember(2)]
        public string TypeValue { get; set; } = "";
    }

    /// <summary>
    /// 表示协议消息的类
    /// </summary>
    [ProtoContract]
    public class ProtoMsg
    {
        /// <summary>
        /// 类型-值消息列表
        /// </summary>
        [ProtoMember(3)]
        public List<TVType>? TVMsg { get; set; }
    }
}