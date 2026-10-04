// Attach to the DataGridView. Name a Label "rowLabel".
api.on("row-selection", event => {
  const row = event.detail.row;
  api.set("rowLabel", "text", row < 0 ? "No row selected" : `Source row: ${row}`);
});
