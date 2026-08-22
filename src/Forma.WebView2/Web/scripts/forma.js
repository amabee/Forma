// How each control kind maps onto the DOM.
//
// `text` decides what the control's Text property means:
//   "content" - written to textContent
//   "value"   - written to the value property (form fields)
//   "none"    - ignored, so containers never overwrite their own children
const CONTROL_TYPES = {
  button: { tag: "button", text: "content" },
  label: { tag: "span", text: "content" },
  textbox: {
    tag: "input",
    text: "value",
    init: (element) => {
      element.type = "text";
    },
  },
  panel: { tag: "div", text: "none" },
  form: {
    tag: "div",
    text: "none",
    init: (element) => element.classList.add("forma-form"),
  },
};

window.forma = {
  receive(message) {
    switch (message.type) {
      case "create":
        this.create(message);
        break;

      case "update":
        this.update(message);
        break;

      case "remove":
        this.remove(message);
        break;

      default:
        console.error("Unknown Forma command:", message.type);
    }
  },

  send(message) {
    // Posted as a structured object, not a string, so the host can read it
    // with WebMessageAsJson.
    window.chrome.webview.postMessage(message);
  },

  applyText(element, spec, value) {
    if (spec.text === "value") {
      element.value = value ?? "";
    } else if (spec.text === "content") {
      element.textContent = value ?? "";
    }
  },

  create(message) {
    const spec = CONTROL_TYPES[message.control];

    if (!spec) {
      console.error("Unknown control:", message.control);

      return;
    }

    // A named parent that is missing means the tree arrived out of order.
    // Falling back to the body would silently misplace the control, so bail.
    const parent = message.parentId
      ? document.getElementById(message.parentId)
      : document.body;

    if (!parent) {
      console.error(
        `Cannot create '${message.id}': parent '${message.parentId}' not found.`,
      );

      return;
    }

    const element = document.createElement(spec.tag);

    element.id = message.id;

    // Recorded so update() knows how to apply text without being told the
    // control kind again.
    element.dataset.formaType = message.control;

    spec.init?.(element);

    const properties = message.properties ?? {};

    if ("text" in properties) {
      this.applyText(element, spec, properties.text);
    }

    parent.appendChild(element);

    if (message.control === "button") {
      element.addEventListener("click", () => {
        window.forma.send({
          type: "event",
          id: message.id,
          event: "click",
          payload: {},
        });
      });
    }
  },

  update(message) {
    const element = document.getElementById(message.id);

    if (!element) return;

    const spec = CONTROL_TYPES[element.dataset.formaType];

    if (!spec) return;

    const properties = message.properties ?? {};

    if ("text" in properties) {
      this.applyText(element, spec, properties.text);
    }
  },

  remove(message) {
    const element = document.getElementById(message.id);

    // Detaching an element takes its descendants with it, which is why the
    // host only sends one remove per subtree.
    element?.remove();
  },
};

// The host delivers commands through PostWebMessageAsJson, which surfaces here
// as a message event carrying the already-parsed object.
window.chrome.webview.addEventListener("message", (event) => {
  window.forma.receive(event.data);
});
