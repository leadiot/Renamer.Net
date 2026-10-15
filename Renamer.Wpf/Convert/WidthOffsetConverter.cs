using System.Globalization;

namespace Com.Scm.Renamer.Convert
{
    /// <summary>
    /// 将绑定到的宽度减去固定偏移量，用于约束横向滚动区域的最大宽度，
    /// 使快捷标签过多时出现横向滚动条，而不是把窗口撑宽。
    /// </summary>
    public sealed class WidthOffsetConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double width = value is double d ? d : 0;
            double offset = 0;
            if (parameter != null)
            {
                double.TryParse(parameter.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out offset);
            }
            return Math.Max(0, width - offset);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
