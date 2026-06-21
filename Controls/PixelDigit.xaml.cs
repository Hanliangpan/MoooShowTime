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
    /// 像素风格数字控件
    /// 5×7 像素网格字形，复古像素霓虹风格
    /// 基于 SVG 像素时钟设计
    /// </summary>
    public sealed partial class PixelDigit : UserControl
    {
        private const int PIXEL_SIZE = 18;      // 每个像素方块大小（放大以提高可读性）
        private const int PIXEL_GAP = 5;        // 像素间距
        private const int COLS = 5;              // 字形列数
        private const int ROWS = 7;              // 字形行数
        private const int PADDING = 10;           // 内边距

        private static readonly double STEP = PIXEL_SIZE + PIXEL_GAP;

        // 0-9 像素字形定义 (7行×5列)
        // 第一维=行(row), 第二维=列(col)
        // true = 亮像素, false = 空
        private static readonly bool[][,] PIXEL_FONTS = new bool[][,]
        {
            // 数字 0: 空心矩形
            new bool[7,5]
            {
                { true,  true,  true,  true,  true },
                { true,  false, false, false, true },
                { true,  false, false, false, true },
                { true,  false, false, false, true },
                { true,  false, false, false, true },
                { true,  false, false, false, true },
                { true,  true,  true,  true,  true },
            },
            // 数字 1: 带顶部斜面+底部横杠
            new bool[7,5]
            {
                { false, false, true,  false, false },
                { false, true,  true,  false, false },
                { false, false, true,  false, false },
                { false, false, true,  false, false },
                { false, false, true,  false, false },
                { false, false, true,  false, false },
                { true,  true,  true,  true,  true },
            },
            // 数字 2: 顶栏+右上+中栏+左下+底栏
            new bool[7,5]
            {
                { true,  true,  true,  true,  true },
                { false, false, false, false, true },
                { false, false, false, false, true },
                { true,  true,  true,  true,  true },
                { true,  false, false, false, false },
                { true,  false, false, false, false },
                { true,  true,  true,  true,  true },
            },
            // 数字 3: 三横+右侧竖
            new bool[7,5]
            {
                { true,  true,  true,  true,  true },
                { false, false, false, false, true },
                { false, false, false, false, true },
                { true,  true,  true,  true,  true },
                { false, false, false, false, true },
                { false, false, false, false, true },
                { true,  true,  true,  true,  true },
            },
            // 数字 4: 左上竖+中栏+右侧全竖
            new bool[7,5]
            {
                { true,  false, false, false, true },
                { true,  false, false, false, true },
                { true,  false, false, false, true },
                { true,  true,  true,  true,  true },
                { false, false, false, false, true },
                { false, false, false, false, true },
                { false, false, false, false, true },
            },
            // 数字 5: 顶栏+左上+中栏+右下+底栏
            new bool[7,5]
            {
                { true,  true,  true,  true,  true },
                { true,  false, false, false, false },
                { true,  false, false, false, false },
                { true,  true,  true,  true,  true },
                { false, false, false, false, true },
                { false, false, false, false, true },
                { true,  true,  true,  true,  true },
            },
            // 数字 6: 顶栏+左侧竖+中栏+下半空心框
            new bool[7,5]
            {
                { true,  true,  true,  true,  true },
                { true,  false, false, false, false },
                { true,  false, false, false, false },
                { true,  true,  true,  true,  true },
                { true,  false, false, false, true },
                { true,  false, false, false, true },
                { true,  true,  true,  true,  true },
            },
            // 数字 7: 顶栏+右上+中栏过渡+右竖
            new bool[7,5]
            {
                { true,  true,  true,  true,  true },
                { false, false, false, false, true },
                { false, false, false, true,  false },
                { false, false, true,  false, false },
                { false, false, true,  false, false },
                { false, false, true,  false, false },
                { false, false, true,  false, false },
            },
            // 数字 8: 上下两个空心框
            new bool[7,5]
            {
                { true,  true,  true,  true,  true },
                { true,  false, false, false, true },
                { true,  false, false, false, true },
                { true,  true,  true,  true,  true },
                { true,  false, false, false, true },
                { true,  false, false, false, true },
                { true,  true,  true,  true,  true },
            },
            // 数字 9: 上半空心框+右侧竖+底栏
            new bool[7,5]
            {
                { true,  true,  true,  true,  true },
                { true,  false, false, false, true },
                { true,  false, false, false, true },
                { true,  true,  true,  true,  true },
                { false, false, false, false, true },
                { false, false, false, false, true },
                { true,  true,  true,  true,  true },
            },
        };

        // 像素颜色 - 绿色霓虹风格 (与SVG匹配)
        private static readonly Color ACTIVE_COLOR = Color.FromArgb(255, 50, 220, 100);  // 明亮绿色
        private static readonly Color INACTIVE_COLOR = Color.FromArgb(12, 34, 197, 94); // 几乎不可见，让数字形状清晰
        private static readonly Color SHADOW_COLOR = Color.FromArgb(40, 33, 197, 94);  // 淡发光

        /// <summary>
        /// 当前显示的数字
        /// </summary>
        public int Value
        {
            get { return (int)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(int), typeof(PixelDigit),
                new PropertyMetadata(0, OnValueChanged));

        private List<Rectangle> _allPixels = new List<Rectangle>();
        private List<Rectangle> _shadowPixels = new List<Rectangle>();

        public PixelDigit()
        {
            this.InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            BuildPixelMatrix();
            UpdateDisplay();
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (PixelDigit)d;
            if (control._allPixels.Count > 0)
                control.UpdateDisplay();
        }

        private void BuildPixelMatrix()
        {
            PixelCanvas.Children.Clear();
            _allPixels.Clear();
            _shadowPixels.Clear();

            int totalCols = COLS;
            int totalRows = ROWS;

            for (int col = 0; col < totalCols; col++)
            {
                for (int row = 0; row < totalRows; row++)
                {
                    // 主像素
                    var rect = new Rectangle
                    {
                        Width = PIXEL_SIZE,
                        Height = PIXEL_SIZE,
                        RadiusX = 3,
                        RadiusY = 3,
                    };

                    Canvas.SetLeft(rect, PADDING + col * STEP);
                    Canvas.SetTop(rect, PADDING + row * STEP);

                    PixelCanvas.Children.Add(rect);
                    _allPixels.Add(rect);

                    // 阴影像素（偏移产生霓虹发光效果）
                    var shadow = new Rectangle
                    {
                        Width = PIXEL_SIZE + 6,
                        Height = PIXEL_SIZE + 6,
                        RadiusX = 4,
                        RadiusY = 4,
                        Opacity = 0.15,
                    };

                    Canvas.SetLeft(shadow, PADDING + col * STEP - 3);
                    Canvas.SetTop(shadow, PADDING + row * STEP - 3);

                    PixelCanvas.Children.Add(shadow);
                    _shadowPixels.Add(shadow);
                }
            }

            // 设置控件尺寸
            this.Width = PADDING * 2 + totalCols * STEP - PIXEL_GAP;
            this.Height = PADDING * 2 + totalRows * STEP - PIXEL_GAP;
        }

        private void UpdateDisplay()
        {
            int digit = Value;
            if (digit < 0) digit = 0;
            if (digit > 9) digit = 9;

            var font = PIXEL_FONTS[digit];

            for (int i = 0; i < _allPixels.Count; i++)
            {
                int col = i % COLS;
                int row = i / COLS;

                bool isActive = font[row, col];

                _allPixels[i].Fill = new SolidColorBrush(isActive ? ACTIVE_COLOR : INACTIVE_COLOR);
                _shadowPixels[i].Fill = new SolidColorBrush(isActive ? SHADOW_COLOR : Colors.Transparent);
            }
        }
    }
}
