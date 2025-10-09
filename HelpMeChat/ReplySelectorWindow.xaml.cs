using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
        /// 构造函数
        /// </summary>
        /// <param name="presetReplies">预设回复字典</param>
        public ReplySelectorWindow(Dictionary<string, string> presetReplies)
        {
            InitializeComponent();
            this.presetReplies = presetReplies;
            ReplyComboBox.ItemsSource = presetReplies.Keys;
            if (presetReplies.Count > 0)
            {
                ReplyComboBox.SelectedIndex = 0;
                // 初始化显示Value
                if (ReplyComboBox.SelectedItem is string selectedKey && presetReplies.TryGetValue(selectedKey, out string? value))
                {
                    ValueTextBlock.Text = value;
                }
            }
        }

        /// <summary>
        /// 预设回复字典
        /// </summary>
        private Dictionary<string, string> presetReplies;

        /// <summary>
        /// ComboBox选择改变事件
        /// </summary>
        private void ReplyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ReplyComboBox.SelectedItem is string selectedKey && presetReplies.TryGetValue(selectedKey, out string? value))
            {
                ValueTextBlock.Text = value;
            }
            else
            {
                ValueTextBlock.Text = "";
            }
        }

        /// <summary>
        /// 确定按钮点击事件
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (ReplyComboBox.SelectedItem is string selectedKey && presetReplies.TryGetValue(selectedKey, out string? value))
            {
                ReplySelected?.Invoke(value);
                Close();
            }
        }

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
            this.Close();
        }
    }
}