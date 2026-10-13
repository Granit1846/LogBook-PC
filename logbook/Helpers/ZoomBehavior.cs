using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace logbook.Helpers
{
    /// <summary>
    /// Позволяет масштабировать содержимое ScrollViewer'а через Ctrl + колесо мыши.
    /// При запуске программы масштаб = 1.0.
    /// </summary>
    public static class ZoomBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(ZoomBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj)
            => (bool)obj.GetValue(IsEnabledProperty);

        public static void SetIsEnabled(DependencyObject obj, bool value)
            => obj.SetValue(IsEnabledProperty, value);

        // Минимум и максимум масштаба
        private const double MinScale = 0.3;
        private const double MaxScale = 2.0;

        // Шаг масштабирования (10% за одно прокручивание)
        private const double ZoomStep = 1.1;

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer sv)
            {
                if ((bool)e.NewValue)
                {
                    sv.PreviewMouseWheel += ScrollViewer_PreviewMouseWheel;
                }
                else
                {
                    sv.PreviewMouseWheel -= ScrollViewer_PreviewMouseWheel;
                }
            }
        }

        private static void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Реагируем только на Ctrl + колесо
            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
                return;

            if (!(sender is ScrollViewer sv))
                return;

            // Ищем текущий LayoutTransform (или создаём)
            var transform = sv.LayoutTransform as ScaleTransform;
            if (transform == null)
            {
                transform = new ScaleTransform(1.0, 1.0);
                sv.LayoutTransform = transform;
            }

            // Вычисляем новый масштаб
            double zoomFactor = e.Delta > 0 ? ZoomStep : 1.0 / ZoomStep;
            double newScale = transform.ScaleX * zoomFactor;

            // Ограничения
            if (newScale < MinScale) newScale = MinScale;
            if (newScale > MaxScale) newScale = MaxScale;

            transform.ScaleX = newScale;
            transform.ScaleY = newScale;

            e.Handled = true;
        }
    }
}