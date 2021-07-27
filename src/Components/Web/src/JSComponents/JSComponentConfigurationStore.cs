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

            var parameters = JSComponentInterop.GetComponentParameters(componentType).ParameterTypes.Keys.ToArray();

            customElements.Add(new CustomElement
            {
                Name = customElementName,
                Parameters = parameters,
            });
        }

        internal struct CustomElement
        {
            public string Name { get; init; }
            public string[] Parameters { get; init; }
        }
    }
}
