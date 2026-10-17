// ============================================================================
// ModSettingsHub.Api — 共享契约（唯一一份，放 <游戏>\UserLibs\，不要放 Mods）
//
// 目的：其他 mod 引用这个 DLL，把"自己的设置页"注册进 ModSettingsHub。
//       - 复杂/需要实时的设置：实现 IModSettingsPage，自己 Build 页面；
//       - 已经有自己面板的 mod：实现 IModSettingsEntry，只给一颗入口，点了打开自己的面板；
//       - 什么都不做的 mod：Hub 自动扫描 MelonPreferences 生成基础设置页。
//
// ★为什么必须放 UserLibs：MelonLoader 会把 UserLibs 里的 DLL 当作"引用"加载，
//   所有 mod 共享同一份程序集 ⇒ 静态注册表才不会被分裂成多份。
// ★各 mod 引用时必须 Private=false（不要把这份 DLL 复制进 Mods）。
// ============================================================================
using System;
using System.Collections.Generic;
using UnityEngine;
using MelonLoader;

namespace ModSettingsHub.Api
{
    /// <summary>由 mod 自己构建的一页设置（内容区父节点已由 Hub 建好）。</summary>
    public interface IModSettingsPage
    {
        /// <summary>唯一 id（建议 = mod 名）。</summary>
        string Id { get; }
        /// <summary>显示名（图标下面/标题栏）。</summary>
        string DisplayName { get; }
        /// <summary>64x64 PNG 字节；可为 null（Hub 用默认图标）。</summary>
        byte[] IconPng { get; }
        /// <summary>在内容区里建自己的控件。调用时机 = 用户点开这一页。</summary>
        void Build(RectTransform parent);
    }

    /// <summary>已经有自己面板/窗口的 mod：Hub 只放一颗入口，点了由 mod 自己打开。</summary>
    public interface IModSettingsEntry
    {
        string Id { get; }
        string DisplayName { get; }
        byte[] IconPng { get; }
        void OnClick();
    }

    // ===================== 快捷键 / 内置 UI（2026-10-17 新增） =====================
    /// <summary>组合键的修饰键（可叠加）。</summary>
    [Flags]
    public enum HotkeyMods
    {
        None = 0,
        Ctrl = 1,
        Shift = 2,
        Alt = 4,
    }

    /// <summary>一个可改绑的热键。mod 自己给默认值和中文名，Hub 负责显示/改键/持久化。</summary>
    public sealed class HotkeySpec
    {
        public readonly string Id;
        public readonly string Label;
        public readonly KeyCode Key;
        public readonly HotkeyMods Mods;

        public HotkeySpec(string id, string label, KeyCode key, HotkeyMods mods = HotkeyMods.None)
        {
            Id = id ?? "";
            Label = label ?? id ?? "";
            Key = key;
            Mods = mods;
        }

        public string DefaultText { get { return HotkeyStore.Format(this.Key, this.Mods); } }
    }

    /// <summary>mod 注册自己的热键与“是否有内置入口”。</summary>
    public interface IModHotkeys
    {
        /// <summary>= MelonPreferences 分类 id（Hub 用它把设置页和热键对应起来）。</summary>
        string ModId { get; }
        bool HasBuiltinUi { get; }
        List<HotkeySpec> Hotkeys { get; }
    }

    /// <summary>给大多数 mod 用的现成实现：注册一个 mod + 若干热键即可。</summary>
    public sealed class SimpleHotkeys : IModHotkeys
    {
        readonly string _modId;
        readonly bool _ui;
        readonly List<HotkeySpec> _keys;

        public SimpleHotkeys(string modId, bool hasBuiltinUi, params HotkeySpec[] keys)
        {
            _modId = modId ?? "";
            _ui = hasBuiltinUi;
            _keys = new List<HotkeySpec>();
            if (keys != null)
                for (int i = 0; i < keys.Length; i++) if (keys[i] != null) _keys.Add(keys[i]);
        }

        public string ModId { get { return _modId; } }
        public bool HasBuiltinUi { get { return _ui; } }
        public List<HotkeySpec> Hotkeys { get { return _keys; } }
    }

    /// <summary>
    /// 键位/内置 UI 的读写。Hub 写、mod 读；配置存在 MelonPreferences 分类 `ModSettingsHub`。
    /// mod 侧典型用法：
    ///   HotkeyStore.Register(new MyHotkeys());
    ///   if (HotkeyStore.Down("MultiHair", HkPanel)) TogglePanel();
    ///   if (HotkeyStore.UiOn("QualityPlus")) BuildEntry();
    /// </summary>
    public static class HotkeyStore
    {
        const string CatName = "ModSettingsHub";
        static MelonPreferences_Category _cat;
        static readonly Dictionary<string, IModHotkeys> _mods = new Dictionary<string, IModHotkeys>();

