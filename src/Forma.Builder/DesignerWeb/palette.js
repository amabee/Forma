// Ctrl+K command palette (menu commands, toolbox controls, controls on the form, forms)
// and the keyboard shortcuts sheet (F1, Help > Keyboard shortcuts).
(() => {
  const d = window.formaDesigner;
  if (!d || d.standalone) return;
  const send = (event, id, payload = {}) => window.forma.send({ type: "designer", id, event, payload });
  const SHORTCUTS = [
    ["General", [["Ctrl+K", "Command palette"], ["F1", "Keyboard shortcuts"], ["Ctrl+N", "New form"], ["Ctrl+Shift+N", "New project"], ["Ctrl+O", "Open"], ["Ctrl+S", "Save"], ["Ctrl+Shift+S", "Save as"], ["Ctrl+Z / Ctrl+Y", "Undo / Redo"], ["Esc", "Close menus and dialogs, leave Preview"]]],
    ["Selection", [["Click", "Select a control"], ["Ctrl/Shift+Click", "Add or remove from the selection"], ["Drag on the form", "Select controls in a rectangle"], ["Ctrl+A", "Select all controls on the form"], ["Esc", "Keep only the main selected control"]]],
    ["Editing", [["Ctrl+C / Ctrl+X / Ctrl+V", "Copy / Cut / Paste"], ["Ctrl+D", "Duplicate"], ["Delete", "Delete the selection"], ["Arrows", "Move by 1px (Shift: 10px)"], ["Alt+Shift+Arrows", "Resize by 1px (Ctrl: 10px)"], ["Alt while dragging", "Turn off snapping"], ["Double-click a control", "Open its default event handler"]]],
    ["View", [["Ctrl+Wheel, Ctrl+= / Ctrl+-", "Zoom the canvas"], ["Ctrl+0", "Zoom to 100%"]]],
  ];
  let dialog = null;

  function element(tag, className, text) {
    const node = document.createElement(tag);
    if (className) node.className = className;
    if (text !== undefined) node.textContent = text;
    return node;
  }

  function close() {
    if (!dialog) return;
    const { node, restore } = dialog;
    node.remove(); dialog = null;
    restore?.focus?.();
  }

  function open(build, label) {
    close();
    const node = element("div", "palette-backdrop");
    node.addEventListener("pointerdown", event => { if (event.target === node) close(); });
    const panel = element("div", "palette");
    panel.setAttribute("role", "dialog"); panel.setAttribute("aria-modal", "true"); panel.setAttribute("aria-label", label);
    node.append(panel);
    dialog = { node, restore: document.activeElement };
    build(panel);
    document.body.append(node);
    panel.querySelector("input, button")?.focus();
  }

  const text = button => [...button.childNodes].filter(node => node.nodeType === 3).map(node => node.textContent).join(" ").replace(/\s+/g, " ").trim() || button.getAttribute("aria-label") || "";

  /** Everything the palette can run, rebuilt each time it opens so it reflects the current design. */
  function entries() {
    const items = [];
    for (const button of document.querySelectorAll('.menubar .menu button:not([data-palette="palette"])')) {
      if (button.disabled) continue;
      const shortcut = button.querySelector("kbd")?.textContent ?? "";
      const menu = button.closest("details")?.querySelector("summary")?.textContent ?? "";
      items.push({ group: "Command", label: text(button), hint: [menu, shortcut].filter(Boolean).join(" · "), run: () => button.click() });
    }
    const seen = new Set();
    for (const tool of document.querySelectorAll(".tool-group:not(.recent-tools) [data-kind]")) {
      if (seen.has(tool.dataset.kind)) continue;
      seen.add(tool.dataset.kind);
      items.push({ group: "Add", label: `Add ${tool.textContent.trim()}`, hint: "Toolbox", run: () => tool.dispatchEvent(new MouseEvent("dblclick", { bubbles: true })) });
    }
    for (const control of d.state?.controls ?? []) {
      if (control.id === d.rootId) continue;
      items.push({ group: "Select", label: `Go to ${control.name}`, hint: control.kind, run: () => { d.select(control.id); send("select", control.id); byIdScroll(control.id); } });
    }
    for (const form of d.state?.forms ?? [])
      if (form.id !== d.rootId) items.push({ group: "Form", label: `Switch to ${form.title || form.name}`, hint: form.name, run: () => send("select-form", d.rootId, { id: form.id }) });
    return items;
  }
  const byIdScroll = id => document.getElementById(id)?.scrollIntoView?.({ block: "nearest", inline: "nearest" });

  /** Every word of the query must appear, in any order; earlier matches rank first. */
  function rank(items, query) {
    const words = query.toLowerCase().split(/\s+/).filter(Boolean);
    if (!words.length) return items.filter(item => item.group === "Command");
    return items.map(item => {
      const haystack = `${item.label} ${item.hint}`.toLowerCase();
      if (!words.every(word => haystack.includes(word))) return null;
      return { item, score: words.reduce((sum, word) => sum + haystack.indexOf(word), 0) + (item.label.toLowerCase().startsWith(words[0]) ? -100 : 0) };
    }).filter(Boolean).sort((a, b) => a.score - b.score).map(entry => entry.item);
  }

  function openPalette() {
    if (d.preview) return;
    open(panel => {
      const items = entries();
      const input = element("input", "palette-input");
      input.type = "text"; input.placeholder = "Type a command, control or form…";
      input.setAttribute("role", "combobox"); input.setAttribute("aria-expanded", "true"); input.setAttribute("aria-controls", "palette-results");
      const list = element("ul", "palette-results");
      list.id = "palette-results"; list.setAttribute("role", "listbox");
      let shown = [], active = 0;
      const run = item => { close(); item.run(); };
      const render = () => {
        shown = rank(items, input.value).slice(0, 50);
        active = Math.min(active, Math.max(0, shown.length - 1));
        list.replaceChildren(...shown.map((item, index) => {
          const row = element("li", "palette-item");
          row.id = `palette-option-${index}`; row.setAttribute("role", "option"); row.setAttribute("aria-selected", String(index === active));
          row.append(element("span", "palette-group", item.group), element("span", "palette-label", item.label), element("span", "palette-hint", item.hint));
          row.addEventListener("pointerdown", event => { event.preventDefault(); run(item); });
          return row;
        }));
        if (!shown.length) list.append(element("li", "palette-empty", "No matches"));
        input.setAttribute("aria-activedescendant", shown.length ? `palette-option-${active}` : "");
        list.children[active]?.scrollIntoView?.({ block: "nearest" });
      };
      input.addEventListener("input", () => { active = 0; render(); });
      input.addEventListener("keydown", event => {
        if (event.key === "ArrowDown" || event.key === "ArrowUp") {
          event.preventDefault();
          if (shown.length) { active = (active + (event.key === "ArrowDown" ? 1 : -1) + shown.length) % shown.length; render(); }
        } else if (event.key === "Enter" && shown[active]) { event.preventDefault(); run(shown[active]); }
      });
      panel.append(input, list);
      render();
    }, "Command palette");
  }

  function openShortcuts() {
    open(panel => {
      panel.classList.add("shortcuts");
      const header = element("div", "palette-header");
      header.append(element("h2", "", "Keyboard shortcuts"));
      const done = element("button", "palette-close", "×");
      done.type = "button"; done.setAttribute("aria-label", "Close"); done.addEventListener("click", close);
      header.append(done);
      panel.append(header);
      for (const [title, rows] of SHORTCUTS) {
        const section = element("section", "shortcut-group");
        section.append(element("h3", "", title));
        const table = element("dl");
        for (const [keys, action] of rows) table.append(element("dt", "", keys), element("dd", "", action));
        section.append(table);
        panel.append(section);
      }
    }, "Keyboard shortcuts");
  }

  window.addEventListener("keydown", event => {
    if (dialog && event.key === "Escape") { event.preventDefault(); event.stopImmediatePropagation(); close(); return; }
    if ((event.ctrlKey || event.metaKey) && !event.shiftKey && !event.altKey && event.key.toLowerCase() === "k") {
      event.preventDefault(); event.stopImmediatePropagation(); openPalette(); return;
    }
    if (event.key === "F1" || (event.ctrlKey && event.key === "/")) { event.preventDefault(); event.stopImmediatePropagation(); openShortcuts(); }
  }, true);
  document.addEventListener("click", event => {
    const button = event.target.closest?.("[data-palette]");
    if (!button) return;
    document.querySelectorAll(".menubar details[open]").forEach(menu => { menu.open = false; });
    if (button.dataset.palette === "shortcuts") openShortcuts(); else openPalette();
  });

  window.formaPalette = { open: openPalette, shortcuts: openShortcuts, close, rank, entries };
})();
