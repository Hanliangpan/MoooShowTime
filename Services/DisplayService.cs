using System;
using Windows.System.Display;

namespace MoooShowTime.Services
{
    /// <summary>
    /// 屏幕常亮管理服务
    /// 使用 DisplayRequest API 防止屏幕自动关闭
    /// 注意：DisplayRequest 是累积性的，每次 RequestActive 必须对应 RequestRelease
    /// </summary>
    public class DisplayService
    {
        private DisplayRequest _displayRequest;
        private int _requestCount;
        private bool _isActive;

        /// <summary>
        /// 屏幕常亮是否已激活
        /// </summary>
        public bool IsActive => _isActive;

        /// <summary>
        /// 请求屏幕保持常亮
        /// </summary>
        public void RequestKeepScreenOn()
        {
            try
            {
                if (_displayRequest == null)
                {
                    _displayRequest = new DisplayRequest();
                }

                _displayRequest.RequestActive();
                _requestCount++;
                _isActive = true;

                System.Diagnostics.Debug.WriteLine("DisplayRequest: Active (count={0})", _requestCount);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("DisplayRequest failed: {0}", ex.Message);
                _isActive = false;
            }
        }

        /// <summary>
        /// 释放屏幕常亮请求
        /// </summary>
        public void ReleaseKeepScreenOn()
        {
            try
            {
                if (_displayRequest != null && _requestCount > 0)
                {
                    _displayRequest.RequestRelease();
                    _requestCount--;
                }

                _isActive = _requestCount > 0;

                System.Diagnostics.Debug.WriteLine("DisplayRequest: Released (count={0})", _requestCount);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("DisplayRequest release failed: {0}", ex.Message);
                // 如果释放失败，重置计数
                _requestCount = 0;
                _isActive = false;
            }
        }

        /// <summary>
        /// 完全释放所有请求并重置
        /// </summary>
        public void ReleaseAll()
        {
            while (_requestCount > 0)
            {
                ReleaseKeepScreenOn();
            }
            _displayRequest = null;
        }
    }
}