        public static event Action Changed;

        /// <summary>Hub 打开期间挂起所有 mod 热键（避免在设置里改键时触发别的面板）。</summary>
        public static bool Suspended;

        public static void Register(IModHotkeys mod)
        {
            try
            {
                if (mod == null || string.IsNullOrEmpty(mod.ModId)) return;
                _mods[mod.ModId] = mod;
            }
            catch { }
        }

        public static IModHotkeys Find(string modId)
        {
            try
            {
                if (string.IsNullOrEmpty(modId)) return null;
                IModHotkeys m;
                return _mods.TryGetValue(modId, out m) ? m : null;
            }
            catch { return null; }
        }

        public static List<IModHotkeys> Snapshot()
        {
            try { return new List<IModHotkeys>(_mods.Values); } catch { return new List<IModHotkeys>(); }
        }

        static MelonPreferences_Category Cat()
        {
            if (_cat == null) _cat = MelonPreferences.CreateCategory(CatName, "ModSettingsHub");
            return _cat;
        }

        static string EntryId(string modId, string id) { return "hk_" + modId + "_" + id; }
        static string UiId(string modId) { return "ui_" + modId; }

        // ★MelonLoader 的 CreateEntry 在 entry 已存在时会抛异常（不是返回已有）⇒ 先查再建
        static MelonPreferences_Entry<string> StrEntry(string id, string def)
        {
            try
            {
                var list = Cat().Entries;
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        var e = list[i] as MelonPreferences_Entry<string>;
                        if (e != null && e.Identifier == id) return e;
                    }
                }
            }
            catch { }
            return Cat().CreateEntry<string>(id, def);
        }

        static MelonPreferences_Entry<bool> BoolEntry(string id, bool def)
        {
            try
            {
                var list = Cat().Entries;
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        var e = list[i] as MelonPreferences_Entry<bool>;
                        if (e != null && e.Identifier == id) return e;
                    }
                }
            }
            catch { }
            return Cat().CreateEntry<bool>(id, def);
        }

        // ---- 文本 <-> 键位 ----
        public static string Format(KeyCode key, HotkeyMods mods)
        {
            try
            {
                string s = "";
                if ((mods & HotkeyMods.Ctrl) != 0) s += "Ctrl+";
                if ((mods & HotkeyMods.Shift) != 0) s += "Shift+";
                if ((mods & HotkeyMods.Alt) != 0) s += "Alt+";
                return s + key.ToString();
            }
            catch { return key.ToString(); }
        }

        public static bool TryParse(string text, out KeyCode key, out HotkeyMods mods)
        {
            key = KeyCode.None;
            mods = HotkeyMods.None;
            try
            {
                if (string.IsNullOrEmpty(text)) return false;
                string[] parts = text.Split('+');
                for (int i = 0; i < parts.Length; i++)
                {
                    string p = parts[i].Trim();
                    if (p.Length == 0) continue;
                    if (string.Equals(p, "Ctrl", StringComparison.OrdinalIgnoreCase)) { mods |= HotkeyMods.Ctrl; continue; }
                    if (string.Equals(p, "Shift", StringComparison.OrdinalIgnoreCase)) { mods |= HotkeyMods.Shift; continue; }
                    if (string.Equals(p, "Alt", StringComparison.OrdinalIgnoreCase)) { mods |= HotkeyMods.Alt; continue; }
                    KeyCode k;
                    if (Enum.TryParse<KeyCode>(p, true, out k)) key = k;
                }
                return key != KeyCode.None;
            }
            catch { return false; }
        }

        static readonly Dictionary<string, string> _bindCache = new Dictionary<string, string>();

        static string BindingCached(string modId, HotkeySpec def)
        {
            try
            {
                string k = modId + "|" + def.Id;
                string v;
                if (_bindCache.TryGetValue(k, out v)) return v;
                v = GetBinding(modId, def);
                _bindCache[k] = v;
                return v;
            }
            catch { return GetBinding(modId, def); }
        }

        public static string GetBinding(string modId, HotkeySpec def)
        {
            try
            {
                if (def == null) return "";
                var e = StrEntry(EntryId(modId, def.Id), def.DefaultText);
                string s = e != null ? e.Value : null;
                if (string.IsNullOrEmpty(s)) s = def.DefaultText;
                return s;
            }
            catch (Exception ex) { MelonLogger.Error("hk get: " + ex.Message); return def != null ? def.DefaultText : ""; }
        }

        public static void SetBinding(string modId, HotkeySpec def, string text)
        {
            try
            {
                if (def == null) return;
                var e = StrEntry(EntryId(modId, def.Id), def.DefaultText);
                if (e != null) e.Value = text ?? def.DefaultText;
                _bindCache.Clear();
                MelonLogger.Msg("hk set " + EntryId(modId, def.Id) + " = " + (e != null ? e.Value : "?")); 
                MelonPreferences.Save();
                var h = Changed;
                if (h != null) h();
            }
            catch (Exception ex) { MelonLogger.Error("hk set: " + ex); }
        }

        /// <summary>当前绑定的主键（未绑定 = KeyCode.None）。</summary>
        public static KeyCode KeyOf(string modId, HotkeySpec def)
        {
            KeyCode k; HotkeyMods m;
            if (TryParse(BindingCached(modId, def), out k, out m)) return k;
            return def != null ? def.Key : KeyCode.None;
        }

        /// <summary>当前绑定的修饰键。</summary>
        public static HotkeyMods ModsOf(string modId, HotkeySpec def)
        {
            KeyCode k; HotkeyMods m;
            if (TryParse(BindingCached(modId, def), out k, out m)) return m;
            return def != null ? def.Mods : HotkeyMods.None;
        }

        public static bool Down(string modId, HotkeySpec def)
        {
            try
            {
                if (Suspended) return false;
                KeyCode k = KeyOf(modId, def);
                if (k == KeyCode.None) return false;
                if (!ModsHeld(ModsOf(modId, def))) return false;
                return Input.GetKeyDown(k);
            }
            catch { return false; }
        }

        public static bool Held(string modId, HotkeySpec def)
        {
            try
            {
                if (Suspended) return false;
                KeyCode k = KeyOf(modId, def);
                if (k == KeyCode.None) return false;
                if (!ModsHeld(ModsOf(modId, def))) return false;
                return Input.GetKey(k);
            }
            catch { return false; }
        }

        static bool ModsHeld(HotkeyMods mods)
        {
            try
            {
                bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
                if ((mods & HotkeyMods.Ctrl) != 0 && !ctrl) return false;
                if ((mods & HotkeyMods.Shift) != 0 && !shift) return false;
                if ((mods & HotkeyMods.Alt) != 0 && !alt) return false;
                // 没要求的修饰键也不许按着（避免 Ctrl+F6 误触发 F6）
                if ((mods & HotkeyMods.Ctrl) == 0 && ctrl) return false;
                if ((mods & HotkeyMods.Shift) == 0 && shift) return false;
                if ((mods & HotkeyMods.Alt) == 0 && alt) return false;
                return true;
            }
            catch { return false; }
        }

        // ---- 内置 UI 开关 ----
        static readonly Dictionary<string, bool> _uiCache = new Dictionary<string, bool>();

        public static bool UiOn(string modId, bool def = true)
        {
            try
            {
                bool v;
                if (_uiCache.TryGetValue(modId, out v)) return v;
                var e = BoolEntry(UiId(modId), def);
                v = e != null ? e.Value : def;
                _uiCache[modId] = v;
                return v;
            }
            catch { return def; }
        }

        public static void SetUiOn(string modId, bool on)
        {
            try
            {
                var e = BoolEntry(UiId(modId), true);
                if (e != null) e.Value = on;
                _uiCache[modId] = on;
                MelonLogger.Msg("hotkey ui: " + modId + " = " + on);
                MelonPreferences.Save();
                var h = Changed;
                if (h != null) h();
            }
            catch { }
        }
    }

    /// <summary>
    /// 静态注册表。注册顺序无关（Hub 每次打开设置窗口时重新读取）。
    /// 线程安全：这些调用都发生在主线程（MelonLoader 初始化 / 用户点击）。
    /// </summary>
    public static class ModSettingsRegistry
    {
        static readonly Dictionary<string, IModSettingsPage> _pages = new Dictionary<string, IModSettingsPage>();
        static readonly Dictionary<string, IModSettingsEntry> _entries = new Dictionary<string, IModSettingsEntry>();

        public static void RegisterPage(IModSettingsPage page)
        {
            if (page == null) return;
            string id = page.Id;
            if (string.IsNullOrEmpty(id)) return;
            _pages[id] = page;
        }

        public static void RegisterEntry(IModSettingsEntry entry)
        {
            if (entry == null) return;
            string id = entry.Id;
            if (string.IsNullOrEmpty(id)) return;
            _entries[id] = entry;
        }

        public static void Unregister(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            _pages.Remove(id);
            _entries.Remove(id);
        }

        /// <summary>快照（避免调用方在遍历时被注册/注销影响）。</summary>
        public static List<IModSettingsPage> SnapshotPages()
        {
            return new List<IModSettingsPage>(_pages.Values);
        }

        public static List<IModSettingsEntry> SnapshotEntries()
        {
            return new List<IModSettingsEntry>(_entries.Values);
        }

        public static int PageCount { get { return _pages.Count; } }
        public static int EntryCount { get { return _entries.Count; } }
    }
}
