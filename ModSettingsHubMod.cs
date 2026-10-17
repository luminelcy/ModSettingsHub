// ============================================================================
// ModSettingsHub — 把 mod 设置**直接做进游戏原生设置窗口**（2026-10-17）
//
// 朋友原话：
//   "最简单的就是根据加载的 mod 来放设置页面"
//   "一些有独立菜单快捷键的 也可以直接绑 入口 就放在设置里"
//   "直接做进设置页面里 就不用外面一排按钮了"
//
// 做法（★不卸载场景、不克隆整窗 ⇒ 没有模态/模糊/退出键三笔账）：
//   ① 设置场景加载后，往左侧图标列 `SideButtons/ButtonLayout`（VLG）里克隆原生页签，
//      每个 mod 一颗（图案由 mod 提供；自动配置页用默认网格图）；
//   ② 点我们的图标 ⇒ 在 `P_SwitchSettingsWindowObject/WindowParent` 上盖一层自己的整页
//      （不透明、最上层、自带 raycast）⇒ 不碰原生 _windowDictionary / UpdateWindow；
//   ③ 点任何原生页签 ⇒ 我们自己加的 onClick 监听先把覆盖层收起来（原生行为不受影响）；
//   ④ 条目多了：左侧列高度自动增长/压缩间距（自动扩充）。
//
// 三个设置来源（优先级从高到低）：
//   A. IModSettingsPage（mod 自己 Build，可实时）  → 注册进 ModSettingsHub.Api
//   B. IModSettingsEntry（mod 已有自己的面板，只给一颗入口）→ 点了交给 mod
//   C. MelonPreferences 自动生成（零改动，改完需重启）
// ============================================================================
using System;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;
using MelonLoader.Utils;
using ModSettingsHub.Api;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[assembly: MelonInfo(typeof(ModSettingsHub.ModSettingsHubMod), "ModSettingsHub", "0.2.0", "XingMang")]

namespace ModSettingsHub
{
    public partial class ModSettingsHubMod : MelonMod
    {
        internal const string SettingsSceneName = "S_SettingsScene";
        internal const string HubName = "ModSettingsHub";

        // ---- 一条设置页（注册页 / 注册入口 / 自动配置分类） ----
        internal sealed class HubEntry
        {
            public string Id = "";
            public string ModId = "";      // = MelonPreferences 分类 id / 注册 id（快捷键与内置 UI 用）
            public string Name = "";
            public byte[] IconPng;
            public IModSettingsPage Page;
            public IModSettingsEntry Entry;
            public MelonPreferences_Category Cfg;
        }

        internal readonly List<HubEntry> Entries = new List<HubEntry>();
        string _entrySignature = "";

        // ---- 设置场景里的节点 ----
        internal Transform ContentRoot;
        internal Transform MainRoot;
        internal Transform SideButtons;
        internal Transform ButtonLayout;
        internal Transform WindowParent;

        float _scanAt = -1f;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("v0.2.0 ready. writes ModSettingsHub entries into the game's own settings window.");
            InitInputLock();
            try
            {
                string api = System.IO.Path.Combine(MelonEnvironment.GameRootDirectory, "UserLibs", "ModSettingsHub.Api.dll");
                MelonLogger.Msg("api dll: " + (System.IO.File.Exists(api) ? "ok" : "MISSING") + " (" + api + ")");
            }
            catch { }
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            try
            {
                ResetSpriteCache();   // 每个场景都重扫内存里的原生 Sprite（它们带九宫 border）
                if (!string.IsNullOrEmpty(sceneName)
                    && sceneName.IndexOf(SettingsSceneName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // 新设置场景：旧引用全失效
                    ContentRoot = null; MainRoot = null; SideButtons = null; ButtonLayout = null; WindowParent = null;
                    _entrySignature = "";          // ★必须清：否则新场景里 RefreshEntries 认为"没变化"⇒ 不再注入
                    Entries.Clear();
                    ResetOverlay();
                    ResetButtonTracking();
                    _scanAt = Time.unscaledTime + 0.35f;
                    LogOnce("scene", "settings scene loaded -> will inject after 0.35s");
                }
                else if (!string.IsNullOrEmpty(sceneName) && sceneName.StartsWith("S_Room", StringComparison.Ordinal))
                {
                    _roomMode = false;
                    _uiHost = null;
                    _uiCanvas = null;
                    _roomCanvas = null;
                    _roomMenu = null;
                    _roomEntry = null;
                    _roomSceneName = null;
                    ResetOverlay();
                    ResetButtonTracking();
                }
            }
            catch (Exception e) { MelonLogger.Error("scene hook: " + e.Message); }
        }

