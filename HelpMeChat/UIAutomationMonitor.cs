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
        private readonly UIA3Automation automation;

        /// <summary>
        /// 定时器
        /// </summary>
        private readonly System.Timers.Timer timer;

        /// <summary>
        /// 编辑元素
        /// </summary>
        private AutomationElement? editElement;

        /// <summary>
        /// 最后的值
        /// </summary>
        private string lastValue = "";

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
            automation = new UIA3Automation();
            timer = new System.Timers.Timer(1500);
            timer.Elapsed += OnTimerElapsed;
            timer.Start();
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
                var wechatWindow = automation.GetDesktop().FindFirstDescendant(cf => cf.ByName("微信").And(cf.ByControlType(ControlType.Window)));
                if (wechatWindow == null) return;
                var editElements = wechatWindow.FindAllDescendants(cf => cf.ByControlType(ControlType.Edit));
                editElement = editElements.FirstOrDefault(el => el.Properties.HasKeyboardFocus.Value) ?? editElement;
                if (editElement == null) return;

                var value = editElement.AsTextBox().Text;
                if (value.EndsWith(">>") && !value.Equals(lastValue))
                {
                    // 捕捉历史对话信息
                    var history = GetChatHistory(wechatWindow);
                    ShowPopup?.Invoke(editElement.BoundingRectangle.Left, editElement.BoundingRectangle.Top, history);
                }
                else if (!value.EndsWith(">>"))
                {
                    HidePopup?.Invoke();
                }
                lastValue = value;
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
            if (editElement != null)
            {
                var currentValue = editElement.AsTextBox().Text;
                if (currentValue.EndsWith(">>"))
                {
                    // 点击输入框
                    editElement.Click();
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