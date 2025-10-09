using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using System;
using System.Linq;
using System.Timers;
using FlaUI.Core.Input;
using FlaUI.Core.Definitions;
using System.Windows.Forms;
using FlaUI.Core.WindowsAPI;

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
        public event Action<double, double>? ShowPopup;

        /// <summary>
        /// 隐藏弹出窗口事件
        /// </summary>
        public event Action? HidePopup;

        /// <summary>
        /// 诗句选择事件
        /// </summary>
        public event Action<string>? PoemSelected;

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
        /// 选择诗句
        /// </summary>
        /// <param name="poem">诗句</param>
        public void SelectPoem(string poem)
        {
            OnPoemSelected(poem);
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
                var wechatWindow = automation.GetDesktop().FindFirstDescendant(cf => cf.ByName("微信").And(cf.ByControlType(FlaUI.Core.Definitions.ControlType.Window)));
                if (wechatWindow == null) return;
                var editElements = wechatWindow.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Edit));
                editElement = editElements.FirstOrDefault(el => el.Properties.HasKeyboardFocus.Value);
                if (editElement == null) return;
                var value = editElement.AsTextBox().Text;
                if (value.EndsWith(">>") && !value.Equals(lastValue))
                {
                    ShowPopup?.Invoke(editElement.BoundingRectangle.Left, editElement.BoundingRectangle.Top - 200);
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
        /// 诗句选择事件
        /// </summary>
        /// <param name="poem">选择的诗句</param>
        private void OnPoemSelected(string poem)
        {
            if (editElement != null)
            {
                var currentValue = editElement.AsTextBox().Text;
                if (currentValue.EndsWith(">>"))
                {
                    // 点击输入框
                    editElement.Click();
                    // 设置剪贴板
                    Clipboard.SetText(poem);
                    // 删除 >>
                    Keyboard.Type(VirtualKeyShort.BACK);
                    Keyboard.Type(VirtualKeyShort.BACK);
                    // 模拟 Ctrl+V 粘贴
                    Keyboard.TypeSimultaneously(new[] { VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_V });
                }
            }
            PoemSelected?.Invoke(poem);
        }
    }
}