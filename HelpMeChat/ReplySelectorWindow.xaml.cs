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
        /// AI 配置列表
        /// </summary>
        private List<AiConfig>? AiConfigs { get; set; }

        /// <summary>
        /// 当前对话用户名称
        /// </summary>
        private string? CurrentUserName { get; set; }

        /// <summary>
        /// 聊天历史
        /// </summary>
        private List<(string, string)>? ChatHistory { get; set; }

        /// <summary>
        /// Ollama IP 地址
        /// </summary>
        private string? OllamaIp { get; set; }

        /// <summary>
        /// Ollama 端口号
        /// </summary>
        private string? OllamaPort { get; set; }

        /// <summary>
        /// 模型名称
        /// </summary>
        private string? Model { get; set; }

        /// <summary>
        /// Ollama API 客户端实例
        /// </summary>
        private OllamaApiClient? Client { get; set; }

        /// <summary>
        /// 当前对话循环取消令牌源
        /// </summary>
        private CancellationTokenSource? Cts { get; set; }

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
        /// <param name="presetReplies">预设回复字典</param>
        /// <param name="aiConfigs">AI 配置列表</param>
        /// <param name="currentUserName">当前对话用户名称</param>
        /// <param name="ollamaIp">Ollama IP</param>
        /// <param name="ollamaPort">Ollama 端口</param>
        /// <param name="model">模型名称</param>
        /// <param name="history">聊天历史</param>
        public ReplySelectorWindow(Dictionary<string, string> presetReplies, List<AiConfig> aiConfigs, string currentUserName, string ollamaIp, string ollamaPort, string model, List<(string, string)> history)
        {
            InitializeComponent();
            PresetReplies = presetReplies;
            AiConfigs = aiConfigs;
            CurrentUserName = currentUserName;
            OllamaIp = ollamaIp;
            OllamaPort = ollamaPort;
            Model = model;
            ChatHistory = history;

            ReplyComboBox.ItemsSource = presetReplies.Keys;
            if (presetReplies.Count > 0)
            {
                ReplyComboBox.SelectedIndex = 0;
                if (ReplyComboBox.SelectedItem is string selectedKey && presetReplies.TryGetValue(selectedKey, out string? value))
                {
                    ValueTextBlock.Text = value;
                }
            }

            // 设置提示词 ComboBox
            var availablePrompts = AiConfigs.Where(c => c.IsShared || c.Name == CurrentUserName).Select(c => c.Name).Distinct().ToList();
            PromptComboBox.ItemsSource = availablePrompts;
            if (availablePrompts.Count > 0)
            {
                PromptComboBox.SelectedIndex = 0;
                SelectedPrompt = availablePrompts[0] ?? string.Empty;
            }
        }

        /// <summary>
        /// 预设回复字典
        /// </summary>
        private Dictionary<string, string>? PresetReplies { get; set; }

        /// <summary>
        /// ComboBox选择改变事件
        /// </summary>
        private void ReplyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ReplyComboBox.SelectedItem is string selectedKey && PresetReplies != null && PresetReplies.TryGetValue(selectedKey, out string? value))
            {
                ValueTextBlock.Text = value;
            }
            else
            {
                ValueTextBlock.Text = "";
            }
        }

        /// <summary>
        /// Prompt ComboBox 选择改变事件
        /// </summary>
        private void PromptComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PromptComboBox.SelectedItem is string promptName)
            {
                SelectedPrompt = promptName;
            }
        }

        /// <summary>
        /// 确定按钮点击事件
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (ReplyComboBox.SelectedItem is string selectedKey && PresetReplies != null && PresetReplies.TryGetValue(selectedKey, out string? value) && value != null)
            {
                ReplySelected?.Invoke(value);
                Close();
            }
        }

        /// <summary>
        /// 生成 AI 回复按钮点击事件
        /// </summary>
        private async void GenerateAiButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SelectedPrompt) || AiConfigs == null) return;
            var config = AiConfigs.FirstOrDefault(c => c.Name == SelectedPrompt);
            if (config == null) return;

            IsGenerating = true;
            GenerateAiButton.IsEnabled = false;
            CancelAiButton.IsEnabled = true;
            RegenerateAiButton.IsEnabled = false;
            ConfirmAiButton.IsEnabled = false;
            AiResponseTextBlock.Text = "正在生成...";

            Cts = new CancellationTokenSource();
            try
            {
                Client ??= new OllamaApiClient();
                var messages = new List<Message>();

                // 添加系统提示
                messages.Add(new Message(MessageRole.System, config.Prompt ?? string.Empty, null, null));

                // 添加历史对话
                messages.Add(new Message(MessageRole.User, string.Join("\n", (ChatHistory ?? new List<(string, string)>()).Select(h => $"{h.Item1}:{h.Item2}")), null, null));

                var stream = Client.Chat.GenerateChatCompletionAsync(Model ?? string.Empty, messages, stream: true, cancellationToken: Cts.Token);

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
                            if (AiResponseTextBlock.Text == "正在生成...") AiResponseTextBlock.Text = "";
                            AiResponseTextBlock.Text += chunk;
                        });
                    }
                }, Cts.Token);
                AiResponse = AiResponseTextBlock.Text;
                ConfirmAiButton.IsEnabled = true;
                RegenerateAiButton.IsEnabled = true;
            }
            catch (OperationCanceledException)
            {
                AiResponseTextBlock.Text = "已取消";
            }
            catch (Exception ex)
            {
                AiResponseTextBlock.Text = $"错误: {ex.Message}";
            }
            finally
            {
                IsGenerating = false;
                GenerateAiButton.IsEnabled = true;
                CancelAiButton.IsEnabled = false;
            }
        }

        /// <summary>
        /// 取消 AI 生成
        /// </summary>
        private void CancelAiButton_Click(object sender, RoutedEventArgs e)
        {
            Cts?.Cancel();
        }

        /// <summary>
        /// 重新生成 AI 回复
        /// </summary>
        private void RegenerateAiButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(SelectedPrompt))
            {
                GenerateAiButton_Click(sender, e);
            }
        }

        /// <summary>
        /// 确认 AI 回复
        /// </summary>
        private void ConfirmAiButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(AiResponse))
            {
                AiReplySelected?.Invoke(AiResponse);
                Close();
            }
        }

        /// <summary>
        /// 打字效果方法已移除；现在直接将 chunk 追加到文本块以实时显示。

        /// <summary>
        /// 窗口鼠标左键按下事件，用于拖动窗口
        /// </summary>
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Cts?.Cancel();
            this.Close();
        }
    }
}