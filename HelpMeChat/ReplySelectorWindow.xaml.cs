using System;
using System.Windows;

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
        public ReplySelectorWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 回复1按钮点击事件
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void Reply1_Click(object sender, RoutedEventArgs e)
        {
            ReplySelected?.Invoke("好的，谢谢！");
            Close();
        }

        /// <summary>
        /// 回复2按钮点击事件
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void Reply2_Click(object sender, RoutedEventArgs e)
        {
            ReplySelected?.Invoke("请稍等一下。");
            Close();
        }

        /// <summary>
        /// 回复3按钮点击事件
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void Reply3_Click(object sender, RoutedEventArgs e)
        {
            ReplySelected?.Invoke("明白了。");
            Close();
        }
    }
}