// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Web.Infrastructure;

namespace Microsoft.AspNetCore.Components.Web
{
    /// <summary>
    /// Specifies options for use when enabling JS component support.
    /// This type is not normally used directly from application code. In most cases, applications should
    /// call methods on the <see cref="IJSComponentConfiguration" /> on their application host builder.
    /// </summary>
    public sealed class JSComponentConfigurationStore
    {
        const string DefaultJavaScriptInitializer = "Blazor._internal.registerCustomElement";

        // Everything's internal here, and can only be operated upon via the extension methods on
        // IJSComponentConfiguration. This is so that, in the future, we can add any additional
        // configuration APIs (as further extension methods) and/or storage (as internal members here)
        // without needing any changes on the downstream code that implements IJSComponentConfiguration,
        // and without exposing any of the configuration storage across layers.

        internal Dictionary<string, Type> JsComponentTypesByIdentifier { get; } = new (StringComparer.Ordinal);

        internal Dictionary<string, List<CustomElement>> CustomElementsByInitializer { get; } = new();

        internal void Add(Type componentType, string identifier)
            => JsComponentTypesByIdentifier.Add(identifier, componentType);

        internal void AddCustomElement(Type componentType, string customElementName, string? javaScriptInitializer)
        {
            Add(componentType, customElementName);

            var initializer = javaScriptInitializer ?? DefaultJavaScriptInitializer;

            if (!CustomElementsByInitializer.TryGetValue(initializer, out var customElements))
            {
                customElements = new();
                CustomElementsByInitializer.Add(initializer, customElements);
            }

            var parameters = JSComponentInterop.GetComponentParameters(componentType).ParameterTypes;
            var parameterInfoForJs = new CustomElementParameter[parameters.Count];
            var index = 0;
            foreach (var (name, type) in parameters)
            {
                parameterInfoForJs[index++] = new CustomElementParameter(name, type);
            }

            customElements.Add(new CustomElement
            {
                Name = customElementName,
                Parameters = parameterInfoForJs,
            });
        }

        internal readonly struct CustomElement
        {
            public readonly string Name { get; init; }
            public readonly CustomElementParameter[] Parameters { get; init; }
        }

        internal readonly struct CustomElementParameter
        {
            public string Name { get; }
            public string Type { get; }

            public CustomElementParameter(string name, Type type)
            {
                Name = name;
                Type = GetJSType(type);
            }

            private static string GetJSType(Type type) => type switch
            {
                var x when x == typeof(string) => "string",
                var x when x == typeof(bool) => "boolean",
                var x when x == typeof(bool?) => "boolean?",
                var x when x == typeof(decimal) => "number",
                var x when x == typeof(decimal?) => "number?",
                var x when x == typeof(double) => "number",
                var x when x == typeof(double?) => "number?",
                var x when x == typeof(float) => "number",
                var x when x == typeof(float?) => "number?",
                var x when x == typeof(int) => "number",
                var x when x == typeof(int?) => "number?",
                var x when x == typeof(long) => "number",
                var x when x == typeof(long?) => "number?",
                _ => "object"
            };
        }
    }
}
