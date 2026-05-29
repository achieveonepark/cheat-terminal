using System.Text;
using Achieve.CheatTerminal.Core;
using UnityEngine;

namespace Achieve.CheatTerminal.Modules
{
    /// <summary>
    /// Provides terminal commands for the most commonly used Unity built-in components:
    /// Transform, Rigidbody, Camera, Light, AudioListener, Time, and GameObject utilities.
    /// </summary>
    public sealed class UnityComponentsModule : ITerminalModule
    {
        public string Name => "UnityComponents";

        public void Install(Terminal terminal)
        {
            terminal.RegisterCommand(new DelegateCommand(
                "transform", RunTransform,
                "Transform tools: get | pos | rot | scale | reset",
                "Components", "transform <get|pos|rot|scale|reset> <name> [x y z]"));

            terminal.RegisterCommand(new DelegateCommand(
                "rb", RunRigidbody,
                "Rigidbody tools: get | velocity | gravity | kinematic | mass | drag",
                "Components", "rb <get|velocity|gravity|kinematic|mass|drag> <name> [args]"));

            terminal.RegisterCommand(new DelegateCommand(
                "cam", RunCamera,
                "Camera tools: list | fov | bg | ortho | size | clip",
                "Components", "cam <list|fov|bg|ortho|size|clip> [args]"));

            terminal.RegisterCommand(new DelegateCommand(
                "light", RunLight,
                "Light tools: list | intensity | color | range | shadow",
                "Components", "light <list|intensity|color|range|shadow> <name> [args]"));

            terminal.RegisterCommand(new DelegateCommand(
                "audio", RunAudio,
                "Audio tools: volume | mute | pause | resume",
                "Components", "audio <volume|mute|pause|resume> [args]"));

            terminal.RegisterCommand(new DelegateCommand(
                "time", RunTime,
                "Time tools: get | scale | fixed",
                "Components", "time <get|scale|fixed> [value]"));

            terminal.RegisterCommand(new DelegateCommand(
                "go", RunGameObject,
                "GameObject tools: list | active | tag",
                "Components", "go <list|active|tag> [name] [args]"));
        }

        // ── Transform ─────────────────────────────────────────────────────────

        private static void RunTransform(CommandContext ctx)
        {
            string sub = ctx.GetString(0, "").ToLowerInvariant();
            switch (sub)
            {
                case "get":   TransformGet(ctx);   break;
                case "pos":   TransformPos(ctx);   break;
                case "rot":   TransformRot(ctx);   break;
                case "scale": TransformScale(ctx); break;
                case "reset": TransformReset(ctx); break;
                default:      ctx.Output.WriteLine($"Unknown sub-command '{sub}'. Use: get | pos | rot | scale | reset", LogLevel.Error); break;
            }
        }

        private static void TransformGet(CommandContext ctx)
        {
            var t = FindTransform(ctx, 1);
            if (t == null) return;
            var sb = new StringBuilder();
            sb.AppendLine($"[{t.name}]");
            sb.AppendLine($"  position : {FormatV3(t.position)}");
            sb.AppendLine($"  rotation : {FormatV3(t.eulerAngles)}");
            sb.AppendLine($"  scale    : {FormatV3(t.localScale)}");
            ctx.Output.WriteLine(sb.ToString().TrimEnd(), LogLevel.System);
        }

        private static void TransformPos(CommandContext ctx)
        {
            var t = FindTransform(ctx, 1);
            if (t == null) return;
            if (!ctx.Has(4))
            {
                ctx.Output.WriteLine($"{t.name}.position = {FormatV3(t.position)}", LogLevel.Info);
                return;
            }
            if (!ParseXYZ(ctx, 2, out var v)) return;
            t.position = v;
            ctx.Output.WriteLine($"{t.name}.position = {FormatV3(t.position)}", LogLevel.Success);
        }

