using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UniTerminal.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UniTerminal.Modules
{
    /// <summary>
    /// Runtime object exploration via reflection.
    /// find &lt;name&gt; | inspect &lt;name&gt; | set &lt;name&gt;.&lt;member&gt; &lt;value&gt; | call &lt;name&gt;.&lt;method&gt; [args]
    ///
    /// Names resolve to: an explicitly registered object first, otherwise a GameObject
    /// found by name. Members are searched across the GameObject's components.
    /// </summary>
    public sealed class ObjectInspectorModule : ITerminalModule
    {
        private const BindingFlags Flags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private readonly Dictionary<string, object> _registered =
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        private ArgumentConverter _converter;

        public string Name => "Inspector";

        /// <summary>Register a named object (e.g. a service that is not a GameObject).</summary>
        public void RegisterObject(string name, object target)
        {
            if (string.IsNullOrWhiteSpace(name) || target == null) return;
            _registered[name] = target;
        }

        public void Install(Terminal terminal)
        {
            _converter = terminal.Converter;

            terminal.RegisterCommand(new DelegateCommand("find", Find,
                "Find GameObjects by name", "Inspector", "find <name>"));
            terminal.RegisterCommand(new DelegateCommand("inspect", Inspect,
                "Inspect an object's fields and properties", "Inspector", "inspect <name>"));
            terminal.RegisterCommand(new DelegateCommand("set", Set,
                "Set a member value", "Inspector", "set <name>.<member> <value>"));
            terminal.RegisterCommand(new DelegateCommand("call", Call,
                "Invoke a method", "Inspector", "call <name>.<method> [args...]"));
        }

        // ---- commands --------------------------------------------------------

        private void Find(CommandContext ctx)
        {
            if (!ctx.Has(0)) { ctx.Output.WriteLine("usage: find <name>", LogLevel.Error); return; }

            string query = ctx.Args[0];
            var all = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var sb = new StringBuilder();
            int found = 0;
            foreach (var go in all)
            {
                if (go.name.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0) continue;
                sb.AppendLine($"  {GetPath(go.transform)}  [{(go.activeInHierarchy ? "active" : "inactive")}]");
                if (++found >= 50) { sb.AppendLine("  ... (truncated)"); break; }
            }

            ctx.Output.WriteLine(found == 0 ? $"No GameObject matching '{query}'" : sb.ToString().TrimEnd(),
                found == 0 ? LogLevel.Warning : LogLevel.System);
        }

        private void Inspect(CommandContext ctx)
        {
            if (!ctx.Has(0)) { ctx.Output.WriteLine("usage: inspect <name>", LogLevel.Error); return; }
            if (!TryResolve(ctx.Args[0], out var target))
            {
                ctx.Output.WriteLine($"Cannot resolve '{ctx.Args[0]}'", LogLevel.Error);
                return;
            }

            var sb = new StringBuilder();
            if (target is GameObject go)
            {
                sb.AppendLine($"{go.name} (GameObject)");
                var components = go.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    var comp = components[i];
                    if (comp == null) continue;
                    AppendMembers(sb, comp, comp.GetType().Name);
                }
            }
            else
            {
                sb.AppendLine($"{ctx.Args[0]} ({target.GetType().Name})");
                AppendMembers(sb, target, target.GetType().Name);
            }

            ctx.Output.WriteLine(sb.ToString().TrimEnd(), LogLevel.System);
        }

        private void Set(CommandContext ctx)
        {
            if (!ctx.Has(1) || !SplitPath(ctx.Args[0], out var objName, out var member))
            {
                ctx.Output.WriteLine("usage: set <name>.<member> <value>", LogLevel.Error);
                return;
            }
            if (!TryResolve(objName, out var target))
            {
                ctx.Output.WriteLine($"Cannot resolve '{objName}'", LogLevel.Error);
                return;
            }

            if (!TryFindOwner(target, member, out var owner, out var field, out var prop))
            {
                ctx.Output.WriteLine($"No member '{member}' on '{objName}'", LogLevel.Error);
                return;
            }

            Type type = field != null ? field.FieldType : prop.PropertyType;
            object value;
            try { value = _converter.Convert(ctx.Args[1], type); }
            catch (Exception e)
            {
                ctx.Output.WriteLine($"Cannot convert '{ctx.Args[1]}' to {type.Name}: {e.Message}", LogLevel.Error);
                return;
            }

            try
            {
                if (field != null) field.SetValue(owner, value);
                else prop.SetValue(owner, value);
                ctx.Output.WriteLine($"{objName}.{member} = {value}", LogLevel.Success);
            }
            catch (Exception e)
            {
                ctx.Output.WriteLine($"Failed to set: {e.Message}", LogLevel.Error);
            }
        }

        private void Call(CommandContext ctx)
        {
            if (!ctx.Has(0) || !SplitPath(ctx.Args[0], out var objName, out var methodName))
            {
                ctx.Output.WriteLine("usage: call <name>.<method> [args...]", LogLevel.Error);
                return;
            }
            if (!TryResolve(objName, out var target))
            {
                ctx.Output.WriteLine($"Cannot resolve '{objName}'", LogLevel.Error);
                return;
            }

            int argCount = ctx.ArgCount - 1;
            if (!TryFindMethod(target, methodName, argCount, out var owner, out var method))
            {
                ctx.Output.WriteLine($"No method '{methodName}' taking {argCount} arg(s) on '{objName}'", LogLevel.Error);
                return;
            }

            var ps = method.GetParameters();
            var invokeArgs = new object[ps.Length];
            try
            {
                for (int i = 0; i < ps.Length; i++)
                    invokeArgs[i] = _converter.Convert(ctx.Args[i + 1], ps[i].ParameterType);
            }
            catch (Exception e)
            {
                ctx.Output.WriteLine($"Argument error: {e.Message}", LogLevel.Error);
                return;
            }

            try
            {
                object result = method.Invoke(owner, invokeArgs);
                ctx.Output.WriteLine(result != null ? $"=> {result}" : $"{methodName}() called", LogLevel.Success);
            }
            catch (TargetInvocationException tie)
            {
                ctx.Output.WriteLine($"Error: {tie.InnerException?.Message ?? tie.Message}", LogLevel.Error);
            }
        }

        // ---- resolution & reflection ----------------------------------------

        private bool TryResolve(string name, out object target)
        {
            if (_registered.TryGetValue(name, out target) && target != null)
                return true;

            var go = GameObject.Find(name);
            if (go != null) { target = go; return true; }

            var all = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var candidate in all)
            {
                if (candidate.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    target = candidate;
                    return true;
                }
            }

            target = null;
            return false;
        }

        private static bool TryFindOwner(object target, string member,
            out object owner, out FieldInfo field, out PropertyInfo prop)
        {
            foreach (var host in Hosts(target))
            {
                var t = host.GetType();
                field = t.GetField(member, Flags);
                if (field != null) { owner = host; prop = null; return true; }

                prop = t.GetProperty(member, Flags);
                if (prop != null && prop.CanWrite) { owner = host; field = null; return true; }
            }
            owner = null; field = null; prop = null;
            return false;
        }

        private static bool TryFindMethod(object target, string name, int argCount,
            out object owner, out MethodInfo method)
        {
            foreach (var host in Hosts(target))
            {
                foreach (var m in host.GetType().GetMethods(Flags))
                {
                    if (!m.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) continue;
                    if (m.GetParameters().Length != argCount) continue;
                    owner = host; method = m; return true;
                }
            }
            owner = null; method = null;
            return false;
        }

        /// <summary>For a GameObject, the candidate hosts are its components; otherwise itself.</summary>
        private static IEnumerable<object> Hosts(object target)
        {
            if (target is GameObject go)
            {
                var components = go.GetComponents<Component>();
                foreach (var c in components)
                    if (c != null) yield return c;
            }
            else
            {
                yield return target;
            }
        }

        private static void AppendMembers(StringBuilder sb, object host, string owner)
        {
            var type = host.GetType();

            foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                sb.AppendLine($"  ├ [{owner}] {f.Name}: {SafeValue(() => f.GetValue(host))}");

            foreach (var f in type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
                if (Attribute.IsDefined(f, typeof(SerializeField)))
                    sb.AppendLine($"  ├ [{owner}] {f.Name}: {SafeValue(() => f.GetValue(host))}");

            foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!p.CanRead || p.GetIndexParameters().Length > 0) continue;
                sb.AppendLine($"  ├ [{owner}] {p.Name}: {SafeValue(() => p.GetValue(host))}");
            }
        }

        private static string SafeValue(Func<object> getter)
        {
            try { var v = getter(); return v?.ToString() ?? "null"; }
            catch (Exception e) { return $"<err: {e.GetType().Name}>"; }
        }

        private static bool SplitPath(string raw, out string objName, out string member)
        {
            int dot = raw.IndexOf('.');
            if (dot <= 0 || dot >= raw.Length - 1)
            {
                objName = raw; member = null;
                return false;
            }
            objName = raw.Substring(0, dot);
            member = raw.Substring(dot + 1);
            return true;
        }

        private static string GetPath(Transform t)
        {
            var sb = new StringBuilder(t.name);
            while (t.parent != null)
            {
                t = t.parent;
                sb.Insert(0, t.name + "/");
            }
            return sb.ToString();
        }
    }
}
