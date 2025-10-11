using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using System;
using System.Linq;
using FlaUI.Core.Definitions;
using System.Windows.Forms;
using FlaUI.Core.WindowsAPI;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FlaUI.Core;
using FlaUI.Core.Input;
using System.Threading;
using HelpMeChat.WeChatTool;

namespace HelpMeChat
{
    /// <summary>
    /// UI 自动化监控类 (事件驱动 + 回退轮询)
    /// </summary>
    public class UIAutomationMonitor : IDisposable
    {
        /// <summary>
        /// 获取 UIA3 自动化实例，用于执行 UI 自动化操作。
        /// </summary>
        public UIA3Automation Automation { get; }

        /// <summary>
        /// 获取或设置最后一次输入的时间戳，用于跟踪用户活动。
        /// </summary>
        public DateTime LastInputTime { get; set; } = DateTime.MinValue;

        /// <summary>
        /// 获取或设置弹出窗口是否可见的标志。
        /// </summary>
        public bool PopupVisible { get; set; } = false;

        /// <summary>
        /// 获取或设置当前聚焦的编辑元素，用于监控文本输入。
        /// </summary>
        public AutomationElement? EditElement { get; set; }

        /// <summary>
        /// 获取或设置编辑元素的最后文本值，用于检测文本变化。
        /// </summary>
        public string LastValue { get; set; } = string.Empty;

        /// <summary>
        /// 当需要显示弹出窗口时触发的事件，传递位置坐标和聊天历史列表。
        /// </summary>
        public event Action<ShowPopupArgs>? ShowPopup;

        /// <summary>
        /// 当需要隐藏弹出窗口时触发的事件。
        /// </summary>
        public event Action? HidePopup;

        /// <summary>
        /// 获取或设置焦点变化事件处理器，用于监听 UI 焦点变化。
        /// </summary>
        private IDisposable? FocusChangedHandler { get; set; }

        /// <summary>
        /// 获取或设置值变化事件处理器，用于监听编辑元素的 Value 和 Name 属性变化。
        /// </summary>
        private IDisposable? ValueChangedHandler { get; set; }

        /// <summary>
        /// 获取或设置回退轮询的取消令牌源，用于控制轮询任务的生命周期。
        /// </summary>
        private CancellationTokenSource? PollCts { get; set; }

        /// <summary>
        /// 获取或设置应用程序配置，用于获取默认设置。
        /// </summary>
        public AppConfig? Config { get; set; }

        /// <summary>
        /// 微信数据库实际密码
        /// </summary>
        public string? WeChatActualKey { get; set; }

        /// <summary>
        /// 微信ID
        /// </summary>
        public string? WeChatId { get; set; }

        /// <summary>
        /// 微信数据库根路径
        /// </summary>
        public string? WeChatDbPath { get; set; }

        /// <summary>
        /// 获取同步锁对象，用于线程安全的操作。
        /// </summary>
        private readonly object Sync = new();

        /// <summary>
        /// 轮询间隔（只有事件不生效时才会使用），单位为毫秒。
        /// </summary>
        private const int FallbackPollIntervalMs = 180;

        /// <summary>
        /// 初始化 UIAutomationMonitor 实例，创建自动化对象并注册焦点变化事件。
        /// </summary>
        public UIAutomationMonitor()
        {
            Automation = new UIA3Automation();
            RegisterFocusChanged();
            TryAttachToFocusedEdit(Automation.FocusedElement());
        }

        /// <summary>
        /// 注册焦点变化事件处理器。
        /// </summary>
        private void RegisterFocusChanged()
        {
            FocusChangedHandler = Automation.RegisterFocusChangedEvent(element =>
            {
                if (element != null)
                {
                    OnFocusChanged(element);
                }
            });
        }

        /// <summary>
        /// 处理焦点变化事件，尝试附加到聚焦的编辑元素。
        /// </summary>
        /// <param name="element">聚焦的自动化元素。</param>
        private void OnFocusChanged(AutomationElement element)
        {
            TryAttachToFocusedEdit(element);
        }

