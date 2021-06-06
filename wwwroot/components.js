import { LitElement, html } from 'https://unpkg.com/lit@2.0.0-rc.2/index.js?module';
import * as Turbo from "https://unpkg.com/@hotwired/turbo@7.0.0-beta.5/dist/turbo.es2017-esm.js?module";

/**
 * A simple counter
 */
class MyCounter extends LitElement {

    static get properties() {
        return {
            name: { type: String },
            _counter: { state: true }
        };
    }
    constructor() {
        super();
        this._counter = 0;
        this.name = "";
    }

    render() {
        return html`
            <p>Client side Counter after server rendered: ${this._counter}</p>
            <p>Also... ${this.name}</p>
            <sl-button type="primary" @click="${() => this._counter += 1}">
                <sl-icon slot="prefix" name="plus"></sl-icon>
                Increment
            </sl-button>
            <sl-button type="primary" @click="${() => this._counter -= 1}">
                <sl-icon slot="prefix" name="dash"></sl-icon>
                Decrement
            </sl-button>
        `;
    }

}

class GoBackButton extends LitElement {
    static get properties() {
        return {
            href: { type: String }
        };
    }
    constructor() {
        super();
        this.href = "/";
    }

    render() {
        return html`
            <sl-button type="text"  @click=${() => Turbo.visit(this.href)}>
                <slot></slot>
            </sl-button>
        `;
    }
}


customElements.define("my-counter", MyCounter);
customElements.define("my-go-back-button", GoBackButton);