        public override void OnUpdate()
        {
            try
            {
                TickHotkeyCapture();     // 改键捕获必须每帧（不受 1s tick 限流）

                float now = Time.unscaledTime;
                if (now < _scanAt) return;
                _scanAt = now + 1f;

                TickRoom();          // 房间入口 + 独立设置窗口（全屏遮罩，不再进游戏设置场景）
                TickSettingsScene(); // 游戏原生设置场景：左列红点入口 + 下拉选 mod

                // ★保险：设置场景不在了（点空白/关闭设置）⇒ 立刻解除相机输入锁 + 热键挂起
                if (InputLocked || HotkeyStore.Suspended)
                {
                    var sc = SceneManager.GetSceneByName(SettingsSceneName);
                    bool gone = !sc.IsValid() || !sc.isLoaded || _overlay == null || !_overlay.activeSelf;
                    if (gone) { InputLocked = false; HotkeyStore.Suspended = false; }
                }
            }
            catch (Exception e) { LogOnce("tickerr", "tick err " + e); }
        }

        internal void LogOnce(string key, string msg)
        {
            // 只打一次（不跟 verbose 走）
            try
            {
                if (_loggedKeys.Add(key)) MelonLogger.Msg(msg);
            }
            catch { }
        }

        readonly HashSet<string> _loggedKeys = new HashSet<string>();

        // ===================== 找设置场景里的节点 =====================
        bool FindSettingsUi()
        {
            try
            {
                var sc = SceneManager.GetSceneByName(SettingsSceneName);
                if (!sc.IsValid() || !sc.isLoaded) return false;

                var roots = sc.GetRootGameObjects();
                if (roots == null || roots.Length == 0) return false;

                Transform canvas = null;
                for (int i = 0; i < roots.Length; i++)
                {
                    try
                    {
                        if (roots[i] == null) continue;
                        var t = roots[i].transform.Find("P_CommonCanvas_Landscape");
                        if (t != null) { canvas = t; break; }
                    }
                    catch { }
                }
                if (canvas == null) canvas = DeepFind(roots, "P_CommonCanvas_Landscape");
                if (canvas == null) return false;

                var content = canvas.Find("ContentRoot");
                if (content == null) return false;
                var main = content.Find("Main");
                if (main == null) return false;
                var side = main.Find("SideButtons");
                if (side == null) return false;
                var layout = side.Find("ButtonLayout");
                if (layout == null) return false;
                var wp = main.Find("P_SwitchSettingsWindowObject/WindowParent");
                if (wp == null) wp = DeepFind(main, "WindowParent");

                ContentRoot = content;
                MainRoot = main;
                SideButtons = side;
                ButtonLayout = layout;
                WindowParent = wp;

                if (WindowParent == null) return false;
                LogOnce("ui", "settings ui found: side='" + side.name + "' buttons=" + layout.childCount
                    + " windowParent='" + WindowParent.name + "' children=" + WindowParent.childCount);
                return true;
            }
            catch (Exception e)
            {
                LogOnce("finderr", "find ui err " + e.Message);
                return false;
            }
        }

        static Transform DeepFind(IEnumerable<GameObject> roots, string name)
        {
            try
            {
                foreach (var go in roots)
                {
                    var t = DeepFind(go != null ? go.transform : null, name);
                    if (t != null) return t;
                }
            }
            catch { }
            return null;
        }

        static Transform DeepFind(Transform t, string name)
        {
            try
            {
                if (t == null) return null;
                if (t.name == name) return t;
                int n = t.childCount;
                for (int i = 0; i < n; i++)
                {
                    var r = DeepFind(t.GetChild(i), name);
                    if (r != null) return r;
                }
            }
            catch { }
            return null;
        }

