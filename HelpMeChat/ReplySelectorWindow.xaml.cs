using Ollama;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Message = Ollama.Message;

namespace HelpMeChat
{
    /// <summary>
    /// 回复选择窗口类
    /// </summary>
    public partial class ReplySelectorWindow : Window
    {
        /// <summary>
        /// 回复选择事件
        /// </summary>
        public event Action<string>? ReplySelected;

        /// <summary>
        /// AI 回复选择事件
        /// </summary>
        public event Action<string>? AiReplySelected;

        /// <summary>
        /// 关闭时保存事件
        /// </summary>
        public event Action? OnCloseSave;

        /// <summary>
        /// 应用程序配置
        /// </summary>
        private AppConfig? Config { get; set; }

        /// <summary>
        /// 当前对话用户名称
        /// </summary>
        private string? CurrentUserName { get; set; }

        /// <summary>
        /// 聊天历史
        /// </summary>
        private List<ChatMessage>? ChatHistory { get; set; }

        /// <summary>
        /// Ollama API 客户端实例
        /// </summary>
        private OllamaApiClient? Client { get; set; }

        /// <summary>
        /// 当前对话循环取消令牌源
        /// </summary>
        private CancellationTokenSource? Cts { get; set; }

        /// <summary>
        /// 当前用户的记忆
        /// </summary>
        private UserMemory? CurrentMemory { get; set; }

        /// <summary>
        /// 当前选择的提示词
        /// </summary>
        private string SelectedPrompt { get; set; } = string.Empty;

        /// <summary>
        /// AI 回复内容
        /// </summary>
        private string AiResponse { get; set; } = string.Empty;

        /// <summary>
        /// 是否正在生成 AI 回复
        /// </summary>
        private bool IsGenerating { get; set; } = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="config">应用程序配置</param>
        /// <param name="currentUserName">当前对话用户名称</param>
        /// <param name="history">聊天历史</param>
        public ReplySelectorWindow(AppConfig config, string currentUserName, List<ChatMessage> history)
        {
            InitializeComponent();
            Config = config;
            CurrentUserName = currentUserName;
            ChatHistory = history;

            CurrentMemory = Config?.UserMemories?.FirstOrDefault(m => m.UserName == CurrentUserName);
            if (CurrentMemory == null)
            {
                CurrentMemory = new UserMemory { UserName = CurrentUserName };
                Config?.UserMemories?.Add(CurrentMemory);
            }

            ReplyComboBox.ItemsSource = Config?.PresetReplies?.Keys;
            if (Config?.PresetReplies != null && Config.PresetReplies.Count > 0)
            {
                if (!string.IsNullOrEmpty(CurrentMemory.LastPresetReply) && Config.PresetReplies.ContainsKey(CurrentMemory.LastPresetReply))
                {
                    ReplyComboBox.SelectedItem = CurrentMemory.LastPresetReply;
                }
                else
                {
                    ReplyComboBox.SelectedIndex = 0;
                }
                if (ReplyComboBox.SelectedItem is string selectedKey && Config.PresetReplies.TryGetValue(selectedKey, out string? value))
                {
                    ValueTextBlock.Text = value;
                }
            }

            // 设置提示词 ComboBox
            var availablePrompts = Config?.AiConfigs?.Where(c => c.IsShared || c.Name == CurrentUserName).Select(c => c.Name).Distinct().ToList();
            PromptComboBox.ItemsSource = availablePrompts;
            if (availablePrompts != null && availablePrompts.Count > 0)
            {
                if (!string.IsNullOrEmpty(CurrentMemory.LastAiConfig) && availablePrompts.Contains(CurrentMemory.LastAiConfig))
                {
                    PromptComboBox.SelectedItem = CurrentMemory.LastAiConfig;
                }
                else
                {
                    PromptComboBox.SelectedIndex = 0;
                }
                SelectedPrompt = PromptComboBox.SelectedItem as string ?? string.Empty;
            }

            // 订阅窗口关闭事件，确保配置被保存
            this.Closed += ReplySelectorWindow_Closed;
        }

