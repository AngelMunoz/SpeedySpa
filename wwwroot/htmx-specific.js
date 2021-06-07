window.addEventListener(
    "htmx:xhr:loadstart",
    function onHtmxXhrLoadStart(event) {
        const el = document.querySelector("#global-progress");
        el.removeAttribute("hidden");
    }
);

window.addEventListener(
    "htmx:xhr:loadend",
    function onHtmxXhrLoadEnd(event) {
        const el = document.querySelector("#global-progress");
        el.setAttribute("hidden", "");
    }
);

window.addEventListener(
    "htmx:xhr:progress",
    function onHtmxXhrProgress(event) {
        console.log(event);
    }
);