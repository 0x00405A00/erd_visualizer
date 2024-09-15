using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace ERD_Visualizer
{
    public static class WindowsShapesExtension
    {

        public static Rect ToRect(this Rectangle rectangle)
        {
            var x = Canvas.GetLeft(rectangle);
            var y = Canvas.GetTop(rectangle);
            return new Rect(x, y, rectangle.Width, rectangle.Height);
        }
    }
}
