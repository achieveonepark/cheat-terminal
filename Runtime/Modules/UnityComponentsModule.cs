using System.Collections.Generic;
using System.Globalization;
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
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public string Name => "UnityComponents";

        public void Install(Terminal terminal)
        {
            terminal.RegisterCommand(new DelegateCommand(
                "transform", RunTransform,
                "Transform tools: get | pos | rot | scale | reset",
                "Components", "transform <get|pos|rot|scale|reset> <name> [x y z]",
                new CommandCompletionProvider(CompleteTransform)));

            terminal.RegisterCommand(new DelegateCommand(
                "rb", RunRigidbody,
                "Rigidbody tools: get | velocity | gravity | kinematic | mass | drag",
                "Components", "rb <get|velocity|gravity|kinematic|mass|drag> <name> [args]",
                new CommandCompletionProvider(CompleteRigidbody)));

            terminal.RegisterCommand(new DelegateCommand(
                "cam", RunCamera,
                "Camera tools: list | fov | bg | ortho | size | clip",
                "Components", "cam <list|fov|bg|ortho|size|clip> [args]",
                new CommandCompletionProvider(CompleteCamera)));

            terminal.RegisterCommand(new DelegateCommand(
                "light", RunLight,
                "Light tools: list | intensity | color | range | shadow",
                "Components", "light <list|intensity|color|range|shadow> <name> [args]",
                new CommandCompletionProvider(CompleteLight)));

            terminal.RegisterCommand(new DelegateCommand(
                "audio", RunAudio,
                "Audio tools: volume | mute | pause | resume",
                "Components", "audio <volume|mute|pause|resume> [args]",
                new CommandCompletionProvider(CompleteAudio)));

            terminal.RegisterCommand(new DelegateCommand(
                "time", RunTime,
                "Time tools: get | scale | fixed",
                "Components", "time <get|scale|fixed> [value]"));

            terminal.RegisterCommand(new DelegateCommand(
                "go", RunGameObject,
                "GameObject tools: list | active | tag",
                "Components", "go <list|active|tag> [name] [args]",
                new CommandCompletionProvider(CompleteGameObjectCommand)));
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
            if (!TryParseBool(ctx, 2, out bool useGravity)) return;
            rb.useGravity = useGravity;
            ctx.Output.WriteLine($"{rb.name}.useGravity = {rb.useGravity}", LogLevel.Success);
        }

        private static void RbKinematic(CommandContext ctx)
        {
            var rb = FindComponent<Rigidbody>(ctx, 1);
            if (rb == null) return;
            if (!ctx.Has(2)) { ctx.Output.WriteLine($"{rb.name}.isKinematic = {rb.isKinematic}", LogLevel.Info); return; }
            if (!TryParseBool(ctx, 2, out bool isKinematic)) return;
            rb.isKinematic = isKinematic;
            ctx.Output.WriteLine($"{rb.name}.isKinematic = {rb.isKinematic}", LogLevel.Success);
        }

        private static void RbMass(CommandContext ctx)
        {
            var rb = FindComponent<Rigidbody>(ctx, 1);
            if (rb == null) return;
            if (!ctx.Has(2)) { ctx.Output.WriteLine($"{rb.name}.mass = {rb.mass}", LogLevel.Info); return; }
            if (!TryParseFloat(ctx, 2, out float val)) return;
            rb.mass = val;
            ctx.Output.WriteLine($"{rb.name}.mass = {rb.mass}", LogLevel.Success);
        }

        private static void RbDrag(CommandContext ctx)
        {
            var rb = FindComponent<Rigidbody>(ctx, 1);
            if (rb == null) return;
            if (!ctx.Has(2)) { ctx.Output.WriteLine($"{rb.name}.drag = {rb.linearDamping}", LogLevel.Info); return; }
            if (!TryParseFloat(ctx, 2, out float val)) return;
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
            if (!TryParseFloat(ctx, 1, out float val)) return;
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
            if (!TryParseFloat(ctx.Args[1], out float r) ||
                !TryParseFloat(ctx.Args[2], out float g) ||
                !TryParseFloat(ctx.Args[3], out float b))
            {
                ctx.Output.WriteLine("Usage: cam bg <r> <g> <b>  (0–1 range)", LogLevel.Error);
                return;
            }
            float a = ctx.Has(4) && TryParseFloat(ctx.Args[4], out float pa) ? pa : 1f;
            cam.backgroundColor = new Color(r, g, b, a);
            ctx.Output.WriteLine($"Camera.main.backgroundColor set.", LogLevel.Success);
        }

        private static void CamOrtho(CommandContext ctx)
        {
            var cam = Camera.main;
            if (cam == null) { ctx.Output.WriteLine("No main camera.", LogLevel.Error); return; }
            if (!ctx.Has(1)) { ctx.Output.WriteLine($"Camera.main.orthographic = {cam.orthographic}", LogLevel.Info); return; }
            if (!TryParseBool(ctx, 1, out bool orthographic)) return;
            cam.orthographic = orthographic;
            ctx.Output.WriteLine($"Camera.main.orthographic = {cam.orthographic}", LogLevel.Success);
        }

        private static void CamSize(CommandContext ctx)
        {
            var cam = Camera.main;
            if (cam == null) { ctx.Output.WriteLine("No main camera.", LogLevel.Error); return; }
            if (!ctx.Has(1)) { ctx.Output.WriteLine($"Camera.main.orthographicSize = {cam.orthographicSize:F2}", LogLevel.Info); return; }
            if (!TryParseFloat(ctx, 1, out float val)) return;
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
            if (!TryParseFloat(ctx.Args[1], out float near) || !TryParseFloat(ctx.Args[2], out float far))
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
            if (!TryParseFloat(ctx, 2, out float val)) return;
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
            if (!TryParseFloat(ctx.Args[2], out float r) ||
                !TryParseFloat(ctx.Args[3], out float g) ||
                !TryParseFloat(ctx.Args[4], out float b))
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
            if (!TryParseFloat(ctx, 2, out float val)) return;
            l.range = Mathf.Max(0f, val);
            ctx.Output.WriteLine($"{l.name}.range = {l.range:F1}", LogLevel.Success);
        }

        private static void LightShadow(CommandContext ctx)
        {
            var l = FindComponent<Light>(ctx, 1);
            if (l == null) return;
            if (!ctx.Has(2)) { ctx.Output.WriteLine($"{l.name}.shadows = {l.shadows}", LogLevel.Info); return; }
            if (!TryParseBool(ctx, 2, out bool on)) return;
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
            if (!TryParseFloat(ctx, 1, out float val)) return;
            AudioListener.volume = Mathf.Clamp01(val);
            ctx.Output.WriteLine($"AudioListener.volume = {AudioListener.volume:F2}", LogLevel.Success);
        }

        private static void AudioMute(CommandContext ctx)
        {
            if (!ctx.Has(1)) { ctx.Output.WriteLine($"AudioListener.pause = {AudioListener.pause}", LogLevel.Info); return; }
            if (!TryParseBool(ctx, 1, out bool pause)) return;
            AudioListener.pause = pause;
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
            if (!TryParseFloat(ctx, 1, out float val)) return;
            Time.timeScale = Mathf.Max(0f, val);
            ctx.Output.WriteLine($"Time.timeScale = {Time.timeScale:F2}", LogLevel.Success);
        }

        private static void TimeFixed(CommandContext ctx)
        {
            if (!ctx.Has(1)) { ctx.Output.WriteLine($"Time.fixedDeltaTime = {Time.fixedDeltaTime:F4}", LogLevel.Info); return; }
            if (!TryParseFloat(ctx, 1, out float val)) return;
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
            GameObject[] all = Object.FindObjectsByType<GameObject>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            const int max = 40;
            int total = 0;
            int shown = 0;
            var body = new StringBuilder();
            foreach (var go in all)
            {
                if (!string.IsNullOrEmpty(filterTag) &&
                    !go.tag.Equals(filterTag, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                total++;
                if (shown++ >= max) continue;
                body.AppendLine($"  {(go.activeInHierarchy ? "+" : "-")} {go.name}  tag={go.tag}  layer={LayerMask.LayerToName(go.layer)}");
            }

            var sb = new StringBuilder();
            sb.AppendLine($"GameObjects ({Mathf.Min(total, max)}/{total}){(string.IsNullOrEmpty(filterTag) ? "" : $" tag={filterTag}")}:");
            sb.Append(body);
            ctx.Output.WriteLine(sb.ToString().TrimEnd(), LogLevel.System);
        }

        private static void GoActive(CommandContext ctx)
        {
            if (!ctx.Has(2)) { ctx.Output.WriteLine("Usage: go active <name> <on|off>", LogLevel.Error); return; }
            var go = FindGameObject(ctx.Args[1], includeInactive: true);
            if (go == null) { ctx.Output.WriteLine($"GameObject '{ctx.Args[1]}' not found.", LogLevel.Error); return; }
            if (!TryParseBool(ctx, 2, out bool active)) return;
            go.SetActive(active);
            ctx.Output.WriteLine($"{go.name}.active = {active}", LogLevel.Success);
        }

        private static void GoTag(CommandContext ctx)
        {
            if (!ctx.Has(1)) { ctx.Output.WriteLine("Usage: go tag <name> [newtag]", LogLevel.Error); return; }
            var go = FindGameObject(ctx.Args[1], includeInactive: true);
            if (go == null) { ctx.Output.WriteLine($"GameObject '{ctx.Args[1]}' not found.", LogLevel.Error); return; }
            if (!ctx.Has(2)) { ctx.Output.WriteLine($"{go.name}.tag = {go.tag}", LogLevel.Info); return; }
            go.tag = ctx.Args[2];
            ctx.Output.WriteLine($"{go.name}.tag = {go.tag}", LogLevel.Success);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void CompleteTransform(CommandCompletionContext ctx, List<CompletionItem> results)
        {
            if (ctx.ArgumentIndex == 1)
                CompleteGameObjects(ctx, results, null, "GameObject");
        }

        private static void CompleteRigidbody(CommandCompletionContext ctx, List<CompletionItem> results)
        {
            if (ctx.ArgumentIndex == 1)
                CompleteGameObjects(ctx, results,
                    go => go.GetComponent<Rigidbody>() != null, "Rigidbody");

            string sub = FirstArg(ctx.Input);
            if (ctx.ArgumentIndex == 2 &&
                (sub.Equals("gravity", System.StringComparison.OrdinalIgnoreCase) ||
                 sub.Equals("kinematic", System.StringComparison.OrdinalIgnoreCase)))
                CompleteBooleans(ctx, results);
        }

        private static void CompleteCamera(CommandCompletionContext ctx, List<CompletionItem> results)
        {
            if (ctx.ArgumentIndex == 1 &&
                FirstArg(ctx.Input).Equals("ortho", System.StringComparison.OrdinalIgnoreCase))
                CompleteBooleans(ctx, results);
        }

        private static void CompleteLight(CommandCompletionContext ctx, List<CompletionItem> results)
        {
            if (ctx.ArgumentIndex == 1)
                CompleteGameObjects(ctx, results,
                    go => go.GetComponent<Light>() != null, "Light");

            if (ctx.ArgumentIndex == 2 &&
                FirstArg(ctx.Input).Equals("shadow", System.StringComparison.OrdinalIgnoreCase))
                CompleteBooleans(ctx, results);
        }

        private static void CompleteAudio(CommandCompletionContext ctx, List<CompletionItem> results)
        {
            if (ctx.ArgumentIndex == 1 &&
                FirstArg(ctx.Input).Equals("mute", System.StringComparison.OrdinalIgnoreCase))
                CompleteBooleans(ctx, results);
        }

        private static void CompleteGameObjectCommand(CommandCompletionContext ctx, List<CompletionItem> results)
        {
            string sub = FirstArg(ctx.Input);
            if (ctx.ArgumentIndex == 1 &&
                (sub.Equals("active", System.StringComparison.OrdinalIgnoreCase) ||
                 sub.Equals("tag", System.StringComparison.OrdinalIgnoreCase)))
            {
                CompleteGameObjects(ctx, results, null, "GameObject");
                return;
            }

            if (ctx.ArgumentIndex == 2 &&
                sub.Equals("active", System.StringComparison.OrdinalIgnoreCase))
                CompleteBooleans(ctx, results);
        }

        private static void CompleteGameObjects(CommandCompletionContext ctx,
            List<CompletionItem> results, System.Func<GameObject, bool> predicate, string detail)
        {
            var all = Object.FindObjectsByType<GameObject>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in all)
            {
                if (predicate != null && !predicate(go))
                    continue;
                if (!StartsWith(go.name, ctx.CurrentToken))
                    continue;

                results.Add(new CompletionItem(go.name, go.name,
                    detail + (go.activeInHierarchy ? "" : " (inactive)"),
                    "Scene", CompletionKind.Argument));
            }
        }

        private static void CompleteBooleans(CommandCompletionContext ctx, List<CompletionItem> results)
        {
            AddKeyword(ctx, results, "on");
            AddKeyword(ctx, results, "off");
            AddKeyword(ctx, results, "true");
            AddKeyword(ctx, results, "false");
        }

        private static void AddKeyword(CommandCompletionContext ctx, List<CompletionItem> results, string value)
        {
            if (!StartsWith(value, ctx.CurrentToken))
                return;

            results.Add(new CompletionItem(value, value,
                "boolean value", "Components", CompletionKind.Keyword));
        }

        private static string FirstArg(string input)
        {
            var parts = (input ?? string.Empty)
                .Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 1 ? parts[1] : string.Empty;
        }

        private static bool StartsWith(string value, string prefix)
            => string.IsNullOrEmpty(prefix) ||
               (!string.IsNullOrEmpty(value) &&
                value.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase));

        private static Transform FindTransform(CommandContext ctx, int argIndex)
        {
            if (!ctx.Has(argIndex)) { ctx.Output.WriteLine("GameObject name required.", LogLevel.Error); return null; }
            var go = FindGameObject(ctx.Args[argIndex], includeInactive: true);
            if (go == null) { ctx.Output.WriteLine($"GameObject '{ctx.Args[argIndex]}' not found.", LogLevel.Error); return null; }
            return go.transform;
        }

        private static T FindComponent<T>(CommandContext ctx, int argIndex) where T : Component
        {
            if (!ctx.Has(argIndex)) { ctx.Output.WriteLine("GameObject name required.", LogLevel.Error); return null; }
            var go = FindGameObject(ctx.Args[argIndex], includeInactive: true);
            if (go == null) { ctx.Output.WriteLine($"GameObject '{ctx.Args[argIndex]}' not found.", LogLevel.Error); return null; }
            var comp = go.GetComponent<T>();
            if (comp == null) { ctx.Output.WriteLine($"'{go.name}' has no {typeof(T).Name} component.", LogLevel.Error); return null; }
            return comp;
        }

        private static bool ParseXYZ(CommandContext ctx, int startIndex, out Vector3 result)
        {
            result = Vector3.zero;
            if (!TryParseFloat(ctx.Args[startIndex],     out float x) ||
                !TryParseFloat(ctx.Args[startIndex + 1], out float y) ||
                !TryParseFloat(ctx.Args[startIndex + 2], out float z))
            {
                ctx.Output.WriteLine("Invalid x y z values.", LogLevel.Error);
                return false;
            }
            result = new Vector3(x, y, z);
            return true;
        }

        private static bool TryParseFloat(CommandContext ctx, int index, out float value)
        {
            if (ctx.Has(index) && TryParseFloat(ctx.Args[index], out value))
                return true;

            value = default;
            ctx.Output.WriteLine("Invalid float.", LogLevel.Error);
            return false;
        }

        private static bool TryParseFloat(string raw, out float value)
            => float.TryParse(raw, NumberStyles.Float, Inv, out value);

        private static bool TryParseBool(CommandContext ctx, int index, out bool value)
        {
            if (ctx.Has(index) && TryParseBool(ctx.Args[index], out value))
                return true;

            value = default;
            ctx.Output.WriteLine("Invalid bool. Use: on/off/true/false/1/0/yes/no", LogLevel.Error);
            return false;
        }

        private static bool TryParseBool(string raw, out bool value)
        {
            if (raw == "1" || raw.Equals("true", System.StringComparison.OrdinalIgnoreCase) ||
                raw.Equals("on", System.StringComparison.OrdinalIgnoreCase) ||
                raw.Equals("yes", System.StringComparison.OrdinalIgnoreCase))
            {
                value = true;
                return true;
            }

            if (raw == "0" || raw.Equals("false", System.StringComparison.OrdinalIgnoreCase) ||
                raw.Equals("off", System.StringComparison.OrdinalIgnoreCase) ||
                raw.Equals("no", System.StringComparison.OrdinalIgnoreCase))
            {
                value = false;
                return true;
            }

            value = default;
            return false;
        }

        private static GameObject FindGameObject(string name, bool includeInactive)
        {
            var active = GameObject.Find(name);
            if (active != null || !includeInactive)
                return active;

            var all = Object.FindObjectsByType<GameObject>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in all)
                if (go.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                    return go;
            return null;
        }

        private static string FormatV3(Vector3 v)
            => string.Format(Inv, "({0:F2}, {1:F2}, {2:F2})", v.x, v.y, v.z);
    }
}