        private static void TransformRot(CommandContext ctx)
        {
            var t = FindTransform(ctx, 1);
            if (t == null) return;
            if (!ctx.Has(4))
            {
                ctx.Output.WriteLine($"{t.name}.eulerAngles = {FormatV3(t.eulerAngles)}", LogLevel.Info);
                return;
            }
            if (!ParseXYZ(ctx, 2, out var v)) return;
            t.eulerAngles = v;
            ctx.Output.WriteLine($"{t.name}.eulerAngles = {FormatV3(t.eulerAngles)}", LogLevel.Success);
        }

        private static void TransformScale(CommandContext ctx)
        {
            var t = FindTransform(ctx, 1);
            if (t == null) return;
            if (!ctx.Has(4))
            {
                ctx.Output.WriteLine($"{t.name}.localScale = {FormatV3(t.localScale)}", LogLevel.Info);
                return;
            }
            if (!ParseXYZ(ctx, 2, out var v)) return;
            t.localScale = v;
            ctx.Output.WriteLine($"{t.name}.localScale = {FormatV3(t.localScale)}", LogLevel.Success);
        }

        private static void TransformReset(CommandContext ctx)
        {
            var t = FindTransform(ctx, 1);
            if (t == null) return;
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale    = Vector3.one;
            ctx.Output.WriteLine($"{t.name} transform reset.", LogLevel.Success);
        }

        // ── Rigidbody ─────────────────────────────────────────────────────────

        private static void RunRigidbody(CommandContext ctx)
        {
            string sub = ctx.GetString(0, "").ToLowerInvariant();
            switch (sub)
            {
                case "get":       RbGet(ctx);       break;
                case "velocity":  RbVelocity(ctx);  break;
                case "gravity":   RbGravity(ctx);   break;
                case "kinematic": RbKinematic(ctx); break;
                case "mass":      RbMass(ctx);      break;
                case "drag":      RbDrag(ctx);      break;
                default:          ctx.Output.WriteLine($"Unknown sub-command '{sub}'. Use: get | velocity | gravity | kinematic | mass | drag", LogLevel.Error); break;
            }
        }

        private static void RbGet(CommandContext ctx)
        {
            var rb = FindComponent<Rigidbody>(ctx, 1);
            if (rb == null) return;
            var sb = new StringBuilder();
            sb.AppendLine($"[{rb.name} / Rigidbody]");
            sb.AppendLine($"  mass        : {rb.mass}");
            sb.AppendLine($"  drag        : {rb.linearDamping}");
            sb.AppendLine($"  angularDrag : {rb.angularDamping}");
            sb.AppendLine($"  velocity    : {FormatV3(rb.linearVelocity)}");
            sb.AppendLine($"  useGravity  : {rb.useGravity}");
            sb.AppendLine($"  isKinematic : {rb.isKinematic}");
            ctx.Output.WriteLine(sb.ToString().TrimEnd(), LogLevel.System);
        }

        private static void RbVelocity(CommandContext ctx)
        {
            var rb = FindComponent<Rigidbody>(ctx, 1);
            if (rb == null) return;
            if (!ctx.Has(4))
            {
                ctx.Output.WriteLine($"{rb.name}.velocity = {FormatV3(rb.linearVelocity)}", LogLevel.Info);
                return;
            }
            if (!ParseXYZ(ctx, 2, out var v)) return;
            rb.linearVelocity = v;
            ctx.Output.WriteLine($"{rb.name}.velocity = {FormatV3(rb.linearVelocity)}", LogLevel.Success);
        }

        private static void RbGravity(CommandContext ctx)
        {
            var rb = FindComponent<Rigidbody>(ctx, 1);
            if (rb == null) return;
            if (!ctx.Has(2)) { ctx.Output.WriteLine($"{rb.name}.useGravity = {rb.useGravity}", LogLevel.Info); return; }
            rb.useGravity = ParseBool(ctx.Args[2]);
            ctx.Output.WriteLine($"{rb.name}.useGravity = {rb.useGravity}", LogLevel.Success);
        }

