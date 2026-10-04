// Attach to the Button. Name the TextBox "nameInput" and Label "greetingLabel".
api.on("click", () => {
  const name = String(api.get("nameInput", "value") ?? "").trim();
  api.set("greetingLabel", "text", name ? `Hello, ${name}!` : "Enter your name first.");
});
