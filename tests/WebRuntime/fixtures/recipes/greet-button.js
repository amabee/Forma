// Attach to the Button. Name the TextBox "nameInput" and Label "greetingLabel".
forma.on("click", () => {
  const name = String(forma.get("nameInput", "value") ?? "").trim();
  forma.set("greetingLabel", "text", name ? `Hello, ${name}!` : "Enter your name first.");
});
