using Ollama;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using Message = Ollama.Message;
using System.IO;
using System.Text.Json;
using System.Text.Encodings.Web;

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
        /// 弹出窗口参数
        /// </summary>
        private ShowPopupArgs Args { get; set; }

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
        /// 当前历史提示索引
        /// </summary>
        private int CurrentHistoryIndex { get; set; } = -1;

        /// <summary>
        /// 保存的当前输入文本
        /// </summary>
        private string SavedCurrentInput { get; set; } = "";

        /// <summary>
        /// 用户记忆实例
        /// </summary>
        private Memories MemoriesInstance { get; set; } = new Memories();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="args">弹出窗口参数</param>
        public ReplySelectorWindow(ShowPopupArgs args)
        {
            InitializeComponent();
            Args = args;

            LoadMemories();
            if (!MemoriesInstance.UserMemories.Any(m => m.UserName == Args.NickName) && !string.IsNullOrEmpty(Args.NickName))
            {
                MemoriesInstance.UserMemories.Add(new UserMemory { UserName = Args.NickName });
            }

            ReplyComboBox.ItemsSource = AppConfig.Config?.PresetReplies?.Keys;
            if (AppConfig.Config?.PresetReplies != null && AppConfig.Config.PresetReplies.Count > 0)
            {
                var currentMemory = MemoriesInstance.UserMemories.FirstOrDefault(m => m.UserName == Args.NickName);
                if (currentMemory != null && !string.IsNullOrEmpty(currentMemory.LastPresetReply) && AppConfig.Config.PresetReplies.ContainsKey(currentMemory.LastPresetReply))
                {
                    ReplyComboBox.SelectedItem = currentMemory.LastPresetReply;
                }
                else
                {
                    ReplyComboBox.SelectedIndex = 0;
                }
                if (ReplyComboBox.SelectedItem is string selectedKey && AppConfig.Config.PresetReplies.TryGetValue(selectedKey, out string? value))
                {
                    ValueTextBlock.Text = value;
                }
            }

            // 设置提示词 ComboBox
            var availablePrompts = AppConfig.Config?.AiConfigs?.Where(c => c.IsShared || c.Name == Args.NickName).Select(c => c.Name).Distinct().ToList();
            PromptComboBox.ItemsSource = availablePrompts;
            if (availablePrompts != null && availablePrompts.Count > 0)
            {
                var currentMemory = MemoriesInstance.UserMemories.FirstOrDefault(m => m.UserName == Args.NickName);
                if (currentMemory != null && !string.IsNullOrEmpty(currentMemory.LastAiConfig) && availablePrompts.Contains(currentMemory.LastAiConfig))
                {
                    PromptComboBox.SelectedItem = currentMemory.LastAiConfig;
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

        private void ReplyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ReplyComboBox.SelectedItem is string selectedKey && AppConfig.Config?.PresetReplies != null && AppConfig.Config.PresetReplies.TryGetValue(selectedKey, out string? value))
            {
                ValueTextBlock.Text = value;
            }
            else
            {
                ValueTextBlock.Text = "";
            }
            // 更新记忆
            if (ReplyComboBox.SelectedItem is string selectedKeyMem)
            {
                var currentMemory = MemoriesInstance.UserMemories.FirstOrDefault(m => m.UserName == Args.NickName);
                if (currentMemory != null)
                {
                    currentMemory.LastPresetReply = selectedKeyMem;
                }
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
            if (PromptComboBox.SelectedItem is string selectedPrompt)
            {
                var currentMemory = MemoriesInstance.UserMemories.FirstOrDefault(m => m.UserName == Args.NickName);
                if (currentMemory != null)
                {
                    currentMemory.LastAiConfig = selectedPrompt;
                }
            }
        }

        /// <summary>
        /// 确定按钮点击事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">路由事件参数</param>
        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (ReplyComboBox.SelectedItem is string selectedKey && AppConfig.Config?.PresetReplies != null && AppConfig.Config.PresetReplies.TryGetValue(selectedKey, out string? value) && value != null)
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
            var button = sender as Button;
            System.Windows.Shapes.Path path = button?.Content as System.Windows.Shapes.Path;
            if (path == null) return;

            if (string.IsNullOrEmpty(SelectedPrompt) || AppConfig.Config?.AiConfigs == null) return;
            var aiConfig = AppConfig.Config.AiConfigs.FirstOrDefault(c => c.Name == SelectedPrompt);
            if (aiConfig == null) return;

            if (!IsGenerating)
            {
                // 开始生成
                IsGenerating = true;
                path.Data = Geometry.Parse("M 0 0 L 5 0 L 5 20 L 0 20 Z M 10 0 L 15 0 L 15 20 L 10 20 Z"); // 暂停图形
                ConfirmAiButton.IsEnabled = false;
                AiResponseTextBox.IsReadOnly = true;
                AiResponseTextBox.Text = "正在生成...";

                Cts = new CancellationTokenSource();
                try
                {
                    Client ??= new OllamaApiClient(baseUri: new Uri(AppConfig.Config.OllamaService!));
                    var messages = new List<Message>();

                    // 添加系统提示
                    string combinedPrompt = (AppConfig.Config?.Prompt ?? string.Empty) + "\n\n" + (aiConfig.Prompt ?? string.Empty);

                    // 添加自定义提示词
                    string customPrompt = CustomPromptTextBox.Text.Trim();
                    if (!string.IsNullOrEmpty(customPrompt))
                    {
                        combinedPrompt += "\n\n" + customPrompt;
                        // 添加到记忆列表
                        var currentMemory = MemoriesInstance.UserMemories.FirstOrDefault(m => m.UserName == Args.NickName);
                        if (currentMemory != null)
                        {
                            if (!MemoriesInstance.CustomPrompts.Contains(customPrompt))
                            {
                                MemoriesInstance.CustomPrompts.Add(customPrompt);
                                if (MemoriesInstance.CustomPrompts.Count > 10)
                                {
                                    MemoriesInstance.CustomPrompts.RemoveAt(0);
                                }
                            }
                        }
                    }

                    messages.Add(new Message(MessageRole.System, combinedPrompt, null, null));

                    // 添加历史对话
                    messages.Add(new Message(MessageRole.User, string.Join("\n", (Args.History ?? new List<ChatMessage>()).Select(h => h.ToFormattedString())), null, null));

                    var stream = Client.Chat.GenerateChatCompletionAsync(AppConfig.Config?.Model ?? string.Empty, messages, stream: true, cancellationToken: Cts.Token);

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
                    path.Data = Geometry.Parse("M 0 0 L 0 20 L 15 10 Z"); // 播放图形
                    GenerateAiButton.IsEnabled = true;
                    AiResponseTextBox.IsReadOnly = false;
                }
            }
            else
            {
                // 取消生成
                Cts?.Cancel();
                IsGenerating = false;
                path.Data = Geometry.Parse("M 0 0 L 0 20 L 15 10 Z"); // 播放图形
                GenerateAiButton.IsEnabled = true;
                ConfirmAiButton.IsEnabled = !string.IsNullOrEmpty(AiResponseTextBox.Text) && AiResponseTextBox.Text != "正在生成..." && AiResponseTextBox.Text != "已取消";
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
            SaveMemories();
            Cts?.Cancel();
            Client?.Dispose();
            Cts?.Dispose();
        }

        /// <summary>
        /// TabControl 选择改变事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">选择改变事件参数</param>
        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is TabControl tabControl)
            {
                var currentMemory = MemoriesInstance.UserMemories.FirstOrDefault(m => m.UserName == Args.NickName);
                if (currentMemory != null)
                {
                    currentMemory.LastTab = tabControl.SelectedIndex == 1 ? "AI" : "Preset";
                }
            }
        }

        /// <summary>
        /// TabControl 加载完成事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">路由事件参数</param>
        private void MainTabControl_Loaded(object sender, RoutedEventArgs e)
        {
            MainTabControl.SelectionChanged -= MainTabControl_SelectionChanged;

            // 根据记忆设置标签页
            var currentMemory = MemoriesInstance.UserMemories.FirstOrDefault(m => m.UserName == Args.NickName);
            if (currentMemory != null && currentMemory.LastTab == "AI")
            {
                MainTabControl.SelectedIndex = 1;
            }
            else
            {
                MainTabControl.SelectedIndex = 0;
            }

            MainTabControl.SelectionChanged += MainTabControl_SelectionChanged;
        }

        /// <summary>
        /// 加载用户记忆
        /// </summary>
        private void LoadMemories()
        {
            string memoriesFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "memories.json");
            try
            {
                if (File.Exists(memoriesFilePath))
                {
                    string json = File.ReadAllText(memoriesFilePath);
                    MemoriesInstance = JsonSerializer.Deserialize<Memories>(json) ?? new Memories();
                }
                else
                {
                    MemoriesInstance = new Memories();
                }
            }
            catch (Exception)
            {
                // 处理加载错误，可以记录日志或使用默认值
                MemoriesInstance = new Memories();
            }
        }

        /// <summary>
        /// 保存用户记忆
        /// </summary>
        private void SaveMemories()
        {
            string memoriesFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "memories.json");
            try
            {
                string json = JsonSerializer.Serialize(MemoriesInstance, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
                File.WriteAllText(memoriesFilePath, json);
            }
            catch (Exception)
            {
                // 处理保存错误，可以记录日志
            }
        }

        /// <summary>
        /// CustomPromptTextBox 键盘预览事件，用于历史提示切换
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">键盘事件参数</param>
        private void CustomPromptTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            int caretIndex = CustomPromptTextBox.CaretIndex;
            int lineIndex = CustomPromptTextBox.GetLineIndexFromCharacterIndex(caretIndex);
            int totalLines = CustomPromptTextBox.LineCount;

            if (e.Key == Key.Up)
            {
                // 只有当光标在第一行时，才切换历史
                if (lineIndex == 0 && MemoriesInstance.CustomPrompts.Count > 0)
                {
                    if (CurrentHistoryIndex == -1)
                    {
                        // 第一次按上键，保存当前输入
                        SavedCurrentInput = CustomPromptTextBox.Text;
                        CurrentHistoryIndex = MemoriesInstance.CustomPrompts.Count - 1;
                    }
                    else if (CurrentHistoryIndex > 0)
                    {
                        CurrentHistoryIndex--;
                    }
                    CustomPromptTextBox.Text = MemoriesInstance.CustomPrompts[CurrentHistoryIndex];
                    CustomPromptTextBox.CaretIndex = CustomPromptTextBox.Text.Length;
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Down)
            {
                // 只有当光标在最后一行时，才切换历史
                if (lineIndex == totalLines - 1)
                {
                    if (CurrentHistoryIndex >= 0 && CurrentHistoryIndex < MemoriesInstance.CustomPrompts.Count - 1)
                    {
                        CurrentHistoryIndex++;
                        CustomPromptTextBox.Text = MemoriesInstance.CustomPrompts[CurrentHistoryIndex];
                        CustomPromptTextBox.CaretIndex = CustomPromptTextBox.Text.Length;
                    }
                    else
                    {
                        // 到底部，恢复到保存的当前输入
                        CurrentHistoryIndex = -1;
                        CustomPromptTextBox.Text = SavedCurrentInput;
                        CustomPromptTextBox.CaretIndex = CustomPromptTextBox.Text.Length;
                    }
                    e.Handled = true;
                }
            }
            else if (e.Key != Key.Left && e.Key != Key.Right && e.Key != Key.Home && e.Key != Key.End && e.Key != Key.Back && e.Key != Key.Delete && !KeyIsModifier(e.Key))
            {
                // 当用户输入新字符时，重置历史索引
                CurrentHistoryIndex = -1;
                SavedCurrentInput = CustomPromptTextBox.Text;
            }
        }

        /// <summary>
        /// 检查按键是否为修饰键
        /// </summary>
        /// <param name="key">按键</param>
        /// <returns>是否为修饰键</returns>
        private bool KeyIsModifier(Key key)
        {
            return key == Key.LeftCtrl || key == Key.RightCtrl || key == Key.LeftShift || key == Key.RightShift || key == Key.LeftAlt || key == Key.RightAlt;
        }
    }
}