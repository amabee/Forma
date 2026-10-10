// Attach to the TextBox. Name the Label "greetingLabel".
forma.on("input", () => {
  forma.set("greetingLabel", "text", component.element.value);
});