        /// <summary>
        /// ComboBox选择改变事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">选择改变事件参数</param>
        private void ReplyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ReplyComboBox.SelectedItem is string selectedKey && Config?.PresetReplies != null && Config.PresetReplies.TryGetValue(selectedKey, out string? value))
            {
                ValueTextBlock.Text = value;
            }
            else
            {
                ValueTextBlock.Text = "";
            }
            // 更新记忆
            if (CurrentMemory != null && ReplyComboBox.SelectedItem is string selectedKeyMem)
            {
                CurrentMemory.LastPresetReply = selectedKeyMem;
            }
        }

        /// <summary>
        /// Prompt ComboBox 选择改变事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">选择改变事件参数</param>
        private void PromptComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PromptComboBox.SelectedItem is string promptName)
            {
                SelectedPrompt = promptName;
            }
            // 更新记忆
            if (CurrentMemory != null && PromptComboBox.SelectedItem is string selectedPrompt)
            {
                CurrentMemory.LastAiConfig = selectedPrompt;
            }
        }

        /// <summary>
        /// 确定按钮点击事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">路由事件参数</param>
        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (ReplyComboBox.SelectedItem is string selectedKey && Config?.PresetReplies != null && Config.PresetReplies.TryGetValue(selectedKey, out string? value) && value != null)
            {
                ReplySelected?.Invoke(value);
                Close();
            }
        }

        /// <summary>
        /// 生成 AI 回复按钮点击事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">路由事件参数</param>
        private async void GenerateAiButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SelectedPrompt) || Config?.AiConfigs == null) return;
            var aiConfig = Config.AiConfigs.FirstOrDefault(c => c.Name == SelectedPrompt);
            if (aiConfig == null) return;

            IsGenerating = true;
            GenerateAiButton.IsEnabled = false;
            CancelAiButton.IsEnabled = true;
            ConfirmAiButton.IsEnabled = false;
            AiResponseTextBox.IsReadOnly = true;
            AiResponseTextBox.Text = "正在生成...";

            Cts = new CancellationTokenSource();
            try
            {
                Client ??= new OllamaApiClient(baseUri: new Uri(Config.OllamaService!));
                var messages = new List<Message>();

                // 添加系统提示
                messages.Add(new Message(MessageRole.System, aiConfig.Prompt ?? string.Empty, null, null));

                // 添加历史对话
                messages.Add(new Message(MessageRole.User, string.Join("\n", (ChatHistory ?? new List<ChatMessage>()).Select(h => $"{h.Sender}:{h.Message}")), null, null));

                var stream = Client.Chat.GenerateChatCompletionAsync(Config?.Model ?? string.Empty, messages, stream: true, cancellationToken: Cts.Token);

                // 在后台线程处理流，使用同步 Invoke 将每个 chunk 追加到 UI，确保 UI 有机会渲染
                await Task.Run(async () =>
                {
                    await foreach (GenerateChatCompletionResponse resp in stream.WithCancellation(Cts.Token))
                    {
                        var chunk = resp.Message.Content;
                        if (string.IsNullOrEmpty(chunk)) continue;
                        if (chunk == ">>") continue;

                        // 同步在 UI 线程执行追加，这样文本更新会立即被应用到可视树
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (AiResponseTextBox.Text == "正在生成...") AiResponseTextBox.Text = "";
                            AiResponseTextBox.Text += chunk;
                        });
                    }
                }, Cts.Token);
                AiResponse = AiResponseTextBox.Text;
                ConfirmAiButton.IsEnabled = true;
            }
            catch (OperationCanceledException)
            {
                AiResponseTextBox.Text = "已取消";
            }
            catch (Exception ex)
            {
                AiResponseTextBox.Text = $"错误: {ex.Message}";
            }
            finally
            {
                IsGenerating = false;
                GenerateAiButton.IsEnabled = true;
                CancelAiButton.IsEnabled = false;
                AiResponseTextBox.IsReadOnly = false;
            }
        }

        /// <summary>
        /// 取消 AI 生成
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">路由事件参数</param>
        private void CancelAiButton_Click(object sender, RoutedEventArgs e)
        {
            Cts?.Cancel();
        }

        /// <summary>
        /// 确认 AI 回复
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">路由事件参数</param>
        private void ConfirmAiButton_Click(object sender, RoutedEventArgs e)
        {
            string finalResponse = AiResponseTextBox.Text;
            if (!string.IsNullOrEmpty(finalResponse))
            {
                AiReplySelected?.Invoke(finalResponse);
                Close();
            }
        }

        /// <summary>
        /// 打字效果方法已移除；现在直接将 chunk 追加到文本块以实时显示。

        /// <summary>
        /// 窗口鼠标左键按下事件，用于拖动窗口
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">鼠标按钮事件参数</param>
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">路由事件参数</param>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            OnCloseSave?.Invoke();
            Cts?.Cancel();
            this.Close();
        }

        /// <summary>
        /// 窗口关闭事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void ReplySelectorWindow_Closed(object? sender, EventArgs e)
        {
            OnCloseSave?.Invoke();
            Cts?.Cancel();
        }

        /// <summary>
        /// TabControl 选择改变事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">选择改变事件参数</param>
        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CurrentMemory != null && sender is TabControl tabControl)
            {
                CurrentMemory.LastTab = tabControl.SelectedIndex == 1 ? "AI" : "Preset";
            }
        }

        /// <summary>
        /// TabControl 加载完成事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">路由事件参数</param>
        private void MainTabControl_Loaded(object sender, RoutedEventArgs e)
        {
            // 根据记忆设置标签页
            if (CurrentMemory != null && CurrentMemory.LastTab == "AI")
            {
                MainTabControl.SelectedIndex = 1;
            }
            else
            {
                MainTabControl.SelectedIndex = 0;
            }
        }
    }
}