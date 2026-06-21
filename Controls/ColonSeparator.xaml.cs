using System;
using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace MoooShowTime.Controls
{
    /// <summary>
    /// 冒号分隔符控件
    /// 两个像素点阵圆点，用于时:分之间
    /// 支持闪烁：可见/隐藏切换
    /// </summary>
    public sealed partial class ColonSeparator : UserControl
    {
        private const int DOT_SIZE = 8;         // 圆点中每个像素的大小（与数字像素匹配）
        private const int DOT_GAP = 3;          // 像素间距
        private const double STEP = DOT_SIZE + DOT_GAP;
        private const int DOT_COLS = 2;         // 圆点宽度 2列
        private const int DOT_ROWS = 2;         // 圆点高度 2行

        /// <summary>
        /// 冒号是否可见（闪烁控制）
        /// </summary>
        public bool IsVisible
        {
            get { return (bool)GetValue(IsVisibleProperty); }
            set { SetValue(IsVisibleProperty, value); }
        }

        public static readonly DependencyProperty IsVisibleProperty =
            DependencyProperty.Register("IsVisible", typeof(bool), typeof(ColonSeparator),
                new PropertyMetadata(true, OnVisibilityChanged));

        /// <summary>
        /// 亮的像素颜色
        /// </summary>
        public SolidColorBrush ActiveBrush
        {
            get { return (SolidColorBrush)GetValue(ActiveBrushProperty); }
            set { SetValue(ActiveBrushProperty, value); }
        }

        public static readonly DependencyProperty ActiveBrushProperty =
            DependencyProperty.Register("ActiveBrush", typeof(SolidColorBrush), typeof(ColonSeparator),
                new PropertyMetadata(new SolidColorBrush(Colors.White), OnAppearanceChanged));

        /// <summary>
        /// 暗的像素颜色
        /// </summary>
        public SolidColorBrush InactiveBrush
        {
            get { return (SolidColorBrush)GetValue(InactiveBrushProperty); }
            set { SetValue(InactiveBrushProperty, value); }
        }

        public static readonly DependencyProperty InactiveBrushProperty =
            DependencyProperty.Register("InactiveBrush", typeof(SolidColorBrush), typeof(ColonSeparator),
                new PropertyMetadata(new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)), OnAppearanceChanged));

        private List<Rectangle> _pixels = new List<Rectangle>();

        public ColonSeparator()
        {
            this.InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            BuildDots();
            ApplyVisibility();
        }

        private void BuildDots()
        {
            ColonCanvas.Children.Clear();
            _pixels.Clear();

            // 冒号两个点垂直居中分布，与数字的段中心对齐
            // 2x2圆点：内容宽 2*11-3=19px，偏移(53-19)/2=17px
            double hOffset = 17;
            DrawDot(hOffset, 5);
            DrawDot(hOffset, 14);
        }

        private void DrawDot(double leftOffset, int startRow)
        {
            for (int r = 0; r < DOT_ROWS; r++)
            {
                for (int c = 0; c < DOT_COLS; c++)
                {
                    var rect = new Rectangle
                    {
                        Width = DOT_SIZE,
                        Height = DOT_SIZE,
                        RadiusX = 1,
                        RadiusY = 1,
                    };

                    Canvas.SetLeft(rect, leftOffset + c * STEP);
                    Canvas.SetTop(rect, startRow * STEP + r * STEP);

                    ColonCanvas.Children.Add(rect);
                    _pixels.Add(rect);
                }
            }
        }

        private static void OnVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ColonSeparator)d;
            control.ApplyVisibility();
        }

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ColonSeparator)d;
            control.ApplyVisibility();
        }

        private void ApplyVisibility()
        {
            foreach (var rect in _pixels)
            {
                rect.Fill = IsVisible ? ActiveBrush : InactiveBrush;
            }
        }
    }
}
