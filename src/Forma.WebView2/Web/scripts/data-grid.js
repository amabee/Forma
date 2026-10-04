window.formaDataGrid = {
  render(element, update) {
    const previousSelection = element._gridProperties?.selectedRow ?? -1;
    const properties = element._gridProperties = { ...element._gridProperties, ...update };
    const runtime = window.formaDesigner?.preview !== false;
    const enabled = runtime && element.dataset.disabled !== "true";
    const columns = properties.columns ?? [], rows = properties.rows ?? [];
    const emit = (event, payload) => window.forma.send({ type: "event", id: element.id, event, payload });
    let filter = element.querySelector(":scope > .grid-filter");
    if (!filter) {
      filter = document.createElement("input"); filter.type = "search"; filter.className = "grid-filter";
      filter.placeholder = "Filter rows…"; filter.setAttribute("aria-label", "Filter grid rows");
      filter.addEventListener("input", () => {
        if (window.formaDesigner?.preview !== false && element.dataset.disabled !== "true" && element._gridProperties.filteringEnabled !== false)
          emit("grid-filter", { text: filter.value });
      });
      element.appendChild(filter);
    }
    filter.hidden = properties.filteringEnabled === false; filter.disabled = !enabled;
    if (filter.value !== (properties.filterText ?? "")) filter.value = properties.filterText ?? "";
    let table = element.querySelector(":scope > table");
    if (!table) { table = document.createElement("table"); element.appendChild(table); }
    const schema = JSON.stringify({ columns, rows, enabled, readOnly: properties.readOnly,
      sortingEnabled: properties.sortingEnabled, sortColumn: properties.sortColumn, sortDirection: properties.sortDirection,
      filteringEnabled: properties.filteringEnabled, filterText: properties.filterText, visibleRowIndices: properties.visibleRowIndices });
    if (element.dataset.gridSchema === schema) {
      table.querySelectorAll('[data-grid-row]').forEach(row => row.setAttribute("aria-selected", String(Number(row.dataset.gridRow) === properties.selectedRow)));
      if (previousSelection !== (properties.selectedRow ?? -1)) element.dispatchEvent(new CustomEvent("row-selection", { detail: { row: properties.selectedRow } }));
      return;
    }
    element.dataset.gridSchema = schema;
    const focusedRow = document.activeElement?.closest("tr[data-grid-row]")?.dataset.gridRow;
    const head = document.createElement("thead"), body = document.createElement("tbody"), heading = document.createElement("tr");
    columns.forEach((text, column) => {
      const th = document.createElement("th"), button = document.createElement("button"); button.type = "button";
      const sorted = properties.sortingEnabled !== false && properties.sortColumn === column;
      th.setAttribute("aria-sort", sorted ? properties.sortDirection ?? "ascending" : "none");
      button.textContent = text + (sorted ? properties.sortDirection === "descending" ? " ▼" : " ▲" : "");
      button.disabled = !enabled || properties.sortingEnabled === false;
      button.addEventListener("click", () => emit("grid-sort", { column })); th.appendChild(button); heading.appendChild(th);
    }); head.appendChild(heading);
    let indices = properties.visibleRowIndices;
    if (!indices) {
      const query = properties.filteringEnabled === false ? "" : (properties.filterText ?? "").toLowerCase();
      indices = rows.map((_, index) => index).filter(index => rows[index].slice(0, columns.length).some(cell => String(cell).toLowerCase().includes(query)));
      if (properties.sortingEnabled !== false && properties.sortColumn >= 0) indices.sort((a, b) => {
        const left = rows[a][properties.sortColumn] ?? "", right = rows[b][properties.sortColumn] ?? "";
        const comparison = left.trim() && right.trim() && Number.isFinite(Number(left)) && Number.isFinite(Number(right))
          ? Number(left) - Number(right) : left.toLowerCase().localeCompare(right.toLowerCase());
        return (properties.sortDirection === "descending" ? -comparison : comparison) || a - b;
      });
    }
    indices.forEach((row, viewIndex) => {
      const tr = document.createElement("tr"); tr.dataset.gridRow = row; tr.tabIndex = enabled ? 0 : -1;
      tr.setAttribute("aria-selected", String(properties.selectedRow === row));
      tr.addEventListener("click", () => { if (enabled) emit("grid-select", { row }); });
      tr.addEventListener("keydown", event => {
        if (!enabled || event.target.closest('[contenteditable="true"]')) return;
        if (event.key === "Enter" || event.key === " ") { event.preventDefault(); emit("grid-select", { row }); }
        if (event.key === "ArrowUp" || event.key === "ArrowDown") {
          event.preventDefault(); const next = indices[viewIndex + (event.key === "ArrowDown" ? 1 : -1)];
          if (next !== undefined) { table.querySelector(`[data-grid-row="${next}"]`)?.focus(); emit("grid-select", { row: next }); }
        }
      });
      columns.forEach((_, column) => {
        const td = document.createElement("td"), original = rows[row]?.[column] ?? "";
        td.textContent = original; td.contentEditable = enabled && !properties.readOnly ? "true" : "false";
        td.addEventListener("blur", () => {
          if (td.contentEditable === "true" && td.textContent !== original) emit("cell", { row, column, value: td.textContent });
        }); tr.appendChild(td);
      }); body.appendChild(tr);
    });
    if (!indices.length) {
      const row = document.createElement("tr"), cell = document.createElement("td"); cell.colSpan = Math.max(1, columns.length);
      cell.className = "grid-empty"; cell.textContent = "No matching rows"; row.appendChild(cell); body.appendChild(row);
    }
    table.replaceChildren(head, body);
    if (focusedRow != null) table.querySelector(`[data-grid-row="${focusedRow}"]`)?.focus();
    if (previousSelection !== (properties.selectedRow ?? -1)) element.dispatchEvent(new CustomEvent("row-selection", { detail: { row: properties.selectedRow } }));
  }
};
