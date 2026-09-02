using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PlaytimeInsights.Controls
{
    public sealed class HeatmapCellButton : Button
    {
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            var swatch = new Rect(1d, 1d, 24d, 24d);
            var background = Background;
            if (background != null)
            {
                drawingContext.DrawRoundedRectangle(
                    background,
                    null,
                    swatch,
                    3d,
                    3d);
            }

            var highlight = IsMouseOver || IsKeyboardFocused;
            var borderBrush = highlight ? Foreground : BorderBrush;
            if (borderBrush != null)
            {
                var borderPen = new Pen(borderBrush, 1d);
                if (borderPen.CanFreeze)
                {
                    borderPen.Freeze();
                }

                drawingContext.DrawRoundedRectangle(
                    null,
                    borderPen,
                    swatch,
                    3d,
                    3d);
            }

            if (IsKeyboardFocused && Foreground != null)
            {
                var focusPen = new Pen(Foreground, 1d);
                if (focusPen.CanFreeze)
                {
                    focusPen.Freeze();
                }

                drawingContext.DrawRectangle(
                    null,
                    focusPen,
                    new Rect(2d, 2d, 22d, 22d));
            }
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            InvalidateVisual();
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            InvalidateVisual();
        }

        protected override void OnGotKeyboardFocus(
            KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);
            InvalidateVisual();
        }

        protected override void OnLostKeyboardFocus(
            KeyboardFocusChangedEventArgs e)
        {
            base.OnLostKeyboardFocus(e);
            InvalidateVisual();
        }
    }
}
