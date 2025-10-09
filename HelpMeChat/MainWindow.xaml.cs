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
        private PoemSelectorWindow? popupWindow;

        /// <summary>
        /// 构造函数
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            monitor = new UIAutomationMonitor();
            monitor.ShowPopup += OnShowPopup;
            monitor.HidePopup += OnHidePopup;
            monitor.PoemSelected += OnPoemSelected;
        }

        /// <summary>
        /// 显示弹出窗口
        /// </summary>
        /// <param name="left">左位置</param>
        /// <param name="top">上位置</param>
        private void OnShowPopup(double left, double top)
        {
            Dispatcher.Invoke(() =>
            {
                if (popupWindow == null || !popupWindow.IsVisible)
                {
                    popupWindow = new PoemSelectorWindow();
                    popupWindow.PoemSelected += OnPoemSelectedInternal;
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
        /// 诗句选择事件
        /// </summary>
        /// <param name="poem">选择的诗句</param>
        private void OnPoemSelected(string poem)
        {
            // 处理诗句选择后的逻辑，如果需要
        }

        /// <summary>
        /// 内部诗句选择事件
        /// </summary>
        /// <param name="poem">选择的诗句</param>
        private void OnPoemSelectedInternal(string poem)
        {
            monitor.SelectPoem(poem);
            OnHidePopup();
        }
    }
}