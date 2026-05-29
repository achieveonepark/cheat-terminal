using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Achieve.CheatTerminal.Core
{
    /// <summary>
    /// An <see cref="ICommand"/> that invokes a [Terminal]-annotated method via reflection.
    /// Template "gold {0}" -> command "gold", arg 0 -> method parameter 0.
    /// A <see cref="CommandContext"/> parameter is injected and never consumes a user arg.
    /// </summary>
    public sealed class AttributeCommand : ICommand
    {
        private static readonly Regex Placeholder = new Regex(@"^\{(\d+)\}$", RegexOptions.Compiled);

        private readonly MethodInfo _method;
        private readonly object _target;
        private readonly ArgumentConverter _converter;
        private readonly ParameterInfo[] _parameters;
        private readonly int[] _argToParam;   // supplied arg index -> method parameter index
        private readonly int _contextParamIndex = -1;

        public string Name { get; }
        public string Description { get; }
        public string Category { get; }
        public string Usage { get; }

        public AttributeCommand(TerminalAttribute attribute, MethodInfo method,
            object target, ArgumentConverter converter)
        {
            _method = method ?? throw new ArgumentNullException(nameof(method));
            _target = target;
            _converter = converter ?? throw new ArgumentNullException(nameof(converter));
            _parameters = method.GetParameters();

            for (int i = 0; i < _parameters.Length; i++)
            {
                if (_parameters[i].ParameterType == typeof(CommandContext))
                {
                    _contextParamIndex = i;
                    break;
                }
            }

            Name = ParseTemplate(attribute.Template, out _argToParam);
            Description = attribute.Description ?? string.Empty;
            Category = string.IsNullOrEmpty(attribute.Category) ? "General" : attribute.Category;
            Usage = BuildUsage();
        }

        public void Execute(CommandContext context)
        {
            var invokeArgs = new object[_parameters.Length];

            for (int p = 0; p < _parameters.Length; p++)
            {
                if (p == _contextParamIndex)
                    invokeArgs[p] = context;
                else if (_parameters[p].HasDefaultValue)
                    invokeArgs[p] = _parameters[p].DefaultValue;
                else
                    invokeArgs[p] = GetDefault(_parameters[p].ParameterType);
            }

            var args = context.Args;
            int bind = Math.Min(_argToParam.Length, args.Count);
            for (int i = 0; i < bind; i++)
            {
                int p = _argToParam[i];
                var type = _parameters[p].ParameterType;
                try
                {
                    invokeArgs[p] = _converter.Convert(args[i], type);
                }
                catch (Exception)
                {
                    context.Output.WriteLine(
                        $"Argument '{args[i]}' is not a valid {type.Name}. Usage: {Usage}",
                        LogLevel.Error);
                    return;
                }
            }

            int required = CountRequiredArgs();
            if (args.Count < required)
            {
                context.Output.WriteLine($"Missing arguments. Usage: {Usage}", LogLevel.Error);
                return;
            }

            object result = _method.Invoke(_target, invokeArgs);
            if (result != null)
                context.Output.WriteLine(result.ToString(), LogLevel.Info);
        }

        private string ParseTemplate(string template, out int[] argToParam)
        {
            if (string.IsNullOrWhiteSpace(template))
                throw new ArgumentException("Terminal template cannot be empty.");

            var tokens = template.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string name = tokens[0];

            var order = new List<int>();
            for (int i = 0; i < tokens.Length; i++)
            {
                var m = Placeholder.Match(tokens[i]);
                if (m.Success)
                    order.Add(int.Parse(m.Groups[1].Value));
            }

            if (order.Count == 0)
            {
                // No placeholders: bind positionally to all non-context parameters.
                for (int p = 0; p < _parameters.Length; p++)
                    if (p != _contextParamIndex)
                        order.Add(p);
            }

            argToParam = order.ToArray();
            return name;
        }

        private int CountRequiredArgs()
        {
            int count = 0;
            foreach (int p in _argToParam)
                if (!_parameters[p].HasDefaultValue)
                    count++;
            return count;
        }

        private string BuildUsage()
        {
            var sb = new StringBuilder(Name);
            foreach (int p in _argToParam)
            {
                var param = _parameters[p];
                sb.Append(param.HasDefaultValue
                    ? $" [{param.Name}]"
                    : $" <{param.Name}>");
            }
            return sb.ToString();
        }

        private static object GetDefault(Type type)
            => type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}
