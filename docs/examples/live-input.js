// Attach to the TextBox. Name the Label "greetingLabel".
api.on("input", () => {
  api.set("greetingLabel", "text", component.element.value);
});
