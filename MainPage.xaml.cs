using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using MoooShowTime.Services;

namespace MoooShowTime
{
    /// <summary>
    /// 时钟主页面
    /// 横屏显示，数码管点阵像素风格，支持常亮/充电常亮模式
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private DispatcherTimer _clockTimer;
        private DispatcherTimer _hintTimer;
        private DisplayService _displayService;
        private BatteryService _batteryService;

        private bool _colonVisible = true;

        // 吃豆人动画
        private DispatcherTimer _pacManTimer;
        private double _pacManPos = 0;
        private double _pacManMouth = 0;
        private double _pacManMouthDir = 1;
        private double _pacManSpeed = 2.5;

        // 缓存的屏幕尺寸和路径常量（初始化后不变）
        private double _screenW, _screenH;
        private double _pathW, _pathH, _totalPath, _pathLen;

        // 缓存的变换对象（避免每帧分配）
        private RotateTransform _pacManRotate;

        // 豆子与幽灵
        private List<double> _dotPositions = new List<double>();
        private List<GhostObj> _ghosts = new List<GhostObj>();
        private Random _rng = new Random();
        private static readonly Color[] _ghostColors = new Color[]
        {
            Color.FromArgb(255, 0xFF, 0x00, 0x00),   // 红色幽灵
            Color.FromArgb(255, 0xFF, 0xB8, 0xFF),   // 粉色幽灵
            Color.FromArgb(255, 0x00, 0xFF, 0xFF),   // 青色幽灵
            Color.FromArgb(255, 0xFF, 0xB8, 0x52),   // 橙色幽灵
        };

        private class GhostObj
        {
            public double Pos;
            public bool Visible;
            public int RespawnTicks;
            public Canvas Element;
            public double Speed;
        }

        // 常亮模式：AlwaysOn（始终常亮）、ChargeOnly（仅充电常亮）
        private enum BrightMode
        {
            AlwaysOn,    // 始终常亮
            ChargeOnly   // 仅在充电时常亮
        }

        private BrightMode _currentMode = BrightMode.AlwaysOn;
        private bool _displayActive = false;

        // 颜色切换调色板（10色）
        private int _currentColorIndex = 0;
        private readonly Color[] _colorPalette = new Color[]
        {
            Color.FromArgb(255, 0x32, 0xDC, 0x64),   // 明亮绿
            Color.FromArgb(255, 0xFF, 0xFF, 0xFF),   // 白色
            Color.FromArgb(255, 0xDC, 0x26, 0x26),   // 红色
            Color.FromArgb(255, 0x25, 0x63, 0xEB),   // 蓝色
            Color.FromArgb(255, 0x22, 0xC5, 0x5E),   // 绿色
            Color.FromArgb(255, 0x00, 0xCE, 0xD1),   // 青色
            Color.FromArgb(255, 0xFF, 0x8C, 0x00),   // 琥珀色
            Color.FromArgb(255, 0x9B, 0x59, 0xB6),   // 紫色
            Color.FromArgb(255, 0xFF, 0x69, 0xB4),   // 粉色
            Color.FromArgb(255, 0xFF, 0xD7, 0x00),   // 金色
        };
        private readonly string[] _colorNames = new string[]
        {
            "明亮绿", "白色", "红色", "蓝色", "绿色",
            "青色", "琥珀色", "紫色", "粉色", "金色"
        };

        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;

            // 锁定横屏
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;

            _displayService = new DisplayService();
            _batteryService = new BatteryService();
            _batteryService.ChargingStateChanged += OnChargingStateChanged;

            // 隐藏状态栏（沉浸式全屏体验）
            var statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
            statusBar.HideAsync();

            // 初始化吃豆人动画
            InitPacMan();
            InitCachedValues();
            InitDots();
            InitGhosts();
            ApplyAccentColor();

