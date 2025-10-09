using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.Generic;

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
        /// 构造函数
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            monitor = new UIAutomationMonitor();
            monitor.ShowPopup += OnShowPopup;
            monitor.HidePopup += OnHidePopup;
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
                    popupWindow = new ReplySelectorWindow();
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