        private static void RbKinematic(CommandContext ctx)
        {
            var rb = FindComponent<Rigidbody>(ctx, 1);
            if (rb == null) return;
            if (!ctx.Has(2)) { ctx.Output.WriteLine($"{rb.name}.isKinematic = {rb.isKinematic}", LogLevel.Info); return; }
            rb.isKinematic = ParseBool(ctx.Args[2]);
            ctx.Output.WriteLine($"{rb.name}.isKinematic = {rb.isKinematic}", LogLevel.Success);
        }

        private static void RbMass(CommandContext ctx)
        {
            var rb = FindComponent<Rigidbody>(ctx, 1);
            if (rb == null) return;
            if (!ctx.Has(2)) { ctx.Output.WriteLine($"{rb.name}.mass = {rb.mass}", LogLevel.Info); return; }
            if (!float.TryParse(ctx.Args[2], out float val)) { ctx.Output.WriteLine("Invalid float value.", LogLevel.Error); return; }
            rb.mass = val;
            ctx.Output.WriteLine($"{rb.name}.mass = {rb.mass}", LogLevel.Success);
        }

        private static void RbDrag(CommandContext ctx)
        {
            var rb = FindComponent<Rigidbody>(ctx, 1);
            if (rb == null) return;
            if (!ctx.Has(2)) { ctx.Output.WriteLine($"{rb.name}.drag = {rb.linearDamping}", LogLevel.Info); return; }
            if (!float.TryParse(ctx.Args[2], out float val)) { ctx.Output.WriteLine("Invalid float value.", LogLevel.Error); return; }
            rb.linearDamping = val;
            ctx.Output.WriteLine($"{rb.name}.drag = {rb.linearDamping}", LogLevel.Success);
        }

        // ── Camera ────────────────────────────────────────────────────────────

        private static void RunCamera(CommandContext ctx)
        {
            string sub = ctx.GetString(0, "list").ToLowerInvariant();
            switch (sub)
            {
                case "list":  CamList(ctx);  break;
                case "fov":   CamFov(ctx);   break;
                case "bg":    CamBg(ctx);    break;
                case "ortho": CamOrtho(ctx); break;
                case "size":  CamSize(ctx);  break;
                case "clip":  CamClip(ctx);  break;
                default:      ctx.Output.WriteLine($"Unknown sub-command '{sub}'. Use: list | fov | bg | ortho | size | clip", LogLevel.Error); break;
            }
        }

        private static void CamList(CommandContext ctx)
        {
            var cams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            if (cams.Length == 0) { ctx.Output.WriteLine("No cameras found.", LogLevel.Warning); return; }
            var sb = new StringBuilder();
            sb.AppendLine($"Cameras ({cams.Length}):");
            foreach (var c in cams)
            {
                bool main = c == Camera.main;
                sb.AppendLine($"  {(main ? "*" : " ")} {c.name}  depth={c.depth}  fov={c.fieldOfView:F1}  {(c.orthographic ? $"ortho size={c.orthographicSize:F2}" : "perspective")}");
            }
            ctx.Output.WriteLine(sb.ToString().TrimEnd(), LogLevel.System);
        }

        private static void CamFov(CommandContext ctx)
        {
            var cam = Camera.main;
            if (cam == null) { ctx.Output.WriteLine("No main camera.", LogLevel.Error); return; }
            if (!ctx.Has(1)) { ctx.Output.WriteLine($"Camera.main.fieldOfView = {cam.fieldOfView:F2}", LogLevel.Info); return; }
            if (!float.TryParse(ctx.Args[1], out float val)) { ctx.Output.WriteLine("Invalid float.", LogLevel.Error); return; }
            cam.fieldOfView = Mathf.Clamp(val, 1f, 179f);
            ctx.Output.WriteLine($"Camera.main.fieldOfView = {cam.fieldOfView:F2}", LogLevel.Success);
        }

