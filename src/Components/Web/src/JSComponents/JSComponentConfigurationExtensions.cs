// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Infrastructure;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.Web
{
    /// <summary>
    /// Extension methods for working on an <see cref="IJSComponentConfiguration"/>.
    /// </summary>
    public static class JSComponentConfigurationExtensions
    {
        /// <summary>
        /// Marks the specified component type as allowed for instantiation from JavaScript.
        /// </summary>
        /// <typeparam name="TComponent">The component type.</typeparam>
        /// <param name="configuration">The <see cref="IJSComponentConfiguration"/>.</param>
        /// <param name="identifier">A unique identifier that will be used by JavaScript code.</param>
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(JSComponentInterop))]
        public static void RegisterForJavaScript<[DynamicallyAccessedMembers(Component)] TComponent>(this IJSComponentConfiguration configuration, string identifier) where TComponent : IComponent
            => configuration.JSComponents.Add(typeof(TComponent), identifier);

        /// <summary>
        /// Allows the specified component type to be used as a custom element.
        /// </summary>
        /// <typeparam name="TComponent">The component type.</typeparam>
        /// <param name="configuration">The <see cref="IJSComponentConfiguration"/>.</param>
        /// <param name="customElementName">A unique name for the custom element. This must conform to custom element naming rules, so it must contain a dash character.</param>
        /// <param name="javaScriptInitializer">Optional. Specifies the identifier for a JavaScript function that will be called to register the custom element. If not specified, the framework will use a default custom element implementation.</param>
        public static void RegisterAsCustomElement<TComponent>(this IJSComponentConfiguration configuration, string customElementName, string? javaScriptInitializer = null) where TComponent: IComponent
            => configuration.JSComponents.AddCustomElement(typeof(TComponent), customElementName, javaScriptInitializer);
    }
}
