using UnityEngine;

namespace ChanFangVR
{
    /// 全局中文字体：优先系统已装的中文字体，保证 UI 中文可正常渲染。
    public static class PrologueFont
    {
        private static Font _cached;
        private static bool _searched;

        private static readonly string[] Prefs =
        {
            "Microsoft YaHei", "Microsoft YaHei UI", "微软雅黑",
            "DengXian", "等线",
            "SimHei", "黑体",
            "SimSun", "宋体", "NSimSun", "新宋体",
            "KaiTi", "楷体", "FangSong", "仿宋",
            "Source Han Sans SC", "Noto Sans CJK SC", "Noto Sans SC",
            "Microsoft JhengHei", "PingFang SC",
        };

        public static Font Get()
        {
            if (_cached != null) return _cached;
            if (_searched) return Fallback();
            _searched = true;

            try
            {
                string[] names = Font.GetOSInstalledFontNames();
                if (names != null)
                {
                    for (int i = 0; i < Prefs.Length; i++)
                    {
                        for (int j = 0; j < names.Length; j++)
                        {
                            if (string.Equals(names[j], Prefs[i], System.StringComparison.OrdinalIgnoreCase))
                            {
                                _cached = Font.CreateDynamicFontFromOSFont(names[j], 32);
                                if (_cached != null) return _cached;
                            }
                        }
                    }
                }
            }
            catch
            {
                // 某些平台不支持枚举系统字体，直接走兜底
            }

            return Fallback();
        }

        /// 兜底：Unity 内置字体。不含中文字形时中文会显示为方框，
        /// 此时请把一个中文 TTF 放到 Assets/Resources/Fonts/ 下并命名为 CJK。
        private static Font Fallback()
        {
            if (_cached != null) return _cached;

            var custom = Resources.Load<Font>("Fonts/CJK");
            if (custom != null)
            {
                _cached = custom;
                return _cached;
            }

            try { _cached = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch { _cached = null; }

            if (_cached == null)
            {
                try { _cached = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
                catch { _cached = null; }
            }
            return _cached;
        }
    }
}