        private static void CamBg(CommandContext ctx)
        {
            var cam = Camera.main;
            if (cam == null) { ctx.Output.WriteLine("No main camera.", LogLevel.Error); return; }
            if (!ctx.Has(3))
            {
                var bg = cam.backgroundColor;
                ctx.Output.WriteLine($"Camera.main.backgroundColor = ({bg.r:F2}, {bg.g:F2}, {bg.b:F2}, {bg.a:F2})", LogLevel.Info);
                return;
            }
            if (!float.TryParse(ctx.Args[1], out float r) ||
                !float.TryParse(ctx.Args[2], out float g) ||
                !float.TryParse(ctx.Args[3], out float b))
            {
                ctx.Output.WriteLine("Usage: cam bg <r> <g> <b>  (0–1 range)", LogLevel.Error);
                return;
            }
            float a = ctx.Has(4) && float.TryParse(ctx.Args[4], out float pa) ? pa : 1f;
            cam.backgroundColor = new Color(r, g, b, a);
            ctx.Output.WriteLine($"Camera.main.backgroundColor set.", LogLevel.Success);
        }

        private static void CamOrtho(CommandContext ctx)
        {
            var cam = Camera.main;
            if (cam == null) { ctx.Output.WriteLine("No main camera.", LogLevel.Error); return; }
            if (!ctx.Has(1)) { ctx.Output.WriteLine($"Camera.main.orthographic = {cam.orthographic}", LogLevel.Info); return; }
            cam.orthographic = ParseBool(ctx.Args[1]);
            ctx.Output.WriteLine($"Camera.main.orthographic = {cam.orthographic}", LogLevel.Success);
        }

        private static void CamSize(CommandContext ctx)
        {
            var cam = Camera.main;
            if (cam == null) { ctx.Output.WriteLine("No main camera.", LogLevel.Error); return; }
            if (!ctx.Has(1)) { ctx.Output.WriteLine($"Camera.main.orthographicSize = {cam.orthographicSize:F2}", LogLevel.Info); return; }
            if (!float.TryParse(ctx.Args[1], out float val)) { ctx.Output.WriteLine("Invalid float.", LogLevel.Error); return; }
            cam.orthographicSize = Mathf.Max(0.01f, val);
            ctx.Output.WriteLine($"Camera.main.orthographicSize = {cam.orthographicSize:F2}", LogLevel.Success);
        }

        private static void CamClip(CommandContext ctx)
        {
            var cam = Camera.main;
            if (cam == null) { ctx.Output.WriteLine("No main camera.", LogLevel.Error); return; }
            if (!ctx.Has(2))
            {
                ctx.Output.WriteLine($"Camera.main clip: near={cam.nearClipPlane:F3}  far={cam.farClipPlane:F1}", LogLevel.Info);
                return;
            }
            if (!float.TryParse(ctx.Args[1], out float near) || !float.TryParse(ctx.Args[2], out float far))
            {
                ctx.Output.WriteLine("Usage: cam clip <near> <far>", LogLevel.Error);
                return;
            }
            cam.nearClipPlane = near;
            cam.farClipPlane  = far;
            ctx.Output.WriteLine($"Camera.main clip: near={cam.nearClipPlane:F3}  far={cam.farClipPlane:F1}", LogLevel.Success);
        }

        // ── Light ─────────────────────────────────────────────────────────────

        private static void RunLight(CommandContext ctx)
        {
            string sub = ctx.GetString(0, "list").ToLowerInvariant();
            switch (sub)
            {
                case "list":      LightList(ctx);      break;
                case "intensity": LightIntensity(ctx); break;
                case "color":     LightColor(ctx);     break;
                case "range":     LightRange(ctx);     break;
                case "shadow":    LightShadow(ctx);    break;
                default:          ctx.Output.WriteLine($"Unknown sub-command '{sub}'. Use: list | intensity | color | range | shadow", LogLevel.Error); break;
            }
        }

