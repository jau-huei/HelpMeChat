using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace HelpMeChat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// 属性改变事件
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 触发属性改变事件
        /// </summary>
        /// <param name="propertyName">属性名</param>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        /// <summary>
        /// UI 自动化监控实例
        /// </summary>
        private UIAutomationMonitor? Monitor { get; set; }

        /// <summary>
        /// 弹出窗口
        /// </summary>
        private ReplySelectorWindow? PopupWindow { get; set; }

        /// <summary>
        /// 应用程序配置
        /// </summary>
        private AppConfig? Config { get; set; }

        /// <summary>
        /// 可观察的预设回复集合
        /// </summary>
        private ObservableCollection<KeyValuePair<string, string>> PresetRepliesPrivate { get; set; } = new ObservableCollection<KeyValuePair<string, string>>();

        /// <summary>
        /// 可观察的 AI 设定集合
        /// </summary>
        private ObservableCollection<AiConfig> AiConfigsPrivate { get; set; } = new ObservableCollection<AiConfig>();

        /// <summary>
        /// 系统托盘图标
        /// </summary>
        private Forms.NotifyIcon? NotifyIcon { get; set; }

        /// <summary>
        /// 预设回复集合属性
        /// </summary>
        public ObservableCollection<KeyValuePair<string, string>> PresetReplies => PresetRepliesPrivate;

        /// <summary>
        /// AI 设定集合属性
        /// </summary>
        public ObservableCollection<AiConfig> AiConfigs => AiConfigsPrivate;

        /// <summary>
        /// 默认 Ollama IP
        /// </summary>
        public string DefaultOllamaIp
        {
            get => Config?.DefaultOllamaIp ?? "";
            set
            {
                if (Config != null)
                {
                    Config.DefaultOllamaIp = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 默认 Ollama 端口
        /// </summary>
        public int DefaultOllamaPort
        {
            get => Config?.DefaultOllamaPort ?? 0;
            set
            {
                if (Config != null)
                {
                    Config.DefaultOllamaPort = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 默认模型
        /// </summary>
        public string DefaultModel
        {
            get => Config?.DefaultModel ?? "";
            set
            {
                if (Config != null)
                {
                    Config.DefaultModel = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 默认微信用户名
        /// </summary>
        public string DefaultWeChatUserName
        {
            get => Config?.DefaultWeChatUserName ?? "";
            set
            {
                if (Config != null)
                {
                    Config.DefaultWeChatUserName = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Config = LoadConfig();
            DataContext = this;
            if (Config.PresetReplies != null)
            {
                foreach (var pair in Config.PresetReplies)
                {
                    PresetRepliesPrivate.Add(pair);
                }
            }
            Monitor = new UIAutomationMonitor();
            Monitor.Config = Config;
            Monitor.ShowPopup += OnShowPopup;
            Monitor.HidePopup += OnHidePopup;
            this.StateChanged += MainWindow_StateChanged;
            this.Closed += MainWindow_Closed;

            NotifyIcon = new Forms.NotifyIcon();
            NotifyIcon.Icon = new Drawing.Icon("Icon.ico");
            NotifyIcon.Text = "HelpMeChat";
            NotifyIcon.DoubleClick += NotifyIcon_DoubleClick;
            NotifyIcon.Visible = false;
        }

        /// <summary>
        /// 加载配置
        /// </summary>
        /// <returns>配置对象</returns>
        private AppConfig LoadConfig()
        {
            string configPath = "config.json";
            if (File.Exists(configPath))
            {
                string json = File.ReadAllText(configPath);
                Config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
            else
            {
                Config = new AppConfig();
            }
            // 确保属性不为 null
            Config.PresetReplies ??= new Dictionary<string, string>();
            Config.AiConfigs ??= new List<AiConfig>();
            PresetRepliesPrivate.Clear();
            if (Config.PresetReplies != null)
            {
                foreach (var pair in Config.PresetReplies)
                {
                    PresetRepliesPrivate.Add(pair);
                }
            }
            AiConfigsPrivate.Clear();
            if (Config.AiConfigs != null)
            {
                foreach (var config in Config.AiConfigs)
                {
                    AiConfigsPrivate.Add(config);
                }
            }
            return Config;
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        private void SaveConfig()
        {
            if (Config == null) return;
            Config.PresetReplies!.Clear();
            foreach (var pair in PresetRepliesPrivate)
            {
                Config.PresetReplies[pair.Key] = pair.Value;
            }
            Config.AiConfigs!.Clear();
            foreach (var config in AiConfigsPrivate)
            {
                Config.AiConfigs.Add(config);
            }
            string configPath = "config.json";
            string json = JsonSerializer.Serialize(Config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, json);
        }

        /// <summary>
        /// 预设回复列表选择改变事件
        /// </summary>
        private void PresetRepliesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PresetRepliesListBox.SelectedItem is KeyValuePair<string, string> selectedPair)
            {
                KeyTextBox.Text = selectedPair.Key;
                ValueTextBox.Text = selectedPair.Value;
            }
        }

        /// <summary>
        /// 添加回复按钮点击
        /// </summary>
        private void AddReply_Click(object sender, RoutedEventArgs e)
        {
            string key = KeyTextBox.Text.Trim();
            string value = ValueTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value) && !PresetRepliesPrivate.Any(p => p.Key == key))
            {
                PresetRepliesPrivate.Add(new KeyValuePair<string, string>(key, value));
                KeyTextBox.Text = "";
                ValueTextBox.Text = "";
            }
            else if (PresetRepliesPrivate.Any(p => p.Key == key))
            {
                MessageBox.Show("显示名称已存在，请使用修改功能。");
            }
        }

        /// <summary>
        /// 修改回复按钮点击
        /// </summary>
        private void ModifyReply_Click(object sender, RoutedEventArgs e)
        {
            if (PresetRepliesListBox.SelectedItem is KeyValuePair<string, string> selectedPair)
            {
                string newKey = KeyTextBox.Text.Trim();
                string newValue = ValueTextBox.Text.Trim();
                if (!string.IsNullOrEmpty(newKey) && !string.IsNullOrEmpty(newValue))
                {
                    if (newKey != selectedPair.Key && PresetRepliesPrivate.Any(p => p.Key == newKey))
                    {
                        MessageBox.Show("新显示名称已存在。");
                        return;
                    }
                    PresetRepliesPrivate.Remove(selectedPair);
                    PresetRepliesPrivate.Add(new KeyValuePair<string, string>(newKey, newValue));
                    KeyTextBox.Text = "";
                    ValueTextBox.Text = "";
                }
            }
            else
            {
                MessageBox.Show("请先选择要修改的项。");
            }
        }

        /// <summary>
        /// 删除回复按钮点击
        /// </summary>
        private void RemoveReply_Click(object sender, RoutedEventArgs e)
        {
            if (PresetRepliesListBox.SelectedItem is KeyValuePair<string, string> selectedPair)
            {
                PresetRepliesPrivate.Remove(selectedPair);
                KeyTextBox.Text = "";
                ValueTextBox.Text = "";
            }
        }

        /// <summary>
        /// AI 设定列表选择改变事件
        /// </summary>
        private void AiConfigsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AiConfigsListBox.SelectedItem is AiConfig selectedConfig)
            {
                AiNameTextBox.Text = selectedConfig.Name ?? "";
                AiIsSharedCheckBox.IsChecked = selectedConfig.IsShared;
                AiPromptTextBox.Text = selectedConfig.Prompt ?? "";
            }
        }

        /// <summary>
        /// 添加 AI 设定按钮点击
        /// </summary>
        private void AddAiConfig_Click(object sender, RoutedEventArgs e)
        {
            string name = AiNameTextBox.Text.Trim();
            bool isShared = AiIsSharedCheckBox.IsChecked ?? false;
            string prompt = AiPromptTextBox.Text.Trim();

            if (!string.IsNullOrEmpty(name) && !AiConfigsPrivate.Any(c => c.Name == name))
            {
                AiConfigsPrivate.Add(new AiConfig
                {
                    Name = name,
                    IsShared = isShared,
                    Prompt = prompt
                });
                ClearAiConfigFields();
            }
            else if (AiConfigsPrivate.Any(c => c.Name == name))
            {
                MessageBox.Show("设定名称已存在，请使用修改功能。");
            }
        }

        /// <summary>
        /// 修改 AI 设定按钮点击
        /// </summary>
        private void ModifyAiConfig_Click(object sender, RoutedEventArgs e)
        {
            if (AiConfigsListBox.SelectedItem is AiConfig selectedConfig)
            {
                string newName = AiNameTextBox.Text.Trim();
                bool isShared = AiIsSharedCheckBox.IsChecked ?? false;
                string prompt = AiPromptTextBox.Text.Trim();

                if (!string.IsNullOrEmpty(newName))
                {
                    if (newName != selectedConfig.Name && AiConfigsPrivate.Any(c => c.Name == newName))
                    {
                        MessageBox.Show("新设定名称已存在。");
                        return;
                    }
                    AiConfigsPrivate.Remove(selectedConfig);
                    AiConfigsPrivate.Add(new AiConfig
                    {
                        Name = newName,
                        IsShared = isShared,
                        Prompt = prompt
                    });
                    ClearAiConfigFields();
                }
            }
            else
            {
                MessageBox.Show("请先选择要修改的项。");
            }
        }

        /// <summary>
        /// 删除 AI 设定按钮点击
        /// </summary>
        private void RemoveAiConfig_Click(object sender, RoutedEventArgs e)
        {
            if (AiConfigsListBox.SelectedItem is AiConfig selectedConfig)
            {
                AiConfigsPrivate.Remove(selectedConfig);
                ClearAiConfigFields();
            }
        }

        /// <summary>
        /// 清空 AI 设定输入字段
        /// </summary>
        private void ClearAiConfigFields()
        {
            AiNameTextBox.Text = "";
            AiIsSharedCheckBox.IsChecked = false;
            AiPromptTextBox.Text = "";
        }

        /// <summary>
        /// 保存配置按钮点击
        /// </summary>
        private void SaveConfig_Click(object sender, RoutedEventArgs e)
        {
            SaveConfig();
            LoadConfig();
            StatusTextBlock.Text = "配置已保存！";
        }

        /// <summary>
        /// 加载配置按钮点击
        /// </summary>
        private void LoadConfig_Click(object sender, RoutedEventArgs e)
        {
            Config = LoadConfig();
            StatusTextBlock.Text = "配置已加载！";
        }

        /// <summary>
        /// 显示弹出窗口
        /// </summary>
        /// <param name="left">左位置</param>
        /// <param name="top">上位置</param>
        /// <param name="history">聊天历史</param>
        /// <param name="currentUserName">当前用户名称</param>
        private void OnShowPopup(double left, double top, List<(string, string)> history, string currentUserName)
        {
            Dispatcher.Invoke(() =>
            {
                if (Config?.PresetReplies == null) return;
                if (PopupWindow == null || !PopupWindow.IsVisible)
                {
                    PopupWindow = new ReplySelectorWindow(Config.PresetReplies, Config.AiConfigs, currentUserName, Config.DefaultOllamaIp, Config.DefaultOllamaPort.ToString(), Config.DefaultModel, history);
                    PopupWindow.ReplySelected += OnReplySelectedInternal;
                    PopupWindow.AiReplySelected += OnAiReplySelectedInternal;

                    // 获取当前显示器 DPI 信息
                    var source = PresentationSource.FromVisual(Application.Current.MainWindow);
                    double dpiX = 1.0, dpiY = 1.0;
                    if (source?.CompositionTarget != null)
                    {
                        dpiX = source.CompositionTarget.TransformFromDevice.M11;
                        dpiY = source.CompositionTarget.TransformFromDevice.M22;
                    }

                    // 将像素坐标转换为 WPF 坐标
                    PopupWindow.Left = left * dpiX;
                    PopupWindow.Top = top * dpiY - PopupWindow.Height;

                    PopupWindow.Show();
                }
            });
        }

        /// <summary>
        /// 隐藏弹出窗口
        /// </summary>
        private void OnHidePopup()
        {
            Dispatcher.Invoke(() =>
            {
                if (PopupWindow != null && PopupWindow.IsVisible)
                {
                    PopupWindow.Close();
                    PopupWindow = null;
                }
            });
        }

        /// <summary>
        /// 内部 AI 回复选择事件
        /// </summary>
        /// <param name="reply">选择的 AI 回复</param>
        private void OnAiReplySelectedInternal(string reply)
        {
            Monitor!.OnReplySelected(reply);
            OnHidePopup();
        }

        /// <summary>
        /// 内部回复选择事件
        /// </summary>
        /// <param name="reply">选择的回复</param>
        private void OnReplySelectedInternal(string reply)
        {
            Monitor!.OnReplySelected(reply);
            OnHidePopup();
        }

        /// <summary>
        /// 窗口状态改变事件
        /// </summary>
        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.Hide();
                NotifyIcon!.Visible = true;
            }
        }

        /// <summary>
        /// 托盘图标双击事件
        /// </summary>
        private void NotifyIcon_DoubleClick(object? sender, EventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
            NotifyIcon!.Visible = false;
        }

        /// <summary>
        /// 窗口关闭事件
        /// </summary>
        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            NotifyIcon!.Dispose();
        }
    }
}