using System;
using Windows.Phone.Devices.Power;

namespace MoooShowTime.Services
{
    /// <summary>
    /// 电池/充电状态监听服务
    /// 通过监听电量变化间接判断充电状态
    /// </summary>
    public class BatteryService
    {
        private Battery _battery;
        private int _lastPercent = -1;
        private bool _isCharging;

        /// <summary>
        /// 是否正在充电
        /// </summary>
        public bool IsCharging
        {
            get { return _isCharging; }
            private set
            {
                if (_isCharging != value)
                {
                    _isCharging = value;
                    ChargingStateChanged?.Invoke(this, value);
                }
            }
        }

        /// <summary>
        /// 当前电量百分比 (0-100)
        /// </summary>
        public int BatteryPercent { get; private set; }

        /// <summary>
        /// 充电状态变化事件
        /// </summary>
        public event EventHandler<bool> ChargingStateChanged;

        /// <summary>
        /// 初始化电池服务
        /// </summary>
        public void Initialize()
        {
            try
            {
                _battery = Battery.GetDefault();
                if (_battery != null)
                {
                    BatteryPercent = _battery.RemainingChargePercent;
                    _lastPercent = BatteryPercent;

                    _battery.RemainingChargePercentChanged += OnBatteryPercentChanged;

                    System.Diagnostics.Debug.WriteLine("BatteryService: Initialized, battery={0}%", BatteryPercent);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("BatteryService init failed: {0}", ex.Message);
            }
        }

        private void OnBatteryPercentChanged(object sender, object e)
        {
            try
            {
                if (_battery == null) return;

                int currentPercent = _battery.RemainingChargePercent;
                System.Diagnostics.Debug.WriteLine("BatteryService: {0}% -> {1}%", _lastPercent, currentPercent);

                // 电量上升 → 正在充电
                if (currentPercent > _lastPercent)
                {
                    IsCharging = true;
                }
                // 电量下降 → 正在放电
                else if (currentPercent < _lastPercent)
                {
                    IsCharging = false;
                }
                // 电量不变 → 保持之前的状态（可能是充满或刚插拔）

                BatteryPercent = currentPercent;
                _lastPercent = currentPercent;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("BatteryService update failed: {0}", ex.Message);
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            if (_battery != null)
            {
                _battery.RemainingChargePercentChanged -= OnBatteryPercentChanged;
                _battery = null;
            }
        }
    }
}
