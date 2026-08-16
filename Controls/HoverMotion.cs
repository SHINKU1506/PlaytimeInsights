using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PlaytimeInsights.Controls
{
    public static class HoverMotion
    {
        private static readonly DependencyProperty IsAttachedProperty =
            DependencyProperty.RegisterAttached(
                "IsAttached",
                typeof(bool),
                typeof(HoverMotion),
                new PropertyMetadata(false));

        private static readonly DependencyProperty IsLiftedProperty =
            DependencyProperty.RegisterAttached(
                "IsLifted",
                typeof(bool),
                typeof(HoverMotion),
                new PropertyMetadata(false));

        public static readonly DependencyProperty EnabledProperty =
            DependencyProperty.RegisterAttached(
                "Enabled",
                typeof(bool),
                typeof(HoverMotion),
                new FrameworkPropertyMetadata(
                    false,
                    OnEnabledChanged));

        public static readonly DependencyProperty LiftYProperty =
            DependencyProperty.RegisterAttached(
                "LiftY",
                typeof(double),
                typeof(HoverMotion),
                new FrameworkPropertyMetadata(1d));

        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.RegisterAttached(
                "Duration",
                typeof(double),
                typeof(HoverMotion),
                new FrameworkPropertyMetadata(120d));

        public static bool GetEnabled(FrameworkElement element)
        {
            return (bool)element.GetValue(EnabledProperty);
        }

        public static void SetEnabled(FrameworkElement element, bool value)
        {
            element.SetValue(EnabledProperty, value);
        }

        public static double GetLiftY(FrameworkElement element)
        {
            return (double)element.GetValue(LiftYProperty);
        }

        public static void SetLiftY(FrameworkElement element, double value)
        {
            element.SetValue(LiftYProperty, value);
        }

        public static double GetDuration(FrameworkElement element)
        {
            return (double)element.GetValue(DurationProperty);
        }

        public static void SetDuration(FrameworkElement element, double value)
        {
            element.SetValue(DurationProperty, value);
        }

        private static void OnEnabledChanged(
            DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs args)
        {
            var element = dependencyObject as FrameworkElement;
            if (element == null)
            {
                return;
            }

            if ((bool)args.NewValue)
            {
                Attach(element);
            }
            else
            {
                Detach(element);
            }
        }

        private static void Attach(FrameworkElement element)
        {
            if ((bool)element.GetValue(IsAttachedProperty))
            {
                return;
            }

            element.SetValue(IsAttachedProperty, true);
            element.MouseEnter += Element_MouseEnter;
            element.MouseLeave += Element_MouseLeave;
            element.Unloaded += Element_Unloaded;
            element.IsEnabledChanged += Element_IsEnabledChanged;
            element.DataContextChanged += Element_DataContextChanged;
        }

        private static void Detach(FrameworkElement element)
        {
            if (!(bool)element.GetValue(IsAttachedProperty))
            {
                return;
            }

            element.MouseEnter -= Element_MouseEnter;
            element.MouseLeave -= Element_MouseLeave;
            element.Unloaded -= Element_Unloaded;
            element.IsEnabledChanged -= Element_IsEnabledChanged;
            element.DataContextChanged -= Element_DataContextChanged;
            element.SetValue(IsAttachedProperty, false);
            ResetY(element);
        }

        private static void Element_MouseEnter(
            object sender,
            MouseEventArgs e)
        {
            var element = sender as FrameworkElement;
            if (element != null &&
                element.IsEnabled &&
                !(bool)element.GetValue(IsLiftedProperty))
            {
                element.SetValue(IsLiftedProperty, true);
                AnimateY(element, -GetLiftY(element), GetDuration(element));
            }
        }

        private static void Element_MouseLeave(
            object sender,
            MouseEventArgs e)
        {
            var element = sender as FrameworkElement;
            if (element != null &&
                element.IsEnabled &&
                (bool)element.GetValue(IsLiftedProperty))
            {
                element.SetValue(IsLiftedProperty, false);
                AnimateY(element, 0d, GetDuration(element));
            }
        }

        private static void Element_Unloaded(
            object sender,
            RoutedEventArgs e)
        {
            ResetY(sender as FrameworkElement);
        }

        private static void Element_IsEnabledChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            var element = sender as FrameworkElement;
            if (element != null && !element.IsEnabled)
            {
                ResetY(element);
            }
        }

        private static void Element_DataContextChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            ResetY(sender as FrameworkElement);
        }

        private static void AnimateY(
            FrameworkElement element,
            double to,
            double durationMilliseconds)
        {
            var transform = EnsureTransform(element);
            if (transform == null)
            {
                return;
            }

            if (!SystemParameters.ClientAreaAnimation ||
                durationMilliseconds <= 0d)
            {
                transform.BeginAnimation(TranslateTransform.YProperty, null);
                transform.Y = to;
                return;
            }

            var duration = TimeSpan.FromMilliseconds(
                durationMilliseconds);
            if (Math.Abs(to) < 0.001d)
            {
                // Leaving: the base value stays at 0; animate the held lift
                // back to 0 and release the clock when finished.
                transform.BeginAnimation(
                    TranslateTransform.YProperty,
                    new DoubleAnimation(
                        -GetLiftY(element),
                        0d,
                        duration)
                    {
                        EasingFunction = new CubicEase
                        {
                            EasingMode = EasingMode.EaseOut
                        },
                        FillBehavior = FillBehavior.Stop
                    },
                    HandoffBehavior.SnapshotAndReplace);
                return;
            }

            // Entering: the base value stays at 0 while the lifted value is
            // held until MouseLeave, DataContext change, unload, or disable.
            transform.BeginAnimation(TranslateTransform.YProperty, null);
            transform.Y = 0d;
            transform.BeginAnimation(
                TranslateTransform.YProperty,
                new DoubleAnimation(0d, to, duration)
                {
                    EasingFunction = new CubicEase
                    {
                        EasingMode = EasingMode.EaseOut
                    },
                    FillBehavior = FillBehavior.HoldEnd
                },
                HandoffBehavior.SnapshotAndReplace);
        }

        private static void ResetY(FrameworkElement element)
        {
            if (element == null)
            {
                return;
            }

            element.SetValue(IsLiftedProperty, false);
            var transform = EnsureTransform(element);
            if (transform == null)
            {
                return;
            }

            transform.BeginAnimation(TranslateTransform.YProperty, null);
            transform.Y = 0d;
        }

        private static TranslateTransform EnsureTransform(
            FrameworkElement element)
        {
            if (element == null)
            {
                return null;
            }

            var translate = element.RenderTransform as TranslateTransform;
            if (translate != null)
            {
                return translate;
            }

            translate = new TranslateTransform(0d, 0d);
            element.RenderTransform = translate;
            return translate;
        }
    }
}
