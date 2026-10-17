// ============================================================================
// ModSettingsHub — 窗口打开时锁住房间相机输入
//
// 取证（dump_20261015，`Project.RoomCameraInputManager`）：
//   GetCameraMoveInputByKey     Vector3   RVA 0x117DE60
//   GetCameraRotationInputByKey Vector2   RVA 0x117E270
//   GetZoomInputByKey           float     RVA 0x117E430
//   GetCameraMoveInputByDrag    Vector3   RVA 0x117DC70
//   GetCameraRotationInputByDrag Vector2  RVA 0x117E090
//   GetCameraMoveInputByScroll  Vector3   RVA 0x117DFF0
//   GetZoomInputByScroll        float
// 窗口打开时这些返回零 ⇒ 鼠标拖动/滚轮/键盘都不会再动视角。
// ============================================================================
using System;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace ModSettingsHub
{
    public partial class ModSettingsHubMod
    {
        static bool InputLocked;
        static ModSettingsHubMod _self;
        HarmonyLib.Harmony _harmony;

        void InitInputLock()
        {
            try
            {
                _self = this;
                if (_harmony == null) _harmony = new HarmonyLib.Harmony("com.xingmang.modsettingshub");
                var t = FindTypeSimple("RoomCameraInputManager");
                if (t == null) { MelonLogger.Msg("input lock: RoomCameraInputManager not found (will retry)"); return; }

                int n = 0;
                n += PatchZero(t, "GetCameraMoveInputByKey", "PrefixVec3");
                n += PatchZero(t, "GetCameraRotationInputByKey", "PrefixVec2");
                n += PatchZero(t, "GetZoomInputByKey", "PrefixFloat");
                n += PatchZero(t, "GetCameraMoveInputByDrag", "PrefixVec3");
                n += PatchZero(t, "GetCameraRotationInputByDrag", "PrefixVec2");
                n += PatchZero(t, "GetCameraMoveInputByScroll", "PrefixVec3");
                n += PatchZero(t, "GetZoomInputByScroll", "PrefixFloat");
                if (n > 0) { MelonLogger.Msg("input lock: patched " + n + " camera input method(s)"); }
                else MelonLogger.Msg("input lock: 0 methods patched (names changed?)");
            }
            catch (Exception e) { MelonLogger.Error("input lock init: " + e); }
        }

        int PatchZero(Type t, string method, string prefixName)
        {
            try
            {
                var m = t.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (m == null) return 0;
                var pm = typeof(ModSettingsHubMod).GetMethod(prefixName, BindingFlags.Static | BindingFlags.NonPublic);
                if (pm == null) return 0;
                _harmony.Patch(m, prefix: new HarmonyMethod(pm));
                return 1;
            }
            catch (Exception e) { MelonLogger.Error("input lock patch " + method + ": " + e.Message); return 0; }
        }

        static bool PrefixVec3(ref Vector3 __result)
        {
            if (!InputLocked) return true;
            __result = Vector3.zero;
            return false;
        }

        static bool PrefixVec2(ref Vector2 __result)
        {
            if (!InputLocked) return true;
            __result = Vector2.zero;
            return false;
        }

        static bool PrefixFloat(ref float __result)
        {
            if (!InputLocked) return true;
            __result = 0f;
            return false;
        }

        static Type FindTypeSimple(string name)
        {
            try
            {
                var asms = AppDomain.CurrentDomain.GetAssemblies();
                for (int a = 0; a < asms.Length; a++)
                {
                    Type[] ts;
                    try { ts = asms[a].GetTypes(); }
                    catch (ReflectionTypeLoadException e) { ts = e.Types; }
                    catch { continue; }
                    if (ts == null) continue;
                    for (int i = 0; i < ts.Length; i++)
                    {
                        var t = ts[i];
                        if (t != null && t.Name == name) return t;
                    }
                }
            }
            catch { }
            return null;
        }
    }
}
