// Attach to the DataGridView. Name a Label "rowLabel".
forma.on("row-selection", event => {
  const row = event.detail.row;
  forma.set("rowLabel", "text", row < 0 ? "No row selected" : `Source row: ${row}`);
});