            // 事件只订阅一次（NavigationCacheMode.Required 防止重复注册）
            DotMatrixPanel.Holding += ClockPanel_Holding;
            DotMatrixPanel.DoubleTapped += ClockPanel_DoubleTapped;
        }

        private void InitCachedValues()
        {
            _screenW = Window.Current.Bounds.Width;
            _screenH = Window.Current.Bounds.Height;
            const double margin = 35;
            _pathW = _screenW - 2 * margin;
            _pathH = _screenH - 2 * margin;
            _totalPath = 2 * (_pathW + _pathH);
            _pathLen = _totalPath;
            _pacManRotate = new RotateTransform { CenterX = 9, CenterY = 9 };
            PacMan.RenderTransform = _pacManRotate;
        }

        private void ApplyAccentColor()
        {
            // WP8.1 WinRT 的 UISettings 不支持 GetColorValue/UIColorType
            // 尝试从 XAML 资源字典读取，失败则使用默认 WP 蓝
            Color accentColor = Color.FromArgb(255, 0x1B, 0xA1, 0xE2); // 默认 WP 蓝
            try
            {
                var res = Application.Current.Resources;
                object val;
                if (res.TryGetValue("SystemAccentColor", out val) && val is Color)
                    accentColor = (Color)val;
                else if (res.TryGetValue("PhoneAccentColor", out val) && val is Color)
                    accentColor = (Color)val;
            }
            catch { }

            var accentBrush = new SolidColorBrush(accentColor);
            foreach (var child in DecorLayer.Children)
            {
                var rect = child as Rectangle;
                if (rect != null) rect.Fill = accentBrush;
            }
            TxtShowTime.Foreground = accentBrush;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _batteryService.Initialize();

            // 启动时钟定时器 - 每500ms更新一次（用于冒号闪烁）
            _clockTimer = new DispatcherTimer();
            _clockTimer.Interval = TimeSpan.FromMilliseconds(500);
            _clockTimer.Tick += OnClockTick;
            _clockTimer.Start();

            // 首次立即刷新
            UpdateClockDisplay();

            // 根据当前模式启用常亮
            ApplyBrightMode();

            // 提示文字几秒后渐隐
            _hintTimer = new DispatcherTimer();
            _hintTimer.Interval = TimeSpan.FromSeconds(5);
            _hintTimer.Tick += (s, args) =>
            {
                TxtHint.Opacity = 0;
                _hintTimer.Stop();
            };
            _hintTimer.Start();

            // 处理返回键 - 退出应用而不是返回
            Windows.Phone.UI.Input.HardwareButtons.BackPressed += OnBackPressed;

            // 启动吃豆人动画
            if (_pacManTimer != null) _pacManTimer.Start();
        }

        /// <summary>
        /// 返回键处理：退出应用
        /// </summary>
        private void OnBackPressed(object sender, Windows.Phone.UI.Input.BackPressedEventArgs e)
        {
            // 直接退出，不显示确认框
            e.Handled = true;
            Application.Current.Exit();
        }

        /// <summary>
        /// 时钟 Tick：切换冒号可见性并更新时间
        /// </summary>
        private void OnClockTick(object sender, object e)
        {
            _colonVisible = !_colonVisible;
            UpdateClockDisplay();
        }

        /// <summary>
        /// 更新时钟数字显示
        /// </summary>
        private void UpdateClockDisplay()
        {
            var now = DateTime.Now;

            int hour = now.Hour;
            int minute = now.Minute;

            DigitHourTens.Value = hour / 10;
            DigitHourOnes.Value = hour % 10;
            DigitMinuteTens.Value = minute / 10;
            DigitMinuteOnes.Value = minute % 10;
            ColonSeparator.IsVisible = _colonVisible;
        }

        /// <summary>
        /// 点击屏幕任意位置切换颜色
        /// </summary>
        private void Grid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _currentColorIndex = (_currentColorIndex + 1) % _colorPalette.Length;
            ApplyDigitColor(_colorPalette[_currentColorIndex]);
            TxtHint.Text = string.Format("✓ {0}", _colorNames[_currentColorIndex]);
            TxtHint.Opacity = 0.6;
            if (_hintTimer != null)
            {
                _hintTimer.Stop();
                _hintTimer.Start();
            }
        }

        private void ApplyDigitColor(Color color)
        {
            var activeBrush = new SolidColorBrush(color);
            var inactiveColor = Color.FromArgb(0x10, color.R, color.G, color.B);
            var inactiveBrush = new SolidColorBrush(inactiveColor);

            DigitHourTens.ActiveBrush = activeBrush;
            DigitHourTens.InactiveBrush = inactiveBrush;
            DigitHourOnes.ActiveBrush = activeBrush;
            DigitHourOnes.InactiveBrush = inactiveBrush;
            DigitMinuteTens.ActiveBrush = activeBrush;
            DigitMinuteTens.InactiveBrush = inactiveBrush;
            DigitMinuteOnes.ActiveBrush = activeBrush;
            DigitMinuteOnes.InactiveBrush = inactiveBrush;
            ColonSeparator.ActiveBrush = activeBrush;
            ColonSeparator.InactiveBrush = inactiveBrush;
        }

        // ==================== 吃豆人动画 ====================

        private void InitPacMan()
        {
            _pacManTimer = new DispatcherTimer();
            _pacManTimer.Interval = TimeSpan.FromMilliseconds(25);
            _pacManTimer.Tick += (s, e) => UpdatePacMan();
        }

        private void UpdatePacMan()
        {
            const double margin = 35;

            // 吃豆人移动
            _pacManPos += _pacManSpeed;

            // 吃豆子
            for (int i = _dotPositions.Count - 1; i >= 0; i--)
            {
                if (Math.Abs(_dotPositions[i] - _pacManPos) < 12)
                {
                    _dotPositions.RemoveAt(i);
                    if (i < DotsCanvas.Children.Count)
                        DotsCanvas.Children.RemoveAt(i);
                }
            }
            if (_dotPositions.Count == 0) InitDots();

            // 幽灵逻辑
            for (int i = 0; i < _ghosts.Count; i++)
            {
                var ghost = _ghosts[i];
                if (ghost.Visible)
                {
                    ghost.Pos += ghost.Speed;
                    if (ghost.Pos < 0) ghost.Pos += 1;
                    if (ghost.Pos >= 1) ghost.Pos -= 1;

                    double dist = Math.Abs(ghost.Pos * _pathLen - _pacManPos);
                    dist = Math.Min(dist, _pathLen - dist);
                    if (dist < 15)
                    {
                        _pacManSpeed = -_pacManSpeed;
                        ghost.Visible = false;
                        ghost.Element.Visibility = Visibility.Collapsed;
                        ghost.RespawnTicks = 2400;
                    }
                }
                else
                {
                    ghost.RespawnTicks--;
                    if (ghost.RespawnTicks <= 0)
                        RespawnGhost(ghost);
                }
            }

            // 更新幽灵显示位置
            UpdateGhostPositions();

            // 循环位置
            if (_pacManPos >= _totalPath) _pacManPos = 0;
            if (_pacManPos < 0) _pacManPos = _totalPath - 1;

            // 计算当前位置和朝向（使用缓存的路径常量）
            double x, y, rotation;
            double pos = _pacManPos;

            if (pos < _pathW)
            { x = margin + pos; y = margin; rotation = 0; }
            else if (pos < _pathW + _pathH)
            { x = _screenW - margin; y = margin + (pos - _pathW); rotation = 90; }
            else if (pos < 2 * _pathW + _pathH)
            { x = _screenW - margin - (pos - _pathW - _pathH); y = _screenH - margin; rotation = 180; }
            else
            { x = margin; y = _screenH - margin - (pos - 2 * _pathW - _pathH); rotation = 270; }

            if (_pacManSpeed < 0) rotation += 180;

            // 嘴部开合动画
            _pacManMouth += 0.18 * _pacManMouthDir;
            if (_pacManMouth > 1) { _pacManMouth = 1; _pacManMouthDir = -1; }
            if (_pacManMouth < 0) { _pacManMouth = 0; _pacManMouthDir = 1; }

            double openY = 2 + (7 * (1 - _pacManMouth));
            double closeY = 16 - (7 * (1 - _pacManMouth));
            PacManMouthTop.Points[1] = new Point(18, openY);
            PacManMouthBottom.Points[2] = new Point(18, closeY);

            // 更新位置和旋转（复用缓存的 RotateTransform，零分配）
            Canvas.SetLeft(PacMan, x - 9);
            Canvas.SetTop(PacMan, y - 9);
            _pacManRotate.Angle = rotation;
        }

        // ==================== 豆子与幽灵辅助方法 ====================

        private void InitDots()
        {
            DotsCanvas.Children.Clear();
            _dotPositions.Clear();
            int dotCount = (int)(_pathLen / 35);
            for (int i = 0; i < dotCount; i++)
            {
                double pos = i * 35;
                _dotPositions.Add(pos);
                double x, y;
                GetXY(pos, out x, out y);
                var dot = new Ellipse { Width = 3, Height = 3, Fill = new SolidColorBrush(Color.FromArgb(200, 0xFF, 0xD7, 0x00)) };
                Canvas.SetLeft(dot, x - 1.5);
                Canvas.SetTop(dot, y - 1.5);
                DotsCanvas.Children.Add(dot);
            }
        }

        private void InitGhosts()
        {
            GhostsCanvas.Children.Clear();
            _ghosts.Clear();
            for (int i = 0; i < _ghostColors.Length; i++)
            {
                var ghost = new GhostObj
                {
                    Pos = (i + 0.5) / _ghostColors.Length,
                    Visible = true,
                    RespawnTicks = 0,
                    Speed = (_rng.Next(2) == 0 ? 1 : -1) * 0.0004,
                    Element = CreateGhostElement(_ghostColors[i])
                };
                _ghosts.Add(ghost);
                GhostsCanvas.Children.Add(ghost.Element);
            }
            UpdateGhostPositions();
        }

        private Canvas CreateGhostElement(Color color)
        {
            var canvas = new Canvas { Width = 14, Height = 16, Opacity = 0.9 };
            // 幽灵身体
            var body = new Ellipse { Width = 14, Height = 10 };
            Canvas.SetTop(body, 0);
            body.Fill = new SolidColorBrush(color);
            canvas.Children.Add(body);
            // 幽灵裙子
            var skirt = new Rectangle { Width = 14, Height = 5, Fill = new SolidColorBrush(color) };
            Canvas.SetTop(skirt, 6);
            canvas.Children.Add(skirt);
            // 锯齿底边
            var teeth = new Polygon { Fill = new SolidColorBrush(color) };
            teeth.Points.Add(new Point(0, 11));
            teeth.Points.Add(new Point(3.5, 15));
            teeth.Points.Add(new Point(7, 11));
            teeth.Points.Add(new Point(10.5, 15));
            teeth.Points.Add(new Point(14, 11));
            teeth.Points.Add(new Point(14, 11));
            teeth.Points.Add(new Point(0, 11));
            canvas.Children.Add(teeth);
            // 左眼
            var eyeL = new Ellipse { Width = 4, Height = 4, Fill = new SolidColorBrush(Colors.White) };
            Canvas.SetLeft(eyeL, 2);
            Canvas.SetTop(eyeL, 3);
            canvas.Children.Add(eyeL);
            var pupilL = new Ellipse { Width = 2, Height = 2, Fill = new SolidColorBrush(Colors.Blue) };
            Canvas.SetLeft(pupilL, 3);
            Canvas.SetTop(pupilL, 4);
            canvas.Children.Add(pupilL);
            // 右眼
            var eyeR = new Ellipse { Width = 4, Height = 4, Fill = new SolidColorBrush(Colors.White) };
            Canvas.SetLeft(eyeR, 8);
            Canvas.SetTop(eyeR, 3);
            canvas.Children.Add(eyeR);
            var pupilR = new Ellipse { Width = 2, Height = 2, Fill = new SolidColorBrush(Colors.Blue) };
            Canvas.SetLeft(pupilR, 9);
            Canvas.SetTop(pupilR, 4);
            canvas.Children.Add(pupilR);
            return canvas;
        }

        private void GetXY(double pathPos, out double x, out double y)
        {
            const double margin = 35;
            double pos = pathPos % _totalPath;
            if (pos < 0) pos += _totalPath;

            if (pos < _pathW)
            { x = margin + pos; y = margin; }
            else if (pos < _pathW + _pathH)
            { x = _screenW - margin; y = margin + (pos - _pathW); }
            else if (pos < 2 * _pathW + _pathH)
            { x = _screenW - margin - (pos - _pathW - _pathH); y = _screenH - margin; }
            else
            { x = margin; y = _screenH - margin - (pos - 2 * _pathW - _pathH); }
        }

        private void RespawnGhost(GhostObj ghost)
        {
            double minDist = _pathLen * 0.25;
            double newPos;
            int attempts = 0;
            do
            {
                newPos = _rng.NextDouble();
                double dist = Math.Abs(newPos * _pathLen - _pacManPos);
                dist = Math.Min(dist, _pathLen - dist);
                if (dist > minDist || attempts++ > 20) break;
            } while (true);

            ghost.Pos = newPos;
            ghost.Visible = true;
            ghost.Element.Visibility = Visibility.Visible;
            ghost.Speed = (_rng.Next(2) == 0 ? 1 : -1) * 0.0004;
        }

        private void UpdateGhostPositions()
        {
            foreach (var ghost in _ghosts)
            {
                if (ghost.Visible && ghost.Element != null)
                {
                    double x, y;
                    GetXY(ghost.Pos * _pathLen, out x, out y);
                    Canvas.SetLeft(ghost.Element, x - 7);
                    Canvas.SetTop(ghost.Element, y - 8);
                }
            }
        }

        /// <summary>
        /// 充电状态变化回调
        /// </summary>
        private void OnChargingStateChanged(object sender, bool isCharging)
        {
            // 在 UI 线程更新
            var ignored = this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                UpdateChargingUI(isCharging);

                // 如果是 ChargeOnly 模式，根据充电状态切换常亮
                if (_currentMode == BrightMode.ChargeOnly)
                {
                    if (isCharging && !_displayActive)
                    {
                        _displayService.RequestKeepScreenOn();
                        _displayActive = true;
                    }
                    else if (!isCharging && _displayActive)
                    {
                        _displayService.ReleaseKeepScreenOn();
                        _displayActive = false;
                    }
                }
            });
        }

        /// <summary>
        /// 更新充电状态 UI
        /// </summary>
        private void UpdateChargingUI(bool isCharging)
        {
            TxtChargingStatus.Visibility = isCharging ? Visibility.Visible : Visibility.Collapsed;
            TxtChargingStatus.Text = isCharging ? "⚡" : "";
            TxtBatteryPercent.Text = string.Format("{0}%", _batteryService.BatteryPercent);
        }

        /// <summary>
        /// 应用当前常亮模式
        /// </summary>
        private void ApplyBrightMode()
        {
            if (_currentMode == BrightMode.AlwaysOn)
            {
                if (!_displayActive)
                {
                    _displayService.RequestKeepScreenOn();
                    _displayActive = true;
                }
                TxtHint.Text = "始终常亮模式 | 长按屏幕可切换";
            }
            else
            {
                // ChargeOnly 模式：根据当前充电状态决定
                if (_batteryService.IsCharging)
                {
                    if (!_displayActive)
                    {
                        _displayService.RequestKeepScreenOn();
                        _displayActive = true;
                    }
                    TxtHint.Text = "充电常亮模式 | 当前正在充电 | 长按可切换";
                }
                else
                {
                    if (_displayActive)
                    {
                        _displayService.ReleaseKeepScreenOn();
                        _displayActive = false;
                    }
                    TxtHint.Text = "充电常亮模式 | 未充电屏幕会关闭 | 长按可切换";
                }
            }

            UpdateChargingUI(_batteryService.IsCharging);
        }

        /// <summary>
        /// 长按屏幕切换常亮模式
        /// </summary>
        private void ClockPanel_Holding(object sender, HoldingRoutedEventArgs e)
        {
            if (e.HoldingState == Windows.UI.Input.HoldingState.Started)
            {
                SwitchBrightMode();
            }
        }

        /// <summary>
        /// 双击切换模式（作为长按的替代）
        /// </summary>
        private void ClockPanel_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            SwitchBrightMode();
        }

        private void SwitchBrightMode()
        {
            _currentMode = _currentMode == BrightMode.AlwaysOn
                ? BrightMode.ChargeOnly
                : BrightMode.AlwaysOn;

            ApplyBrightMode();

            // 显示模式切换提示
            ShowModeToast();
        }

        private void ShowModeToast()
        {
            string msg = _currentMode == BrightMode.AlwaysOn
                ? "✓ 切换为【始终常亮】"
                : "✓ 切换为【充电常亮】";

            TxtHint.Text = msg;
            TxtHint.Opacity = 0.6;
            if (_hintTimer != null)
            {
                _hintTimer.Stop();
                _hintTimer.Start();
            }
        }

        /// <summary>
        /// 页面离开时清理资源
        /// </summary>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Cleanup();
            base.OnNavigatedFrom(e);
        }

        private void Cleanup()
        {
            if (_clockTimer != null)
            {
                _clockTimer.Stop();
                _clockTimer = null;
            }

            if (_hintTimer != null)
            {
                _hintTimer.Stop();
                _hintTimer = null;
            }

            if (_pacManTimer != null)
            {
                _pacManTimer.Stop();
                _pacManTimer = null;
            }

            _displayService?.ReleaseAll();
            _displayActive = false;

            _batteryService?.Cleanup();

            Windows.Phone.UI.Input.HardwareButtons.BackPressed -= OnBackPressed;
            DotMatrixPanel.Holding -= ClockPanel_Holding;
            DotMatrixPanel.DoubleTapped -= ClockPanel_DoubleTapped;
        }
    }
}
