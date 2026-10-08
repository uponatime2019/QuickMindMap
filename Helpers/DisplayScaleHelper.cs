using System;
using Microsoft.UI.Xaml;

namespace QuickMindMap.Helpers
{
    public static class DisplayScaleHelper
    {
        private static double _cachedScaleRatio = 1.0;

        public static double CurrentScaleRatio
        {
            get => _cachedScaleRatio > 0 ? _cachedScaleRatio : 1.0;
            set
            {
                if (value > 0) _cachedScaleRatio = value;
            }
        }

        public static double GetDesktopScalingRatio(XamlRoot? xamlRoot = null)
        {
            if (xamlRoot != null && xamlRoot.RasterizationScale > 0)
            {
                _cachedScaleRatio = xamlRoot.RasterizationScale;
                return _cachedScaleRatio;
            }

            try
            {
                if (Application.Current is App app && app.MainWindow != null)
                {
                    double ratio = WindowHelper.GetScaleRatio(app.MainWindow);
                    if (ratio > 0)
                    {
                        _cachedScaleRatio = ratio;
                        return ratio;
                    }
                }
            }
            catch { }

            return _cachedScaleRatio > 0 ? _cachedScaleRatio : 1.0;
        }
    }
}