        private static void LightList(CommandContext ctx)
        {
            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            if (lights.Length == 0) { ctx.Output.WriteLine("No lights found.", LogLevel.Warning); return; }
            var sb = new StringBuilder();
            sb.AppendLine($"Lights ({lights.Length}):");
            foreach (var l in lights)
                sb.AppendLine($"  {l.name}  type={l.type}  intensity={l.intensity:F2}  range={l.range:F1}  shadow={l.shadows}");
            ctx.Output.WriteLine(sb.ToString().TrimEnd(), LogLevel.System);
        }

        private static void LightIntensity(CommandContext ctx)
        {
            var l = FindComponent<Light>(ctx, 1);
            if (l == null) return;
            if (!ctx.Has(2)) { ctx.Output.WriteLine($"{l.name}.intensity = {l.intensity:F2}", LogLevel.Info); return; }
            if (!float.TryParse(ctx.Args[2], out float val)) { ctx.Output.WriteLine("Invalid float.", LogLevel.Error); return; }
            l.intensity = Mathf.Max(0f, val);
            ctx.Output.WriteLine($"{l.name}.intensity = {l.intensity:F2}", LogLevel.Success);
        }

        private static void LightColor(CommandContext ctx)
        {
            var l = FindComponent<Light>(ctx, 1);
            if (l == null) return;
            if (!ctx.Has(4))
            {
                ctx.Output.WriteLine($"{l.name}.color = ({l.color.r:F2}, {l.color.g:F2}, {l.color.b:F2})", LogLevel.Info);
                return;
            }
            if (!float.TryParse(ctx.Args[2], out float r) ||
                !float.TryParse(ctx.Args[3], out float g) ||
                !float.TryParse(ctx.Args[4], out float b))
            {
                ctx.Output.WriteLine("Usage: light color <name> <r> <g> <b>  (0–1 range)", LogLevel.Error);
                return;
            }
            l.color = new Color(r, g, b);
            ctx.Output.WriteLine($"{l.name}.color set.", LogLevel.Success);
        }

        private static void LightRange(CommandContext ctx)
        {
            var l = FindComponent<Light>(ctx, 1);
            if (l == null) return;
            if (!ctx.Has(2)) { ctx.Output.WriteLine($"{l.name}.range = {l.range:F1}", LogLevel.Info); return; }
            if (!float.TryParse(ctx.Args[2], out float val)) { ctx.Output.WriteLine("Invalid float.", LogLevel.Error); return; }
            l.range = Mathf.Max(0f, val);
            ctx.Output.WriteLine($"{l.name}.range = {l.range:F1}", LogLevel.Success);
        }

        private static void LightShadow(CommandContext ctx)
        {
            var l = FindComponent<Light>(ctx, 1);
            if (l == null) return;
            if (!ctx.Has(2)) { ctx.Output.WriteLine($"{l.name}.shadows = {l.shadows}", LogLevel.Info); return; }
            bool on = ParseBool(ctx.Args[2]);
            l.shadows = on ? LightShadows.Soft : LightShadows.None;
            ctx.Output.WriteLine($"{l.name}.shadows = {l.shadows}", LogLevel.Success);
        }

        // ── Audio ─────────────────────────────────────────────────────────────

        private static void RunAudio(CommandContext ctx)
        {
            string sub = ctx.GetString(0, "").ToLowerInvariant();
            switch (sub)
            {
                case "volume": AudioVolume(ctx); break;
                case "mute":   AudioMute(ctx);   break;
                case "pause":  AudioListener.pause = true;  ctx.Output.WriteLine("Audio paused.",  LogLevel.Success); break;
                case "resume": AudioListener.pause = false; ctx.Output.WriteLine("Audio resumed.", LogLevel.Success); break;
                default:       ctx.Output.WriteLine($"Unknown sub-command '{sub}'. Use: volume | mute | pause | resume", LogLevel.Error); break;
            }
        }

