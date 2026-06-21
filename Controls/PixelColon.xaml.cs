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
    /// 像素风格冒号分隔符控件
    /// 两个 2×2 像素方块，绿色霓虹风格
    /// 与 PixelDigit 的 7×5 像素网格对齐
    /// </summary>
    public sealed partial class PixelColon : UserControl
    {
        private const int PIXEL_SIZE = 18;      // 像素大小（与 PixelDigit 匹配）
        private const int PIXEL_GAP = 5;        // 像素间距
        private const double STEP = PIXEL_SIZE + PIXEL_GAP;  // 23
        private const int DOT_COLS = 2;         // 每个点 2 列
        private const int DOT_ROWS = 2;         // 每个点 2 行
        private const int PADDING = 10;          // 内边距（与 PixelDigit 匹配）

        // 像素颜色 - 绿色霓虹风格
        private static readonly Color ACTIVE_COLOR = Color.FromArgb(255, 50, 220, 100);
        private static readonly Color INACTIVE_COLOR = Color.FromArgb(12, 34, 197, 94);
        private static readonly Color SHADOW_COLOR = Color.FromArgb(40, 33, 197, 94);

        /// <summary>
        /// 冒号是否可见（闪烁控制）
        /// </summary>
        public bool IsVisible
        {
            get { return (bool)GetValue(IsVisibleProperty); }
            set { SetValue(IsVisibleProperty, value); }
        }

        public static readonly DependencyProperty IsVisibleProperty =
            DependencyProperty.Register("IsVisible", typeof(bool), typeof(PixelColon),
                new PropertyMetadata(true, OnVisibilityChanged));

        private List<Rectangle> _pixels = new List<Rectangle>();
        private List<Rectangle> _shadows = new List<Rectangle>();

        public PixelColon()
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
            _shadows.Clear();

            // PixelDigit 7行网格 (row 0~6), 带 PADDING=10
            // 冒号上点对齐 row 2~3, 下点对齐 row 4~5
            // 水平居中：Canvas宽度64, 2个像素宽=2*18+5=41, 居中偏移=(64-41)/2=11.5
            
            const double leftOffset = 11.5;
            
            // 上点: row 2 开始（grid row 2~3），加上 PADDING 偏移
            DrawDot(leftOffset, PADDING + 2 * STEP);
            // 下点: row 4 开始（grid row 4~5），加上 PADDING 偏移
            DrawDot(leftOffset, PADDING + 4 * STEP);
        }

        private void DrawDot(double leftOffset, double topOffset)
        {
            for (int r = 0; r < DOT_ROWS; r++)
            {
                for (int c = 0; c < DOT_COLS; c++)
                {
                    var rect = new Rectangle
                    {
                        Width = PIXEL_SIZE,
                        Height = PIXEL_SIZE,
                        RadiusX = 3,
                        RadiusY = 3,
                    };

                    Canvas.SetLeft(rect, leftOffset + c * STEP);
                    Canvas.SetTop(rect, topOffset + r * STEP);

                    ColonCanvas.Children.Add(rect);
                    _pixels.Add(rect);

                    var shadow = new Rectangle
                    {
                        Width = PIXEL_SIZE + 6,
                        Height = PIXEL_SIZE + 6,
                        RadiusX = 4,
                        RadiusY = 4,
                        Opacity = 0.15,
                    };

                    Canvas.SetLeft(shadow, leftOffset + c * STEP - 3);
                    Canvas.SetTop(shadow, topOffset + r * STEP - 3);

                    ColonCanvas.Children.Add(shadow);
                    _shadows.Add(shadow);
                }
            }
        }

        private static void OnVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (PixelColon)d;
            control.ApplyVisibility();
        }

        private void ApplyVisibility()
        {
            for (int i = 0; i < _pixels.Count; i++)
            {
                _pixels[i].Fill = new SolidColorBrush(IsVisible ? ACTIVE_COLOR : INACTIVE_COLOR);
                _shadows[i].Fill = new SolidColorBrush(IsVisible ? SHADOW_COLOR : Colors.Transparent);
            }
        }
    }
}
