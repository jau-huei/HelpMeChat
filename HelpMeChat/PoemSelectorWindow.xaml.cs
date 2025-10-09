using System;
using System.Windows;

namespace HelpMeChat
{
    /// <summary>
    /// 诗句选择窗口类
    /// </summary>
    public partial class PoemSelectorWindow : Window
    {
        /// <summary>
        /// 诗句选择事件
        /// </summary>
        public event Action<string>? PoemSelected;

        /// <summary>
        /// 构造函数
        /// </summary>
        public PoemSelectorWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 诗1按钮点击事件
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void Poem1_Click(object sender, RoutedEventArgs e)
        {
            PoemSelected?.Invoke("床前明月光，疑是地上霜。举头望明月，低头思故乡。");
            Close();
        }

        /// <summary>
        /// 诗2按钮点击事件
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void Poem2_Click(object sender, RoutedEventArgs e)
        {
            PoemSelected?.Invoke("春眠不觉晓，处处闻啼鸟。夜来风雨声，花落知多少。");
            Close();
        }

        /// <summary>
        /// 诗3按钮点击事件
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void Poem3_Click(object sender, RoutedEventArgs e)
        {
            PoemSelected?.Invoke("白日依山尽，黄河入海流。欲穷千里目，更上一层楼。");
            Close();
        }
    }
}