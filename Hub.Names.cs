// ============================================================================
// ModSettingsHub — 显示名（中文）与过滤
//   自动 cfg 页的"名字"来自 MelonPreferences 分类 identifier（英文），这里统一映射成中文。
//   没命中的保持原名；第三方 mod 也尽量给个中文名。
// ============================================================================
using System;
using System.Collections.Generic;
using MelonLoader;

namespace ModSettingsHub
{
    public partial class ModSettingsHubMod
    {
        // 常见字段整句（优先）
        static readonly Dictionary<string, string> FieldSentences = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Enable unlimited text", "启用无限文本" },
            { "Verbose logging",       "详细日志" },
            { "Item panel width",      "物品面板宽度" },
            { "Item panel height",     "物品面板高度" },
            { "Zone panel width",      "区域面板宽度" },
            { "Zone panel height",     "区域面板高度" },
            { "Panel X",               "面板 X" },
            { "Panel Y",               "面板 Y" },
        };

        // ★identifier → 中文（精确，优先）。MelonPreferences 的 DisplayName 在本作多数 mod 里
        //   等于**说明句**（QualityPlus 就是这样），直接拿来当标签会"挤一堆字"，所以一律按 identifier。
        static readonly Dictionary<string, string> IdentCn = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // ---- QualityPlus ----
            { "Level", "画质档位" },
            { "SSAO_On", "SSAO 开关" },
            { "SSAO_Intensity", "SSAO 强度" },
            { "SSAO_Radius", "SSAO 半径" },
            { "SSAO_Source", "SSAO 深度源" },
            { "SSAO_AfterOpaque", "SSAO 叠加方式" },
            { "RenderScale", "渲染倍率" },
            { "MSAA", "MSAA 抗锯齿" },
            { "ShadowDistance", "阴影距离" },
            { "ShadowCascades", "阴影级联" },
            { "HDR", "HDR" },
            { "Bloom", "泛光" },
            { "Vignette", "暗角" },
            { "Saturation", "饱和度" },
            { "Contrast", "对比度" },
            { "Tonemapping", "色调映射" },
            { "AvatarCastShadows", "角色投射阴影" },
            { "AvatarReceiveShadows", "角色接收阴影" },
            { "MainLightShadows", "主光阴影" },
            { "MainLightReachAvatars", "主光照亮角色" },
            { "MainLightReachAvatars2", "主光照亮角色" },
            { "ShadowStrength", "阴影强度" },
            { "ShadowResolution", "阴影分辨率" },
            { "SoftShadows", "软阴影" },
            { "TextureFullResolution", "贴图全分辨率" },
            { "QualityLevelUltra", "画质档 Ultra" },
            { "TimeSun", "时间阳光" },
            { "TimeSunManual", "时间来源：手动" },
            { "TimeSunHour", "手动时间" },
            { "TimeSunPanel", "时间面板" },
            { "TimeSunPanelX", "时间面板 X" },
            { "TimeSunPanelY", "时间面板 Y" },
            { "TimeSunSource", "时间来源" },
            { "TimeSunSky", "天空随时段" },
            { "TimeSunDim", "天空明暗" },
            { "TimeSunSpeed", "时间流速" },
            { "TimeSunFlow", "时间流动" },
            { "Fog_On", "灯光雾开关" },
            { "Fog_Brightness", "灯光雾亮度" },
            { "Fog_Density", "灯光雾浓度" },
            { "Fog_Penumbra", "边缘柔度" },
            { "Fog_Range", "光晕大小" },
            { "Fog_Warmth", "灯光冷暖" },
            { "Fog_Range2", "光晕大小（第二段）" },
            { "Fog_Diffusion", "逆光增亮" },
            { "Fog_LampLight", "补光强度" },
            { "Fog_LampRange", "补光范围" },
            { "Fog_LampOn", "补光开关" },
            { "Fog_Shadow", "光罩阴影" },
            { "Fog_Shadow2", "光罩阴影（第二段）" },
            { "Room_On", "房间光开关" },
            { "Room_Lum", "房间光亮度" },
            { "Room_Y", "房间光 Y 偏移" },
            { "Room_Yaw", "房间光左右角度" },
            { "Room_Pitch", "房间光俯仰" },
            { "Room_Warmth", "房间光冷暖" },
            { "Room_FogOn", "光柱雾开关" },
            { "Room_FogBright", "光柱亮度" },
            { "Room_FogDensity", "光柱浓度" },
            { "Room_Range", "房间光范围" },
            { "Room_X", "房间光 X 偏移" },
            { "Room_Z", "房间光 Z 偏移" },
            { "Fog_AddLight", "额外补光" },
            { "AvatarLight_Mode", "角色光模式" },
            { "AvatarLight_Min", "角色光保底" },
            { "Fog_FillOn", "填充补光开关" },
            { "Fog_FillLum", "填充补光亮度" },
            { "Fog_FillRange", "填充补光范围" },
            { "Fog_FillWarmth", "填充补光冷暖" },