        private static void AudioVolume(CommandContext ctx)
        {
            if (!ctx.Has(1)) { ctx.Output.WriteLine($"AudioListener.volume = {AudioListener.volume:F2}", LogLevel.Info); return; }
            if (!float.TryParse(ctx.Args[1], out float val)) { ctx.Output.WriteLine("Invalid float.", LogLevel.Error); return; }
            AudioListener.volume = Mathf.Clamp01(val);
            ctx.Output.WriteLine($"AudioListener.volume = {AudioListener.volume:F2}", LogLevel.Success);
        }

        private static void AudioMute(CommandContext ctx)
        {
            if (!ctx.Has(1)) { ctx.Output.WriteLine($"AudioListener.pause = {AudioListener.pause}", LogLevel.Info); return; }
            AudioListener.pause = ParseBool(ctx.Args[1]);
            ctx.Output.WriteLine($"AudioListener.pause = {AudioListener.pause}", LogLevel.Success);
        }

        // ── Time ──────────────────────────────────────────────────────────────

        private static void RunTime(CommandContext ctx)
        {
            string sub = ctx.GetString(0, "get").ToLowerInvariant();
            switch (sub)
            {
                case "get":   TimeGet(ctx);   break;
                case "scale": TimeScale(ctx); break;
                case "fixed": TimeFixed(ctx); break;
                default:      ctx.Output.WriteLine($"Unknown sub-command '{sub}'. Use: get | scale | fixed", LogLevel.Error); break;
            }
        }

