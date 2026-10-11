// Attach to a separate Button, after browsing in Preview.
// Name the pickers "inputFile" / "outputFolder" and a Label "pathLabel".
forma.on("click", () => {
  const file = forma.get("inputFile", "selectedPath");
  const folder = forma.get("outputFolder", "selectedPath");
  forma.set("pathLabel", "text", `File: ${file || "None"}\nFolder: ${folder || "None"}`);
});