            // ---- 其它 mod（截图里点过名的） ----
            { "MaxSubAvatars", "最大子头像数" },
            { "RescanSeconds", "重新扫描间隔" },
            { "MaxRescanSeconds", "最大重扫间隔" },
            { "WatchdogSeconds", "看门狗间隔" },
            { "PositionEpsilon", "位置误差" },
            { "RotationEpsilonDeg", "旋转误差（度）" },
            { "LogStats", "统计日志" },
            { "AnimatorCullMode", "动画剔除模式" },
            { "PanelScale", "面板缩放" },
            { "ItemW", "物品面板宽度" },
            { "ItemH", "物品面板高度" },
            { "ZoneW", "区域面板宽度" },
            { "ZoneH", "区域面板高度" },
            { "PanelBottomX", "面板 X" },
            { "PanelBottomY", "面板 Y" },
            { "ButtonOffsetX", "按钮偏移 X" },
            { "ButtonOffsetY", "按钮偏移 Y" },
            { "Online", "在线词典" },
            { "UseBuiltinDict", "内置词典" },
            { "OnlyPlayerContent", "只翻译玩家内容" },
            { "ShowOriginal", "显示原文" },
            { "EnableInputPanel", "中文输入面板" },
            { "PillExtraLeft", "药丸向左延长" },
            { "ButtonMoved", "翻译按钮已移动" },
            { "TRPanelX", "转换面板 X" },
            { "TRPanelY", "转换面板 Y" },
            { "TRPanelW", "转换面板宽度" },
            { "TRPanelH", "转换面板高度" },
            { "TRPanelVisible", "转换面板常驻" },
            { "TRBtnX", "转换按钮 X" },
            { "TRBtnY", "转换按钮 Y" },
            { "TRBtnMoved", "转换按钮已移动" },
            { "RecordPanelX", "记录面板 X" },
            { "RecordPanelY", "记录面板 Y" },
            { "CastWidth", "渲染宽度" },
            { "CastHeight", "渲染高度" },
            { "CustomLimit", "放置上限" },
            { "HiddenFps", "后台帧率" },
            { "HiddenRenderScale", "后台渲染倍率" },
            { "DropMsaaAndShadows", "后台关 MSAA/阴影" },
            { "TextureMipmapLimit", "后台贴图 mip 限制" },
            { "ReleaseMemory", "释放内存" },
            { "MemoryReleaseDelaySeconds", "内存释放延迟" },
            { "SquareCrop", "方形裁剪" },
            { "PanelW", "面板宽度" },
            { "PanelH", "面板高度" },
            { "PanelVisible", "面板显示" },
            { "Fill", "填充比例" },
            { "SettleSeconds", "等待秒数" },
            { "ToggleFrame", "内部：F2 去抖帧" },
            { "ToggleTime", "内部：F2 去抖时间" },
            { "X", "面板 X" },
            { "Y", "面板 Y" },

