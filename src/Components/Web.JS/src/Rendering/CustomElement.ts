import { RootComponentsFunctions } from "./JSRootComponents";

// We track the attribute->parameter mappings internally so that the CustomElement class knows what
// parameters are on each custom element without having to pass that info from subclasses.
type CustomElementAttributeMapping = { [attributeName: string]: CustomElementParameterInfo };
export const attributeMappingsByElementName: { [customElementName: string]: CustomElementAttributeMapping } = {};

// Defines the signature of a custom element initializer callback. The built-in default initializer
// uses the built-in CustomElement class.
export interface RegisterCustomElementCallback {
    (name: string, parameters: CustomElementParameterInfo[]): void;
}

interface CustomElementParameterInfo {
    name: string;
}

export function defaultRegisterCustomElement(elementName: string, parameters: CustomElementParameterInfo[]): void {
    // Default logic for registering a custom element is just to use the Blazor.CustomElement class
    customElements.define(elementName, class ConfiguredCustomElement extends CustomElement {
        static get observedAttributes() { return CustomElement.getObservedAttributes(elementName); }
    });
}

export class CustomElement extends HTMLElement {
    private _attributeMappings: CustomElementAttributeMapping;
    private _parameterValues: { [dotNetName: string]: any } = {};
    private _addRootComponentPromise: Promise<any>;
    private _hasPendingSetParameters = true; // The constructor will call setParameters, so it starts true
    private _isDisposed = false;
    private _disposalTimeoutHandle: any;

    // Subclasses will need to call this if they want to retain the built-in behavior for knowing which
    // attribute names to observe, since they have to return it from a static function
    static getObservedAttributes(elementName: string): string[] {
        return Object.keys(attributeMappingsByElementName[elementName]);
    }

    constructor() {
        super();

        this._attributeMappings = attributeMappingsByElementName[this.localName];
        if (!this._attributeMappings) {
            throw new Error(`The tag name ${this.localName} was not registered as a custom element.`);
        }

        // Defer until end of execution cycle so that (1) we know the heap is unlocked, and (2) the initial parameter
        // values will be populated from the initial attributes before we send them to .NET
        this._addRootComponentPromise = Promise.resolve().then(() => {
            this._hasPendingSetParameters = false;

            // This is the same as calling Blazor.rootComponents.add(...)
            return RootComponentsFunctions.add(this, this.localName, this._parameterValues);
        });

        // Also allow assignment of parameters via properties. This is the only way to set complex-typed values.
        for (const [attributeName, parameterInfo] of Object.entries(this._attributeMappings)) {
            const dotNetName = parameterInfo.name;
            const camelCaseName = dotNetName[0].toLowerCase() + dotNetName.substr(1);
            Object.defineProperty(this, camelCaseName, {
                get: () => this._parameterValues[dotNetName],
                set: newValue => {
                    if (this.hasAttribute(attributeName)) {
                        // It's nice to keep the DOM in sync with the properties. This set a string representation
                        // of the value, but this will get overwritten with the original typed value before we send it to .NET
                        this.setAttribute(attributeName, newValue);
                    }

                    this._parameterValues[dotNetName] = newValue;
                    this._supplyUpdatedParameters();
                }
            });
        }
    }

    connectedCallback() {
        if (this._isDisposed) {
            throw new Error(`Cannot connect component ${this.localName} to the document after it has been disposed.`);
        }

        clearTimeout(this._disposalTimeoutHandle);
    }

    disconnectedCallback() {
        this._disposalTimeoutHandle = setTimeout(async () => {
            this._isDisposed = true;
            const rootComponent = await this._addRootComponentPromise;
            rootComponent.dispose();
        }, 1000);
    }

    attributeChangedCallback(name: string, oldValue: string, newValue: string) {
        const parameterInfo = this._attributeMappings[name];
        if (parameterInfo) {
            this._parameterValues[parameterInfo.name] = newValue;
            this._supplyUpdatedParameters();
        }
    }

    private async _supplyUpdatedParameters() {
        if (!this._hasPendingSetParameters) {
            this._hasPendingSetParameters = true;

            // Continuation from here will always be async, so at the earliest it will be at
            // the end of the current JS execution cycle
            const rootComponent = await this._addRootComponentPromise;
            if (!this._isDisposed) {
                const setParametersPromise = rootComponent.setParameters(this._parameterValues);
                this._hasPendingSetParameters = false; // We just snapshotted _parameterValues, so we need to start allowing new calls in case it changes further
                await setParametersPromise;
            }
        }
    }
}
