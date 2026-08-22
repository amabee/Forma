window.forma = {
  receive(message) {
    console.log("Forma command:", message);

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
    }
  },

  send(message) {
    // Posted as a structured object, not a string, so the host can read it
    // with WebMessageAsJson.
    window.chrome.webview.postMessage(message);
  },

  create(message) {
    let element;

    switch (message.control) {
      case "button":
        element = document.createElement("button");
        break;

      case "textbox":
        element = document.createElement("input");

        element.type = "text";
        break;

      default:
        console.error("Unknown control:", message.control);

        return;
    }

    element.id = message.id;

    if (message.properties?.text) {
      element.textContent = message.properties.text;
    }

    document.body.appendChild(element);

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

    const properties = message.properties ?? {};

    if ("text" in properties) {
      if (element.tagName === "INPUT") {
        element.value = properties.text ?? "";
      } else {
        element.textContent = properties.text ?? "";
      }
    }
  },

  remove(message) {
    const element = document.getElementById(message.id);

    element?.remove();
  },
};

// The host delivers commands through PostWebMessageAsJson, which surfaces here
// as a message event carrying the already-parsed object.
window.chrome.webview.addEventListener("message", (event) => {
  window.forma.receive(event.data);
});
