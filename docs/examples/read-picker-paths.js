// Attach to a separate Button, after browsing in Preview.
// Name the pickers "inputFile" / "outputFolder" and a Label "pathLabel".
api.on("click", () => {
  const file = api.get("inputFile", "selectedPath");
  const folder = api.get("outputFolder", "selectedPath");
  api.set("pathLabel", "text", `File: ${file || "None"}\nFolder: ${folder || "None"}`);
});
