using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace MoooShowTime.Controls
{
    /// <summary>
    /// 点阵像素数码管数字控件
    /// 7段数码管风格，每段由多个像素方块组成
    /// 像素间隔制造复古点阵效果
    /// </summary>
    public sealed partial class DotMatrixDigit : UserControl
    {
        // 数字宽度 = 列数 * (像素大小 + 间距) + 外边距
        // 数字高度 = 行数 * (像素大小 + 间距) + 外边距
        private const int PIXEL_SIZE = 8;        // 每个像素方块的大小（适当缩小以适应屏幕）
        private const int PIXEL_GAP = 3;         // 像素之间的间距（制造点阵感）
        private const int SEGMENT_GAP = 2;       // 段之间的间距
        private const int COLS = 6;              // 每段列数（水平段长度）
        private const int ROWS = 8;              // 每段行数（垂直段长度）
        private const int HORIZ_PADDING = 8;     // 左右内边距
        private const int VERT_PADDING = 8;      // 上下内边距

        // 像素步长（像素大小 + 间距）
        private static readonly double STEP = PIXEL_SIZE + PIXEL_GAP;

        // 7段位置定义（基于列/行坐标）
        // 水平段(A,G,D): 2行像素点阵，col=1~COLS, row=固定值~固定值+1
        // 垂直段(B,C,E,F): 2列像素点阵，col=固定值~固定值+1, row=范围
        // 网格: (COLS+4)列 x (ROWS*3+6)行 (每段占2行或2列，中间有1行/列间隙)
        // 段A: 顶部水平 (col=2~7, row=0~1)
        // 段B: 右上垂直 (col=8~9, row=2~9)
        // 段C: 右下垂直 (col=8~9, row=11~18)
        // 段D: 底部水平 (col=2~7, row=19~20)
        // 段E: 左下垂直 (col=0~1, row=11~18)
        // 段F: 左上垂直 (col=0~1, row=2~9)
        // 段G: 中间水平 (col=2~7, row=10~11)

        // 0-9 的 7 段显示表 (A,B,C,D,E,F,G)
        private static readonly bool[][] SEGMENT_MAP = new bool[][]
        {
            //            A      B      C      D      E      F      G
            new bool[] { true,  true,  true,  true,  true,  true,  false }, // 0
            new bool[] { false, true,  true,  false, false, false, false }, // 1
            new bool[] { true,  true,  false, true,  true,  false, true  }, // 2
            new bool[] { true,  true,  true,  true,  false, false, true  }, // 3
            new bool[] { false, true,  true,  false, false, true,  true  }, // 4
            new bool[] { true,  false, true,  true,  false, true,  true  }, // 5
            new bool[] { true,  false, true,  true,  true,  true,  true  }, // 6
            new bool[] { true,  true,  true,  false, false, false, false }, // 7
            new bool[] { true,  true,  true,  true,  true,  true,  true  }, // 8
            new bool[] { true,  true,  true,  true,  false, true,  true  }, // 9
        };

        /// <summary>
        /// 当前显示的数字
        /// </summary>
        public int Value
        {
            get { return (int)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(int), typeof(DotMatrixDigit),
                new PropertyMetadata(0, OnValueChanged));

        /// <summary>
        /// 亮的像素颜色
        /// </summary>
        public SolidColorBrush ActiveBrush
        {
            get { return (SolidColorBrush)GetValue(ActiveBrushProperty); }
            set { SetValue(ActiveBrushProperty, value); }
        }

        public static readonly DependencyProperty ActiveBrushProperty =
            DependencyProperty.Register("ActiveBrush", typeof(SolidColorBrush), typeof(DotMatrixDigit),
                new PropertyMetadata(new SolidColorBrush(Colors.White), OnAppearanceChanged));

        /// <summary>
        /// 暗的像素颜色（背景像素，微弱可见）
        /// </summary>
        public SolidColorBrush InactiveBrush
        {
            get { return (SolidColorBrush)GetValue(InactiveBrushProperty); }
            set { SetValue(InactiveBrushProperty, value); }
        }

        public static readonly DependencyProperty InactiveBrushProperty =
            DependencyProperty.Register("InactiveBrush", typeof(SolidColorBrush), typeof(DotMatrixDigit),
                new PropertyMetadata(new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)), OnAppearanceChanged));

        private List<Rectangle> _allPixels = new List<Rectangle>();

        public DotMatrixDigit()
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
            var control = (DotMatrixDigit)d;
            if (control._allPixels.Count > 0)
                control.UpdateDisplay();
        }

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (DotMatrixDigit)d;
            if (control._allPixels.Count > 0)
                control.UpdateDisplay();
        }

        /// <summary>
        /// 构建所有像素点（包括亮和暗的），一次性创建
        /// </summary>
        private void BuildPixelMatrix()
        {
            DigitCanvas.Children.Clear();
            _allPixels.Clear();

            // 总布局: (COLS+4)列 x (ROWS*3+6)行 的网格
            // col 0~1: 左垂直段 (F上, E下) - 2列
            // col 2~7: 水平段 (A, G, D) - COLS列
            // col 8~9: 右垂直段 (B上, C下) - 2列
            int totalCols = COLS + 4;
            int totalRows = ROWS * 3 + 6;

            for (int col = 0; col < totalCols; col++)
            {
                for (int row = 0; row < totalRows; row++)
                {
                    if (!IsSegmentPixel(col, row))
                        continue;

                    var rect = new Rectangle
                    {
                        Width = PIXEL_SIZE,
                        Height = PIXEL_SIZE,
                        RadiusX = 2,
                        RadiusY = 2,
                    };

                    Canvas.SetLeft(rect, HORIZ_PADDING + col * STEP);
                    Canvas.SetTop(rect, VERT_PADDING + row * STEP);

                    DigitCanvas.Children.Add(rect);
                    _allPixels.Add(rect);
                }
            }

            // 设置控件尺寸
            this.Width = HORIZ_PADDING * 2 + totalCols * STEP - PIXEL_GAP;
            this.Height = VERT_PADDING * 2 + totalRows * STEP - PIXEL_GAP;
        }

        /// <summary>
        /// 判断指定列/行位置是否属于 7 段中的某一段
        /// 水平段(A,G,D): 2行多列，col=2~COLS+1, row=段行~段行+1
        /// 垂直段(B,C,E,F): 2列多行，col=段列~段列+1, row=段范围
        /// </summary>
        private bool IsSegmentPixel(int col, int row)
        {
            // 段A: 顶部水平 col=2~7, row=0~1
            if (row >= 0 && row <= 1 && col >= 2 && col <= COLS + 1) return true;

            // 段B: 右上垂直 col=8~9, row=2~9
            if (col >= COLS + 2 && col <= COLS + 3 && row >= 2 && row <= ROWS + 1) return true;

            // 段C: 右下垂直 col=8~9, row=11~18
            if (col >= COLS + 2 && col <= COLS + 3 && row >= ROWS + 3 && row <= ROWS * 2 + 2) return true;

            // 段D: 底部水平 col=2~7, row=19~20
            if (row >= ROWS * 2 + 3 && row <= ROWS * 2 + 4 && col >= 2 && col <= COLS + 1) return true;

            // 段E: 左下垂直 col=0~1, row=11~18
            if (col >= 0 && col <= 1 && row >= ROWS + 3 && row <= ROWS * 2 + 2) return true;

            // 段F: 左上垂直 col=0~1, row=2~9
            if (col >= 0 && col <= 1 && row >= 2 && row <= ROWS + 1) return true;

            // 段G: 中间水平 col=2~7, row=10~11
            if (row >= ROWS + 2 && row <= ROWS + 3 && col >= 2 && col <= COLS + 1) return true;

            return false;
        }

        /// <summary>
        /// 获取像素所属的段索引 (0=A, 1=B, 2=C, 3=D, 4=E, 5=F, 6=G)
        /// 水平段用行判断优先，垂直段用列判断优先
        /// </summary>
        private int GetSegmentIndex(int col, int row)
        {
            // 段A: 顶部水平 col=2~7, row=0~1
            if (row >= 0 && row <= 1 && col >= 2 && col <= COLS + 1) return 0;

            // 段B: 右上垂直 col=8~9, row=2~9 (注意col判断先于G的行判断)
            if (col >= COLS + 2 && col <= COLS + 3 && row >= 2 && row <= ROWS + 1) return 1;

            // 段C: 右下垂直 col=8~9, row=11~18
            if (col >= COLS + 2 && col <= COLS + 3 && row >= ROWS + 3 && row <= ROWS * 2 + 2) return 2;

            // 段D: 底部水平 col=2~7, row=19~20
            if (row >= ROWS * 2 + 3 && row <= ROWS * 2 + 4 && col >= 2 && col <= COLS + 1) return 3;

            // 段E: 左下垂直 col=0~1, row=11~18
            if (col >= 0 && col <= 1 && row >= ROWS + 3 && row <= ROWS * 2 + 2) return 4;

            // 段F: 左上垂直 col=0~1, row=2~9
            if (col >= 0 && col <= 1 && row >= 2 && row <= ROWS + 1) return 5;

            // 段G: 中间水平 col=2~7, row=10~11
            if (row >= ROWS + 2 && row <= ROWS + 3 && col >= 2 && col <= COLS + 1) return 6;

            return -1;
        }

        /// <summary>
        /// 更新显示：根据 Value 点亮/熄灭对应段
        /// </summary>
        private void UpdateDisplay()
        {
            int digit = Value;
            if (digit < 0) digit = 0;
            if (digit > 9) digit = 9;

            bool[] segments = SEGMENT_MAP[digit];

            foreach (var rect in _allPixels)
            {
                double left = Canvas.GetLeft(rect);
                double top = Canvas.GetTop(rect);

                int col = (int)Math.Round((left - HORIZ_PADDING) / STEP);
                int row = (int)Math.Round((top - VERT_PADDING) / STEP);

                int segIdx = GetSegmentIndex(col, row);
                bool isActive = segIdx >= 0 && segments[segIdx];

                rect.Fill = isActive ? ActiveBrush : InactiveBrush;
            }
        }
    }
}
