using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using System;
using System.Linq;
using System.Timers;
using FlaUI.Core.Input;
using FlaUI.Core.Definitions;
using System.Windows.Forms;
using FlaUI.Core.WindowsAPI;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HelpMeChat
{
    /// <summary>
    /// UI 自动化监控类
    /// </summary>
    public class UIAutomationMonitor
    {
        /// <summary>
        /// 自动化实例
        /// </summary>
        public UIA3Automation Automation { get; }

        /// <summary>
        /// 定时器
        /// </summary>
        public System.Timers.Timer Timer { get; }

        /// <summary>
        /// 最后输入时间
        /// </summary>
        public DateTime LastInputTime { get; set; } = DateTime.MinValue;

        /// <summary>
        /// 弹窗是否可见
        /// </summary>
        public bool PopupVisible { get; set; } = false;

        /// <summary>
        /// 正常轮询间隔
        /// </summary>
        public int NormalInterval { get; set; } = 1000;

        /// <summary>
        /// 快速检测间隔
        /// </summary>
        public int FastInterval { get; set; } = 300;

        /// <summary>
        /// 编辑元素
        /// </summary>
        public AutomationElement? EditElement { get; set; }

        /// <summary>
        /// 最后的值
        /// </summary>
        public string LastValue { get; set; } = "";

        /// <summary>
        /// 显示弹出窗口事件
        /// </summary>
        public event Action<double, double, List<(string, string)>>? ShowPopup;

        /// <summary>
        /// 隐藏弹出窗口事件
        /// </summary>
        public event Action? HidePopup;

        /// <summary>
        /// 构造函数
        /// </summary>
        public UIAutomationMonitor()
        {
            Automation = new UIA3Automation();
            Timer = new System.Timers.Timer(NormalInterval);
            Timer.Elapsed += OnTimerElapsed;
            Timer.Start();
        }

        /// <summary>
        /// 获取聊天历史
        /// </summary>
        /// <param name="wechatWindow">微信窗口元素</param>
        /// <returns>聊天历史列表，元组为 (发送者, 消息内容)</returns>
        private List<(string, string)> GetChatHistory(AutomationElement wechatWindow)
        {
            var messageList = wechatWindow.FindFirstDescendant(cf => cf.ByName("消息").And(cf.ByControlType(ControlType.List)));
            if (messageList == null) return new List<(string, string)>();
            var listItems = messageList.FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem))
                .OrderBy(item => item.BoundingRectangle.Top)
                .ToList();
            var history = new List<(string, string)>();
            foreach (var item in listItems)
            {
                var name = item.Properties.Name.Value ?? "";
                if (string.IsNullOrEmpty(name)) continue;
                // 过滤日期
                if (Regex.IsMatch(name, @"\d{4}年\d{1,2}月\d{1,2}日 \d{1,2}:\d{2}")) continue;
                // 根据子元素中的按钮名称决定发送者
                var button = item.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button));
                string sender = "未知";
                if (button != null)
                {
                    sender = button.Properties.Name.Value ?? "未知";
                }
                history.Add((sender, name));
            }
            return history;
        }

        /// <summary>
        /// 定时器事件
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                var wechatWindow = Automation.GetDesktop().FindFirstDescendant(cf => cf.ByName("微信").And(cf.ByControlType(ControlType.Window)));
                if (wechatWindow == null) return;
                var editElements = wechatWindow.FindAllDescendants(cf => cf.ByControlType(ControlType.Edit));
                EditElement = editElements.FirstOrDefault(el => el.Properties.HasKeyboardFocus.Value) ?? EditElement;
                if (EditElement == null) return;

                var value = EditElement.AsTextBox().Text;
                bool inputChanged = !value.Equals(LastValue);
                LastValue = value;

                // 检测输入变化，动态调整轮询间隔
                if (inputChanged)
                {
                    LastInputTime = DateTime.Now;
                    // 输入时，拉长轮询间隔，减少卡顿
                    Timer.Interval = NormalInterval;
                }

                // 检测弹窗触发条件
                if (value.EndsWith(">>") && inputChanged)
                {
                    var history = GetChatHistory(wechatWindow);
                    ShowPopup?.Invoke(EditElement.BoundingRectangle.Left, EditElement.BoundingRectangle.Top, history);
                    PopupVisible = true;
                    // 弹窗后，短暂加快轮询，提升响应
                    Timer.Interval = FastInterval;
                }
                else if (!value.EndsWith(">>") && PopupVisible)
                {
                    HidePopup?.Invoke();
                    PopupVisible = false;
                    Timer.Interval = NormalInterval;
                }

                // 如果弹窗已显示，且 1 秒内无输入变化，恢复正常轮询
                if (PopupVisible && (DateTime.Now - LastInputTime).TotalMilliseconds > 1000)
                {
                    Timer.Interval = NormalInterval;
                }
            }
            catch
            {
                // Ignore errors
            }
        }

        /// <summary>
        /// 回复选择事件
        /// </summary>
        /// <param name="reply">选择的回复</param>
        public void OnReplySelected(string reply)
        {
            if (EditElement != null)
            {
                var currentValue = EditElement.AsTextBox().Text;
                if (currentValue.EndsWith(">>"))
                {
                    // 点击输入框
                    EditElement.Click();
                    // 设置剪贴板
                    Clipboard.SetText(reply);
                    // 删除 >>
                    Keyboard.Type(VirtualKeyShort.END);
                    Keyboard.Type(VirtualKeyShort.BACK);
                    Keyboard.Type(VirtualKeyShort.BACK);
                    // 模拟 Ctrl+V 粘贴
                    Keyboard.TypeSimultaneously(new[] { VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_V });
                }
            }
        }
    }
}