        /// <summary>
        /// 尝试附加到聚焦的编辑元素，如果它是微信窗口内的编辑控件。
        /// </summary>
        /// <param name="focused">聚焦的自动化元素。</param>
        private void TryAttachToFocusedEdit(AutomationElement? focused)
        {
            if (focused == null) return;
            try
            {
                if (focused.ControlType != ControlType.Edit) return;
                var wechatWindow = GetWeChatWindowFromElement(focused);
                if (wechatWindow == null) return;

                if (!Equals(EditElement, focused))
                {
                    EditElement = focused;
                    LastValue = GetElementTextSafe(EditElement);
                    RegisterTextChangeHandlers(EditElement);
                }
            }
            catch { }
        }

        /// <summary>
        /// 从给定的元素向上查找微信窗口。
        /// </summary>
        /// <param name="element">起始自动化元素。</param>
        /// <returns>微信窗口元素，如果未找到则返回 null。</returns>
        private AutomationElement? GetWeChatWindowFromElement(AutomationElement element)
        {
            try
            {
                var current = element;
                while (current != null)
                {
                    if (current.ControlType == ControlType.Window && (current.Properties.Name.Value ?? string.Empty) == "微信")
                    {
                        return current;
                    }
                    current = current.Parent;
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// 安全地获取元素的文本值，支持多种模式。
        /// </summary>
        /// <param name="element">要获取文本的自动化元素。</param>
        /// <returns>元素的文本值，如果失败则返回空字符串。</returns>
        private string GetElementTextSafe(AutomationElement element)
        {
            try
            {
                if (element.Patterns.Value.IsSupported)
                {
                    return element.Patterns.Value.Pattern.Value ?? string.Empty;
                }
                if (element.Patterns.Text.IsSupported)
                {
                    return element.Patterns.Text.Pattern.DocumentRange.GetText(int.MaxValue) ?? string.Empty;
                }
                return element.AsTextBox().Text;
            }
            catch { return string.Empty; }
        }

        /// <summary>
        /// 仅注册属性变化事件 + 启动回退轮询
        /// </summary>
        private void RegisterTextChangeHandlers(AutomationElement edit)
        {
            lock (Sync)
            {
                ValueChangedHandler?.Dispose();
                ValueChangedHandler = null;
                PollCts?.Cancel();
                PollCts = null;

                // 1. 属性事件 (Value/Name)
                try
                {
                    var props = new[]
                    {
                        edit.Automation.PropertyLibrary.Value.Value,
                        edit.Automation.PropertyLibrary.Element.Name
                    };
                    ValueChangedHandler = edit.RegisterPropertyChangedEvent(TreeScope.Element, (sender, property, newValue) =>
                    {
                        OnAnyTextMaybeChanged();
                    }, props);
                }
                catch { }

                // 2. 启动回退轮询：某些自绘输入框不触发上面事件
                PollCts = new CancellationTokenSource();
                _ = StartFallbackPollingAsync(edit, PollCts.Token);
            }
        }

        /// <summary>
        /// 异步启动回退轮询任务，用于定期检查文本变化。
        /// </summary>
        /// <param name="edit">要轮询的编辑元素。</param>
        /// <param name="token">取消令牌，用于停止轮询。</param>
        private async Task StartFallbackPollingAsync(AutomationElement edit, CancellationToken token)
        {
            // 小延迟，避免刚聚焦时频繁读取
            try { await Task.Delay(150, token); } catch { return; }
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (!Equals(EditElement, edit)) break; // 焦点已变
                    OnAnyTextMaybeChanged();
                }
                catch { }
                try { await Task.Delay(FallbackPollIntervalMs, token); } catch { break; }
            }
        }

        /// <summary>
        /// 处理可能的文本变化，更新最后值并评估弹出逻辑。
        /// </summary>
        private void OnAnyTextMaybeChanged()
        {
            try
            {
                if (EditElement == null) return;
                var value = GetElementTextSafe(EditElement);
                if (value == LastValue) return;
                LastInputTime = DateTime.Now;
                LastValue = value;
                EvaluatePopupLogic(value);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 根据文本值评估是否显示或隐藏弹出窗口。
        /// </summary>
        /// <param name="value">当前文本值。</param>
        private void EvaluatePopupLogic(string value)
        {
            if (EditElement == null) return;
            var wechatWindow = GetWeChatWindowFromElement(EditElement);
            if (wechatWindow == null) return;

            if (value.EndsWith(">>") && !PopupVisible)
            {
                using var wechatDb = WeChatDBHelper.DecryptWeChatDatabases(Config!, WeChatActualKey!);
                var strNickName = EditElement.Properties.Name.Value ?? string.Empty;
                var history = GetChatHistory(wechatWindow, wechatDb, strNickName);
                var weChatUserName = Config?.WeChatUserName ?? string.Empty;
                history.Add(new ChatMessage(weChatUserName, value));

                ShowPopup?.Invoke(new ShowPopupArgs
                {
                    Left = EditElement.BoundingRectangle.Left,
                    Top = EditElement.BoundingRectangle.Top,
                    History = history,
                    NickName = strNickName,
                    ApiConfig = Config
                });
                PopupVisible = true;
            }
            else if (!value.EndsWith(">>") && PopupVisible)
            {
                HidePopup?.Invoke();
                PopupVisible = false;
            }
        }

        // TODO
        private List<ChatMessage> GetChatHistory(AutomationElement wechatWindow, DecryptedDatabases? wechatDb, string strNickName)
        {
            if (wechatDb == null || string.IsNullOrEmpty(WeChatId) || string.IsNullOrEmpty(strNickName))
                return GetChatHistory(wechatWindow);

            var userNames = wechatDb.GetUserNamesByNickName(strNickName);
            if (userNames == null || userNames.Count != 1)
                return GetChatHistory(wechatWindow);

            var msgRecords = wechatDb.GetLatestMessagesByTalker(userNames[0], 100);

            return GetChatHistory(wechatWindow);
        }

        /// <summary>
        /// 获取微信窗口的聊天历史记录。
        /// </summary>
        /// <param name="wechatWindow">微信窗口元素。</param>
        /// <returns>聊天历史列表，每个元素包含发送者和消息。</returns>
        private List<ChatMessage> GetChatHistory(AutomationElement wechatWindow)
        {
            var messageList = wechatWindow.FindFirstDescendant(cf => cf.ByName("消息").And(cf.ByControlType(ControlType.List)));
            if (messageList == null) return new List<ChatMessage>();
            var listItems = messageList.FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem))
                .OrderBy(item => item.BoundingRectangle.Top)
                .ToList();
            var history = new List<ChatMessage>();
            foreach (var item in listItems)
            {
                var name = item.Properties.Name.Value ?? string.Empty;
                if (string.IsNullOrEmpty(name)) continue;
                if (Regex.IsMatch(name, @"\d{4}年\d{1,2}月\d{1,2}日 \d{1,2}:\d{2}")) continue;
                var button = item.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button));
                if (button == null) continue;

                string sender = button.Properties.Name.Value ?? "未知";
                history.Add(new ChatMessage(sender, name));
            }
            return history;
        }

        /// <summary>
        /// 当选择回复时，将回复文本插入到编辑元素中。
        /// </summary>
        /// <param name="reply">要插入的回复文本。</param>
        public void OnReplySelected(string reply)
        {
            if (EditElement != null)
            {
                var currentValue = GetElementTextSafe(EditElement);
                if (currentValue.EndsWith(">>"))
                {
                    EditElement.Click();
                    Clipboard.SetText(reply);
                    Keyboard.Type(VirtualKeyShort.END);
                    Keyboard.Type(VirtualKeyShort.BACK);
                    Keyboard.Type(VirtualKeyShort.BACK);
                    Keyboard.TypeSimultaneously(new[] { VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_V });
                }
            }
        }

        /// <summary>
        /// 释放资源，包括事件处理器和自动化对象。
        /// </summary>
        public void Dispose()
        {
            ValueChangedHandler?.Dispose();
            FocusChangedHandler?.Dispose();
            PollCts?.Cancel();
            Automation.Dispose();
        }
    }
}