        // ===================== 收集条目 =====================
        bool RefreshEntries()
        {
            try
            {
                var list = new List<HubEntry>();
                var seen = new HashSet<string>();

                // A. 注册的自建页
                var pages = ModSettingsRegistry.SnapshotPages();
                for (int i = 0; i < pages.Count; i++)
                {
                    var p = pages[i];
                    if (p == null || string.IsNullOrEmpty(p.Id)) continue;
                    string id = "page:" + p.Id;
                    if (!seen.Add(id)) continue;
                    list.Add(new HubEntry { Id = id, ModId = p.Id, Name = SafeName(p.DisplayName, p.Id), IconPng = p.IconPng, Page = p });
                }

                // B. 注册的"打开我自己面板"入口
                var entries = ModSettingsRegistry.SnapshotEntries();
                for (int i = 0; i < entries.Count; i++)
                {
                    var e = entries[i];
                    if (e == null || string.IsNullOrEmpty(e.Id)) continue;
                    string id = "entry:" + e.Id;
                    if (!seen.Add(id)) continue;
                    list.Add(new HubEntry { Id = id, ModId = e.Id, Name = SafeName(e.DisplayName, e.Id), IconPng = e.IconPng, Entry = e });
                }

                // C. MelonPreferences 自动页（排除 Hub 自己、排除已经用 A/B 覆盖的 mod）
                try
                {
                    var cats = MelonPreferences.Categories;
                    if (cats != null)
                    {
                        for (int i = 0; i < cats.Count; i++)
                        {
                            var c = cats[i];
                            if (c == null) continue;
                            string cid = c.Identifier;
                            if (string.IsNullOrEmpty(cid)) continue;
                            if (cid == HubName) continue;
                            if (HideCategory(cid)) continue;
                            try { if (c.Entries == null || c.Entries.Count == 0) continue; } catch { continue; }
                            // 已被 A/B 覆盖（按 id 去掉 "page:"/"entry:" 前缀比较）
                            bool covered = false;
                            foreach (var s in seen)
                            {
                                int k = s.IndexOf(':');
                                if (k >= 0 && string.Equals(s.Substring(k + 1), cid, StringComparison.OrdinalIgnoreCase)) { covered = true; break; }
                            }
                            if (covered) continue;
                            string id = "cfg:" + cid;
                            if (!seen.Add(id)) continue;
                            list.Add(new HubEntry { Id = id, ModId = cid, Name = ChineseName(cid), Cfg = c });
                        }
                    }
                }
                catch (Exception e) { LogOnce("cfgerr", "prefs scan err " + e.Message); }

                // 签名变化才替换（避免每次 tick 重建图标）
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < list.Count; i++) sb.Append(list[i].Id).Append('|');
                string sig = sb.ToString();
                if (sig == _entrySignature) return false;

                _entrySignature = sig;
                Entries.Clear();
                Entries.AddRange(list);
                LogOnce("entries:" + list.Count, "entries changed -> " + list.Count + " (" + sig + ")");
                return true;
            }
            catch (Exception e)
            {
                LogOnce("refresherr", "refresh entries err " + e);
                return false;
            }
        }

        static string SafeName(string a, string b)
        {
            if (!string.IsNullOrEmpty(a)) return a;
            return b ?? "";
        }

        // 分类里只有日志/调试开关 ⇒ 不值得占一个 mod 页（用户："为什么有个表情导演？"）
        static bool OnlyLogCategory(MelonPreferences_Category c)
        {
            try
            {
                if (c == null || c.Entries == null) return false;
                int useful = 0;
                for (int i = 0; i < c.Entries.Count; i++)
                {
                    var e = c.Entries[i];
                    if (e == null) continue;
                    string id = EntryIdentifier(e);
                    if (string.IsNullOrEmpty(id)) continue;
                    if (id.IndexOf("Verbose", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                    if (id.IndexOf("Debug", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                    if (id.IndexOf("Log", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                    useful++;
                }
                return useful == 0;
            }
            catch { return false; }
        }
    }
}
