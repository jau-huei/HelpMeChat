using System;
using System.Globalization;
using System.Windows.Data;

namespace HelpMeChat
{
    /// <summary>
    /// 将字符串转换为只显示第一行，并限制长度最多50个字符，如果超过或有换行则添加省略号。
    /// </summary>
    public class FirstLineConverter : IValueConverter
    {
        /// <summary>
        /// 转换方法：提取第一行，限制长度并添加省略号。
        /// </summary>
        /// <param name="value">要转换的值，应为字符串。</param>
        /// <param name="targetType">目标类型。</param>
        /// <param name="parameter">转换参数。</param>
        /// <param name="culture">文化信息。</param>
        /// <returns>转换后的字符串。</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                string firstLine = str.Split('\n')[0];
                bool hasNewline = str.Contains('\n');
                if (hasNewline || firstLine.Length > 50)
                {
                    return firstLine.Length > 50 ? firstLine.Substring(0, 50) + "..." : firstLine + "...";
                }
                else
                {
                    return firstLine;
                }
            }
            return value;
        }

        /// <summary>
        /// 反向转换方法，未实现。
        /// </summary>
        /// <param name="value">要反向转换的值。</param>
        /// <param name="targetType">目标类型。</param>
        /// <param name="parameter">转换参数。</param>
        /// <param name="culture">文化信息。</param>
        /// <returns>抛出 NotImplementedException。</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}