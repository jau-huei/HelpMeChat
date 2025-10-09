using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace HelpMeChat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// UI 自动化监控实例
        /// </summary>
        private UIAutomationMonitor monitor;

        /// <summary>
        /// 弹出窗口
        /// </summary>
        private ReplySelectorWindow? popupWindow;

        /// <summary>
        /// 应用程序配置
        /// </summary>
        private AppConfig config;

        /// <summary>
        /// 可观察的预设回复集合
        /// </summary>
        private ObservableCollection<KeyValuePair<string, string>> _presetReplies = new ObservableCollection<KeyValuePair<string, string>>();

        /// <summary>
        /// 预设回复集合属性
        /// </summary>
        public ObservableCollection<KeyValuePair<string, string>> PresetReplies => _presetReplies;

        /// <summary>
        /// 构造函数
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            config = LoadConfig();
            DataContext = this;
            foreach (var pair in config.PresetReplies)
            {
                _presetReplies.Add(pair);
            }
            monitor = new UIAutomationMonitor();
            monitor.ShowPopup += OnShowPopup;
            monitor.HidePopup += OnHidePopup;
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
                config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
            else
            {
                config = new AppConfig();
            }
            _presetReplies.Clear();
            foreach (var pair in config.PresetReplies)
            {
                _presetReplies.Add(pair);
            }
            return config;
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        private void SaveConfig()
        {
            config.PresetReplies.Clear();
            foreach (var pair in _presetReplies)
            {
                config.PresetReplies[pair.Key] = pair.Value;
            }
            string configPath = "config.json";
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
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
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value) && !_presetReplies.Any(p => p.Key == key))
            {
                _presetReplies.Add(new KeyValuePair<string, string>(key, value));
                KeyTextBox.Text = "";
                ValueTextBox.Text = "";
            }
            else if (_presetReplies.Any(p => p.Key == key))
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
                    if (newKey != selectedPair.Key && _presetReplies.Any(p => p.Key == newKey))
                    {
                        MessageBox.Show("新显示名称已存在。");
                        return;
                    }
                    _presetReplies.Remove(selectedPair);
                    _presetReplies.Add(new KeyValuePair<string, string>(newKey, newValue));
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
                _presetReplies.Remove(selectedPair);
                KeyTextBox.Text = "";
                ValueTextBox.Text = "";
            }
        }

        /// <summary>
        /// 保存配置按钮点击
        /// </summary>
        private void SaveConfig_Click(object sender, RoutedEventArgs e)
        {
            SaveConfig();
            StatusTextBlock.Text = "配置已保存！";
        }

        /// <summary>
        /// 加载配置按钮点击
        /// </summary>
        private void LoadConfig_Click(object sender, RoutedEventArgs e)
        {
            config = LoadConfig();
            StatusTextBlock.Text = "配置已加载！";
        }

        /// <summary>
        /// 显示弹出窗口
        /// </summary>
        /// <param name="left">左位置</param>
        /// <param name="top">上位置</param>
        /// <param name="history">聊天历史</param>
        private void OnShowPopup(double left, double top, List<(string, string)> history)
        {
            Dispatcher.Invoke(() =>
            {
                if (popupWindow == null || !popupWindow.IsVisible)
                {
                    popupWindow = new ReplySelectorWindow(config.PresetReplies);
                    popupWindow.ReplySelected += OnReplySelectedInternal;
                    popupWindow.Left = left;
                    popupWindow.Top = top;
                    popupWindow.Show();
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
                if (popupWindow != null && popupWindow.IsVisible)
                {
                    popupWindow.Close();
                    popupWindow = null;
                }
            });
        }

        /// <summary>
        /// 回复选择事件
        /// </summary>
        /// <param name="reply">选择的回复</param>
        private void OnReplySelected(string reply)
        {
            // 处理回复选择后的逻辑，如果需要
        }

        /// <summary>
        /// 内部回复选择事件
        /// </summary>
        /// <param name="reply">选择的回复</param>
        private void OnReplySelectedInternal(string reply)
        {
            monitor.OnReplySelected(reply);
            OnHidePopup();
        }
    }
}