        private static void TimeGet(CommandContext ctx)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Time]");
            sb.AppendLine($"  timeScale      : {Time.timeScale:F2}");
            sb.AppendLine($"  fixedDeltaTime : {Time.fixedDeltaTime:F4}");
            sb.AppendLine($"  time           : {Time.time:F2}");
            sb.AppendLine($"  frameCount     : {Time.frameCount}");
            ctx.Output.WriteLine(sb.ToString().TrimEnd(), LogLevel.System);
        }

        private static void TimeScale(CommandContext ctx)
        {
            if (!ctx.Has(1)) { ctx.Output.WriteLine($"Time.timeScale = {Time.timeScale:F2}", LogLevel.Info); return; }
            if (!float.TryParse(ctx.Args[1], out float val)) { ctx.Output.WriteLine("Invalid float.", LogLevel.Error); return; }
            Time.timeScale = Mathf.Max(0f, val);
            ctx.Output.WriteLine($"Time.timeScale = {Time.timeScale:F2}", LogLevel.Success);
        }

        private static void TimeFixed(CommandContext ctx)
        {
            if (!ctx.Has(1)) { ctx.Output.WriteLine($"Time.fixedDeltaTime = {Time.fixedDeltaTime:F4}", LogLevel.Info); return; }
            if (!float.TryParse(ctx.Args[1], out float val)) { ctx.Output.WriteLine("Invalid float.", LogLevel.Error); return; }
            Time.fixedDeltaTime = Mathf.Max(0.0001f, val);
            ctx.Output.WriteLine($"Time.fixedDeltaTime = {Time.fixedDeltaTime:F4}", LogLevel.Success);
        }

        // ── GameObject ────────────────────────────────────────────────────────

        private static void RunGameObject(CommandContext ctx)
        {
            string sub = ctx.GetString(0, "list").ToLowerInvariant();
            switch (sub)
            {
                case "list":   GoList(ctx);   break;
                case "active": GoActive(ctx); break;
                case "tag":    GoTag(ctx);    break;
                default:       ctx.Output.WriteLine($"Unknown sub-command '{sub}'. Use: list | active | tag", LogLevel.Error); break;
            }
        }

        private static void GoList(CommandContext ctx)
        {
            string filterTag = ctx.GetString(1, "");
            GameObject[] all = string.IsNullOrEmpty(filterTag)
                ? Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                : GameObject.FindGameObjectsWithTag(filterTag);

            const int max = 40;
            var sb = new StringBuilder();
            sb.AppendLine($"GameObjects ({Mathf.Min(all.Length, max)}/{all.Length}){(string.IsNullOrEmpty(filterTag) ? "" : $" tag={filterTag}")}:");
            int shown = 0;
            foreach (var go in all)
            {
                if (shown++ >= max) break;
                sb.AppendLine($"  {(go.activeInHierarchy ? "+" : "-")} {go.name}  tag={go.tag}  layer={LayerMask.LayerToName(go.layer)}");
            }
            ctx.Output.WriteLine(sb.ToString().TrimEnd(), LogLevel.System);
        }

        private static void GoActive(CommandContext ctx)
        {
            if (!ctx.Has(2)) { ctx.Output.WriteLine("Usage: go active <name> <on|off>", LogLevel.Error); return; }
            var go = GameObject.Find(ctx.Args[1]);
            if (go == null) { ctx.Output.WriteLine($"GameObject '{ctx.Args[1]}' not found.", LogLevel.Error); return; }
            bool active = ParseBool(ctx.Args[2]);
            go.SetActive(active);
            ctx.Output.WriteLine($"{go.name}.active = {active}", LogLevel.Success);
        }

        private static void GoTag(CommandContext ctx)
        {
            if (!ctx.Has(1)) { ctx.Output.WriteLine("Usage: go tag <name> [newtag]", LogLevel.Error); return; }
            var go = GameObject.Find(ctx.Args[1]);
            if (go == null) { ctx.Output.WriteLine($"GameObject '{ctx.Args[1]}' not found.", LogLevel.Error); return; }
            if (!ctx.Has(2)) { ctx.Output.WriteLine($"{go.name}.tag = {go.tag}", LogLevel.Info); return; }
            go.tag = ctx.Args[2];
            ctx.Output.WriteLine($"{go.name}.tag = {go.tag}", LogLevel.Success);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static Transform FindTransform(CommandContext ctx, int argIndex)
        {
            if (!ctx.Has(argIndex)) { ctx.Output.WriteLine("GameObject name required.", LogLevel.Error); return null; }
            var go = GameObject.Find(ctx.Args[argIndex]);
            if (go == null) { ctx.Output.WriteLine($"GameObject '{ctx.Args[argIndex]}' not found.", LogLevel.Error); return null; }
            return go.transform;
        }

        private static T FindComponent<T>(CommandContext ctx, int argIndex) where T : Component
        {
            if (!ctx.Has(argIndex)) { ctx.Output.WriteLine("GameObject name required.", LogLevel.Error); return null; }
            var go = GameObject.Find(ctx.Args[argIndex]);
            if (go == null) { ctx.Output.WriteLine($"GameObject '{ctx.Args[argIndex]}' not found.", LogLevel.Error); return null; }
            var comp = go.GetComponent<T>();
            if (comp == null) { ctx.Output.WriteLine($"'{go.name}' has no {typeof(T).Name} component.", LogLevel.Error); return null; }
            return comp;
        }

        private static bool ParseXYZ(CommandContext ctx, int startIndex, out Vector3 result)
        {
            result = Vector3.zero;
            if (!float.TryParse(ctx.Args[startIndex],     out float x) ||
                !float.TryParse(ctx.Args[startIndex + 1], out float y) ||
                !float.TryParse(ctx.Args[startIndex + 2], out float z))
            {
                ctx.Output.WriteLine("Invalid x y z values.", LogLevel.Error);
                return false;
            }
            result = new Vector3(x, y, z);
            return true;
        }

        private static bool ParseBool(string s)
        {
            s = s.ToLowerInvariant();
            return s == "on" || s == "true" || s == "1" || s == "yes";
        }

        private static string FormatV3(Vector3 v) => $"({v.x:F2}, {v.y:F2}, {v.z:F2})";
    }
}