            // ---- UnityExplorer（identifier 自带空格） ----
            { "UnityExplorer Toggle", "开关快捷键" },
            { "Hide On Startup", "启动时隐藏" },
            { "Startup Delay Time", "启动延迟（秒）" },
            { "Target Display", "目标显示器" },
            { "Force Unlock Mouse", "强制解锁鼠标" },
            { "Force Unlock Toggle Key", "强制解锁快捷键" },
            { "Disable EventSystem override", "禁用 EventSystem 覆盖" },
            { "Default Output Path", "默认输出路径" },
            { "dnSpy Path", "dnSpy 路径" },
            { "Main Navbar Anchor", "主导航栏位置" },
            { "Log Unity Debug", "记录 Unity 调试日志" },
            { "Log To Disk", "日志写入磁盘" },
            { "World Mouse-Inspect Keybind", "世界鼠标检查快捷键" },
            { "UI Mouse-Inspect Keybind", "UI 鼠标检查快捷键" },
            { "CSharp Console Assembly Blacklist", "C# 控制台程序集黑名单" },
            { "Member Signature Blacklist", "成员签名黑名单" },
            { "TimeScale Toggle", "时间缩放切换键" },
            { "Pause Keybind", "暂停快捷键" },
            { "Playback Keybind", "播放快捷键" },
            { "Speed-Down Keybind", "减速快捷键" },
            { "Speed-Up Keybind", "加速快捷键" },
        };


        // 通用词（按词边界替换 / identifier 拆词兜底）
        static readonly (string en, string cn)[] FieldWords = new (string, string)[]
        {
            ("Enable", "启用"), ("Enabled", "启用"), ("Disable", "禁用"), ("Unlimited", "无限"), ("Text", "文本"),
            ("Logging", "日志"), ("Verbose", "详细"), ("Panel", "面板"), ("Item", "物品"), ("Items", "物品"),
            ("Zone", "区域"), ("Width", "宽度"), ("Height", "高度"), ("Position", "位置"),
            ("Rotation", "旋转"), ("Scale", "缩放"), ("Size", "大小"), ("Speed", "速度"),
            ("Opacity", "不透明度"), ("Volume", "音量"), ("Color", "颜色"), ("Colour", "颜色"), ("Range", "范围"),
            ("Intensity", "强度"), ("Distance", "距离"), ("Offset", "偏移"), ("Time", "时间"),
            ("Font", "字体"), ("Window", "窗口"), ("Mode", "模式"), ("Camera", "相机"),
            ("Room", "房间"), ("Avatar", "角色"), ("Avatars", "角色"), ("Furniture", "家具"), ("Light", "灯光"),
            ("Lighting", "灯光"), ("Lights", "灯光"), ("Shadow", "阴影"), ("Shadows", "阴影"),
            ("Quality", "画质"), ("Frame", "帧"), ("Rate", "率"), ("Fps", "帧率"),
            ("Max", "最大"), ("Min", "最小"), ("Default", "默认"), ("Show", "显示"),
            ("Hide", "隐藏"), ("Open", "打开"), ("Close", "关闭"), ("Toggle", "切换"),
            ("Delay", "延迟"), ("Seconds", "秒"), ("Second", "秒"), ("Startup", "启动"),
            ("Display", "显示器"), ("Path", "路径"), ("Output", "输出"), ("Mouse", "鼠标"),
            ("Unlock", "解锁"), ("Event", "事件"), ("System", "系统"), ("Override", "覆盖"),
            ("Debug", "调试"), ("Disk", "磁盘"), ("Console", "控制台"), ("Assembly", "程序集"),
            ("Blacklist", "黑名单"), ("Signature", "签名"), ("Button", "按钮"), ("Moved", "已移动"),
            ("Persistent", "常驻"), ("Visible", "显示"), ("Online", "在线"), ("Builtin", "内置"),
            ("Dictionary", "词典"), ("Only", "只"), ("Player", "玩家"), ("Content", "内容"),
            ("Original", "原文"), ("Input", "输入"), ("Record", "记录"), ("Cast", "投屏"),
            ("Fill", "填充"), ("Settle", "等待"), ("Crop", "裁剪"), ("Square", "方形"),
            ("Memory", "内存"), ("Release", "释放"), ("Drop", "关闭"), ("Texture", "贴图"),
            ("Mipmap", "mip"), ("Limit", "上限"), ("Custom", "自定义"), ("Static", "静态"),
            ("Constraints", "约束"), ("Rescan", "重扫"), ("Watchdog", "看门狗"),
            ("Epsilon", "误差"), ("Deg", "度"), ("Stats", "统计"), ("Animator", "动画"),
            ("Cull", "剔除"), ("Overlay", "浮层"), ("Level", "档位"), ("Source", "来源"),
            ("Sub", "子"), ("Placeable", "可放置"), ("Placement", "放置"), ("Placed", "放置"),
            ("Lum", "亮度"), ("Bright", "亮度"), ("Brightness", "亮度"), ("Density", "浓度"),
            ("Warmth", "冷暖"), ("Diffusion", "逆光"), ("Penumbra", "柔度"), ("Beams", "光柱"),
            ("Ambient", "环境"), ("Centre", "中央"), ("Center", "中央"), ("Multiplier", "倍率"),
            ("Main", "主"), ("Reach", "照亮"), ("Strength", "强度"), ("Resolution", "分辨率"),
            ("Soft", "软"), ("Panel", "面板"), ("Bottom", "底部"), ("Top", "顶部"),
            ("Left", "左"), ("Right", "右"), ("Screen", "屏幕"), ("Anchor", "锚点"),
            ("Keybind", "快捷键"), ("Key", "键"), ("Value", "值"), ("Mode", "模式"),
            ("Fast", "快速"), ("Slow", "慢"), ("Up", "上"), ("Down", "下"),
        };

        internal static string ChineseIdentifier(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id)) return "";
                string exact;
                if (IdentCn.TryGetValue(id, out exact)) return exact;

                // 下划线/连字符当分隔，再按驼峰切词
                string t = id.Replace('_', ' ').Replace('-', ' ').Trim();
                var spaced = new System.Text.StringBuilder(t.Length + 8);
                for (int i = 0; i < t.Length; i++)
                {
                    char c = t[i];
                    if (i > 0 && c != ' ' && t[i - 1] != ' ')
                    {
                        bool boundary = char.IsUpper(c) && !char.IsUpper(t[i - 1]);
                        if (!boundary && char.IsDigit(c) && !char.IsDigit(t[i - 1])) boundary = true;
                        if (boundary) spaced.Append(' ');
                    }
                    spaced.Append(c);
                }

                var parts = spaced.ToString().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var sb = new System.Text.StringBuilder();
                bool lastCn = false;
                for (int i = 0; i < parts.Length; i++)
                {
                    string w = parts[i];
                    string cn = w;
                    for (int k = 0; k < FieldWords.Length; k++)
                    {
                        if (string.Equals(FieldWords[k].en, w, StringComparison.OrdinalIgnoreCase)) { cn = FieldWords[k].cn; break; }
                    }
                    bool isCn = HasChinese(cn);
                    if (sb.Length > 0 && !(isCn && lastCn)) sb.Append(' ');
                    sb.Append(cn);
                    lastCn = isCn;
                }
                string result = sb.ToString().Trim();
                return result.Length > 0 ? result : id;
            }
            catch { return id; }
        }

        static bool HasChinese(string s)
        {
            try
            {
                if (string.IsNullOrEmpty(s)) return false;
                for (int i = 0; i < s.Length; i++)
                    if (s[i] >= 0x4E00 && s[i] <= 0x9FFF) return true;
            }
            catch { }
            return false;
        }

        // 不显示的内部键（布局/缓存/测试），改了只会把面板搞乱
        static bool IsInternalEntry(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id)) return true;
                if (id.StartsWith("Layout_", StringComparison.OrdinalIgnoreCase)) return true;
                if (id.Equals("Fog_Params", StringComparison.OrdinalIgnoreCase)) return true;
                if (id.Equals("ToggleFrame", StringComparison.OrdinalIgnoreCase)) return true;
                if (id.Equals("ToggleTime", StringComparison.OrdinalIgnoreCase)) return true;
                if (id.Equals("Custom", StringComparison.OrdinalIgnoreCase)) return true;
                if (id.Equals("SafeMode", StringComparison.OrdinalIgnoreCase)) return true;
                return false;
            }
            catch { return false; }
        }


        internal static string TranslateField(string s)
        {
            try
            {
                if (string.IsNullOrEmpty(s)) return s;
                string t;
                if (FieldSentences.TryGetValue(s.Trim(), out t)) return t;
                t = s;
                for (int i = 0; i < FieldWords.Length; i++)
                {
                    t = System.Text.RegularExpressions.Regex.Replace(
                        t,
                        "\\b" + System.Text.RegularExpressions.Regex.Escape(FieldWords[i].en) + "\\b",
                        FieldWords[i].cn,
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                }
                return t;
            }
            catch { return s; }
        }
        // 顺序 = 匹配优先级（先精确，再关键词）
        static readonly string[][] NameMap = new string[][]
        {
            new [] { "qualityplus",        "画质增强" },
            new [] { "roomitemtransformui","家具调整" },
            new [] { "roomitemclipboard",  "家具复制" },
            new [] { "ingametranslate",    "游戏翻译" },
            new [] { "multihair",          "多发型" },
            new [] { "multislots",         "多凹槽" },
            new [] { "roomsnapfilter",     "拍照滤镜" },
            new [] { "graffitiundo",       "画布撤回" },
            new [] { "graffitilog",        "涂鸦日志" },
            new [] { "graffiti log",       "涂鸦日志" },
            new [] { "codecor",            "联机装修" },
            new [] { "syncownerhud",       "同步者显示" },
            new [] { "roomownertransfer",  "房主转交" },
            new [] { "roomavatarlimit",    "子头像上限" },
            new [] { "avatarstickerlimit", "贴纸上限" },
            new [] { "customfurniture",    "自定义家具" },
            new [] { "stackperf",          "性能优化" },
            new [] { "backgroundsaver",    "后台省电" },
            new [] { "manualroomid",       "房间号" },
            new [] { "avatardirector",     "表情导演" },
            new [] { "furnituresticker",   "家具贴纸" },
            new [] { "customsticker",      "衣服贴纸" },
            new [] { "unityexplorer",      "场景探索器" },
            new [] { "unlimitedfurniture", "无限家具" },
            new [] { "bettercamera",       "相机增强" },
            new [] { "watchpartyextension","一起看" },
            new [] { "goghunlimitedtext",  "文本不限" },
            new [] { "xingmang",           "通用设置" },
        };

        // 不显示的（探针/内部）
        static bool HideCategory(string id)
        {
            if (string.IsNullOrEmpty(id)) return true;
            if (id.Equals("SettingsProbe", StringComparison.OrdinalIgnoreCase)) return true;
            if (id.EndsWith("Probe", StringComparison.OrdinalIgnoreCase)) return true;
            if (id.Equals("ModSettingsHub", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        internal static string ChineseName(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id)) return "";
                string low = id.ToLowerInvariant().Replace("_", " ").Replace("-", " ");
                for (int i = 0; i < NameMap.Length; i++)
                {
                    if (low.IndexOf(NameMap[i][0], StringComparison.Ordinal) >= 0) return NameMap[i][1];
                }
            }
            catch { }
            return id;
        }

        // 图标上的字：中文取第一个字，英文只取首字母（用户："里面只放 mod 的第一个字"）
        internal static string IconGlyph(string name)
        {
            try
            {
                if (string.IsNullOrEmpty(name)) return "?";
                char c0 = name[0];
                if (c0 >= 0x4E00 && c0 <= 0x9FFF) return name.Substring(0, 1);
                return char.ToUpperInvariant(c0).ToString();
            }
            catch { return "?"; }
        }
    }
}
