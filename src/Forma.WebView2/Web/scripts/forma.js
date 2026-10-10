// How each control kind maps onto the DOM.
//
// `text` decides what the control's Text property means:
//   "content" - written to textContent
//   "value"   - written to the value property (form fields)
//   "none"    - ignored, so containers never overwrite their own children
const CONTROL_TYPES = {
  stackpanel: { tag: "div", text: "none", init: initContainer },
  hstack: { tag: "div", text: "none", init: initContainer },
  vstack: { tag: "div", text: "none", init: initContainer },
  wrappanel: { tag: "div", text: "none", init: initContainer },
  centerpanel: { tag: "div", text: "none", init: initContainer },
  scrollablepanel: { tag: "div", text: "none", init: initContainer },
  breadcrumb: { tag: "nav", text: "none" },
  sidenavigation: { tag: "nav", text: "none" },
  dropdownbutton: { tag: "div", text: "none" },
  splitbutton: { tag: "div", text: "none" },
  commandbutton: { tag: "button", text: "none" },
  chip: { tag: "div", text: "none" },
  chipgroup: { tag: "div", text: "none" },
  buttongroup: { tag: "div", text: "none" },
  iconbutton: { tag: "button", text: "none" },
  floatingactionbutton: { tag: "button", text: "none" },
  radiogroup: { tag: "div", text: "none" },
  checkboxgroup: { tag: "div", text: "none" },
  segmentedcontrol: { tag: "div", text: "none" },
  rating: { tag: "div", text: "none" },
  card: { tag: "section", text: "none", init: initContainer },
  icon: { tag: "span", text: "none" },
  emptystate: { tag: "section", text: "none" },
  skeleton: { tag: "div", text: "none" },
  badge: { tag: "span", text: "none" },
  avatar: { tag: "div", text: "none" },
  divider: { tag: "div", text: "none" },
  toast: { tag: "div", text: "component", init: el => { el.hidden = true; } },
  spinner: { tag: "div", text: "none" },
  loadingoverlay: { tag: "div", text: "component", init: el => { el.hidden = true; } },
  tooltip: { tag: "div", text: "component", init: el => { el.hidden = true; } },
  contextmenu: { tag: "div", text: "component", init: el => { el.hidden = true; } },
  contextmenustrip: { tag: "div", text: "component", init: el => { el.hidden = true; } },
  dialog: { tag: "div", text: "component", init: el => { el.hidden = true; } },
  confirmationdialog: { tag: "div", text: "component", init: el => { el.hidden = true; } },
  menustrip: { tag: "nav", text: "none" },
  toolbar: { tag: "div", text: "none" },
  toolstrip: { tag: "div", text: "none" },
  statusbar: { tag: "div", text: "none" },
  filepicker: { tag: "div", text: "none", init: initPathPicker },
  folderpicker: { tag: "div", text: "none", init: initPathPicker },
  propertygrid: { tag: "div", text: "none" },
  listview: { tag: "select", text: "none", init: el => { el.size = 6; } },
  treeview: { tag: "div", text: "none" },
  pagination: { tag: "div", text: "none" },
  button: { tag: "button", text: "content" },
  linklabel: { tag: "a", text: "content" },
  maskedtextbox: { tag: "input", text: "value", init: el => { el.type = "text"; } },
  checkedlistbox: { tag: "div", text: "none" },
  richtextbox: { tag: "div", text: "none", init: initRichText },
  picturebox: { tag: "img", text: "alt" },
  label: { tag: "span", text: "content" },
  textbox: {
    tag: "input",
    text: "value",
    init: (element) => {
      element.type = "text";
    },
  },
  searchbox: { tag: "input", text: "value", init: el => { el.type = "search"; } },
  passwordbox: { tag: "input", text: "value", init: el => { el.type = "password"; } },
  textarea: { tag: "textarea", text: "value" },
  numericupdown: { tag: "input", text: "none", init: el => { el.type = "number"; } },
  slider: { tag: "input", text: "none", init: el => { el.type = "range"; } },
  progressbar: { tag: "progress", text: "none" },
  circularprogress: { tag: "div", text: "none" },
  datepicker: { tag: "input", text: "none", init: el => { el.type = "date"; } },
  timepicker: { tag: "input", text: "none", init: el => { el.type = "time"; } },
  datetimepicker: { tag: "input", text: "none", init: el => { el.type = "datetime-local"; } },
  colorpicker: { tag: "input", text: "none", init: el => { el.type = "color"; } },
  toggleswitch: { tag: "label", text: "caption", init: el => initCheck(el, "checkbox") },
  togglebutton: { tag: "button", text: "content" },
  panel: { tag: "div", text: "none" },
  groupbox: { tag: "div", text: "header", init: initContainer },
  splitcontainer: { tag: "div", text: "none", init: initContainer },
  tabcontrol: { tag: "div", text: "none", init: initContainer },
  flowlayoutpanel: { tag: "div", text: "none", init: initContainer },
  tablelayoutpanel: { tag: "div", text: "none", init: initContainer },
  checkbox: { tag: "label", text: "caption", init: el => initCheck(el, "checkbox") },
  radiobutton: { tag: "label", text: "caption", init: el => initCheck(el, "radio") },
  combobox: { tag: "select", text: "none" },
  listbox: { tag: "select", text: "none", init: el => { el.size = 5; } },
  image: { tag: "img", text: "alt" },
  datagridview: { tag: "div", text: "none" },
  timer: { tag: "div", text: "component", init: el => { el.hidden = true; } },
  backgroundworker: { tag: "div", text: "component", init: el => { el.hidden = true; } },
  form: {
    tag: "div",
    text: "none",
    init: (element) => element.classList.add("forma-form"),
  },
};

let contextPopup = null;
function closeContextPopup() {
  contextPopup?.remove();
  contextPopup = null;
}

function applyDialog(source, properties) {
  const id = `dialog-popup-${source.id}`;
  let modal = document.getElementById(id);
  if (!properties.isOpen || window.formaDesigner?.preview === false) { modal?.remove(); return; }
  if (!modal) {
    modal = document.createElement("dialog"); modal.id = id; modal.dataset.dialogSource = source.id;
    modal.dataset.componentSource = source.id;
    modal.className = "forma-dialog-popup";
    modal.setAttribute("aria-labelledby", `${id}-title`);
    modal.setAttribute("aria-describedby", `${id}-message`);
    const title = document.createElement("h2"), message = document.createElement("p"), footer = document.createElement("div");
    title.id = `${id}-title`; message.id = `${id}-message`; footer.className = "dialog-buttons";
    modal.append(title, message, footer); document.body.appendChild(modal);
    modal.addEventListener("cancel", event => {
      event.preventDefault();
      if (modal.dataset.canCancel === "true") {
        window.forma.send({ type: "event", id: source.id, event: "dialog-result", payload: { result: "Cancel" } });
        if (modal.close) modal.close(); else modal.removeAttribute("open");
      }
    });
  }
  modal.querySelector("h2").textContent = properties.dialogTitle ?? "Message";
  modal.querySelector("p").textContent = properties.message ?? "";
  modal.dataset.canCancel = String(properties.canCancel !== false);
  if (modal.dataset.buttons !== properties.buttons) {
    const labels = { OK: ["OK"], OKCancel: ["OK", "Cancel"], YesNo: ["Yes", "No"], YesNoCancel: ["Yes", "No", "Cancel"] }[properties.buttons] ?? ["OK"];
    modal.querySelector(".dialog-buttons").replaceChildren(...labels.map(label => {
      const button = document.createElement("button"); button.type = "button"; button.textContent = label;
      button.addEventListener("click", () => {
        window.forma.send({ type: "event", id: source.id, event: "dialog-result", payload: { result: label } });
        if (modal.close) modal.close(); else modal.removeAttribute("open");
      }); return button;
    }));
    modal.dataset.buttons = properties.buttons;
  }
  if (!modal.open) {
    if (typeof modal.showModal === "function") modal.showModal();
    else modal.setAttribute("open", "");
  }
}

function initPathPicker(el) {
  const input = document.createElement("input"), button = document.createElement("button");
  input.type = "text"; input.readOnly = true; input.placeholder = "No path selected";
  input.setAttribute("aria-label", "Selected path");
  button.type = "button"; button.textContent = "Browse…";
  button.addEventListener("click", () => {
    if (!button.disabled) window.forma.send({ type: "event", id: el.id, event: "browse", payload: {} });
  });
  el.append(input, button);
}

function layoutChildren(host) {
  return Array.from(host.children).flatMap(child => child.dataset.formaType ? [child]
    : child.dataset.layoutSlot ? Array.from(child.children).filter(node => node.dataset.formaType) : []);
}
function ensureLayoutCells(container, requiredSlot = 1) {
  const host = container.querySelector(":scope > .layout-content");
  const children = layoutChildren(host), split = container.dataset.formaType === "splitcontainer";
  const columns = split ? (container.dataset.orientation === "vertical" ? 1 : 2) : Number(container.dataset.columns ?? 2);
  const maxSlot = Math.max(requiredSlot, ...children.map(child => Number(child.dataset.slot ?? 1)));
  const rows = split ? (columns === 1 ? 2 : 1) : Math.max(Number(container.dataset.rowCount ?? 2), Math.ceil(maxSlot / columns));
  const count = split ? 2 : columns * rows;
  const cells = Array.from(host.children).filter(child => child.dataset.layoutSlot);
  if (cells.length !== count) {
    const next = Array.from({ length: count }, (_, index) => {
      const cell = document.createElement("div"); cell.dataset.layoutSlot = index + 1;
      cell.className = split ? "layout-pane" : "layout-cell";
      cell.setAttribute("aria-label", split ? `Pane ${index + 1}` : `Row ${Math.floor(index / columns) + 1}, column ${index % columns + 1}`);
      return cell;
    });
    host.replaceChildren(...next);
    children.forEach(child => next[Math.min(count - 1, Number(child.dataset.slot ?? 1) - 1)].appendChild(child));
  }
  host.style.display = "grid"; host.style.gridTemplateColumns = `repeat(${columns}, minmax(0, 1fr))`;
  host.style.gridTemplateRows = `repeat(${rows}, minmax(${split ? 0 : 36}px, 1fr))`;
  container.dataset.actualRows = rows;
  Array.from(host.children).forEach((cell, index) => cell.setAttribute("aria-label", split ? `Pane ${index + 1}` : `Row ${Math.floor(index / columns) + 1}, column ${index % columns + 1}`));
}
function childHost(parent, slot = 1) {
  const host = parent.querySelector?.(":scope > .layout-content") ?? parent;
  if (["splitcontainer", "tablelayoutpanel"].includes(parent.dataset?.formaType)) {
    ensureLayoutCells(parent, slot);
    const cells = Array.from(host.children).filter(child => child.dataset.layoutSlot);
    return cells[Math.max(0, Math.min(cells.length - 1, slot - 1))];
  }
  return host;
}
window.formaChildHost = childHost;

function initContainer(el) {
  const header = document.createElement("div"); header.className = "layout-header";
  const content = document.createElement("div"); content.className = "layout-content";
  if (["stackpanel", "hstack", "vstack", "wrappanel", "centerpanel", "scrollablepanel"].includes(el.dataset.formaType)) header.hidden = true;
  el.append(header, content);
}

function richFlags(node, editor) {
  const flags = {};
  for (let parent = node.parentElement; parent && parent !== editor; parent = parent.parentElement) {
    if (flags.bold == null && (parent.style.fontWeight || ["B", "STRONG"].includes(parent.tagName))) flags.bold = parent.style.fontWeight ? parent.style.fontWeight === "bold" : true;
    if (flags.italic == null && (parent.style.fontStyle || ["I", "EM"].includes(parent.tagName))) flags.italic = parent.style.fontStyle ? parent.style.fontStyle === "italic" : true;
    if (flags.underline == null && (parent.style.textDecoration || parent.tagName === "U")) flags.underline = parent.style.textDecoration ? parent.style.textDecoration.includes("underline") : true;
  }
  return { bold: flags.bold ?? false, italic: flags.italic ?? false, underline: flags.underline ?? false };
}
function readRichDocument(editor) {
  const blocks = [];
  for (const child of editor.childNodes) {
    const runs = [], walk = (node) => {
      if (node.nodeType === 3) runs.push({ text: node.textContent, ...richFlags(node, editor) });
      else if (node.nodeName === "BR") runs.push({ text: "\n", bold: false, italic: false, underline: false });
      else for (const nested of node.childNodes) walk(nested);
    };
    walk(child);
    blocks.push({ kind: child.dataset?.kind ?? "paragraph", runs });
  }
  return blocks.length ? blocks : [{ kind: "paragraph", runs: [] }];
}
function renderRichBlocks(editor, blocks) {
  editor.replaceChildren(...blocks.map(block => {
    const p = document.createElement("p"); p.dataset.kind = block.kind;
    for (const run of block.runs ?? []) {
      const span = document.createElement("span"); span.textContent = run.text;
      span.style.fontWeight = run.bold ? "bold" : "normal"; span.style.fontStyle = run.italic ? "italic" : "normal"; span.style.textDecoration = run.underline ? "underline" : "none"; p.appendChild(span);
    }
    if (!p.childNodes.length) p.appendChild(document.createElement("br")); return p;
  }));
}
function publishRichText(el, normalize = false) {
  const editor = el.querySelector(".rich-content"), blocks = readRichDocument(editor);
  if (normalize) {
    const selection = window.getSelection(), range = selection?.rangeCount ? selection.getRangeAt(0) : null;
    function offset(container, count) { const prefix = document.createRange(); prefix.selectNodeContents(editor); prefix.setEnd(container, count); return prefix.toString().length; }
    const positions = range && editor.contains(range.startContainer) && editor.contains(range.endContainer) ? [offset(range.startContainer, range.startOffset), offset(range.endContainer, range.endOffset)] : null;
    renderRichBlocks(editor, blocks);
    if (positions) {
      function locate(position) { const walker = document.createTreeWalker(editor, 4); let last = null;
        while (walker.nextNode()) { const node = walker.currentNode; last = node; if (position <= node.length) return [node, position]; position -= node.length; }
        return last ? [last, last.length] : [editor, 0];
      }
      const next = document.createRange(); next.setStart(...locate(positions[0])); next.setEnd(...locate(positions[1])); selection.removeAllRanges(); selection.addRange(next);
    }
  }
  el.dataset.richDocument = JSON.stringify(blocks);
  const design = window.formaDesigner;
  if (design && !design.preview) window.forma.send({ type: "designer", id: el.id, event: "property", payload: { property: "document", value: JSON.stringify(blocks) } });
  else window.forma.send({ type: "event", id: el.id, event: "rich-input", payload: { document: blocks } });
}
function formatRichSelection(editor, key) {
  const selection = window.getSelection(); if (!selection?.rangeCount) return false;
  const range = selection.getRangeAt(0);
  if (range.collapsed || !editor.contains(range.startContainer) || !editor.contains(range.endContainer)) return false;
  if (["paragraph", "bullet", "number"].includes(key)) {
    for (const block of editor.children) if (range.intersectsNode(block)) block.dataset.kind = key;
    return true;
  }
  const walker = document.createTreeWalker(editor, 4), targets = [];
  while (walker.nextNode()) {
    const node = walker.currentNode;
    if (!range.intersectsNode(node)) continue;
    const start = node === range.startContainer ? range.startOffset : 0, end = node === range.endContainer ? range.endOffset : node.length;
    if (end > start) targets.push({ node, start, end, flags: richFlags(node, editor) });
  }
  if (!targets.length) return false;
  const enable = !targets.every(t => t.flags[key]); const spans = [];
  for (const target of targets) {
    const { node, start, end } = target;
    if (end < node.length) node.splitText(end);
    const selected = start ? node.splitText(start) : node;
    const span = document.createElement("span");
    const flags = { ...target.flags, [key]: enable };
    // Explicit normal values override ancestor formatting when toggling off.
    span.style.fontWeight = flags.bold ? "bold" : "normal";
    span.style.fontStyle = flags.italic ? "italic" : "normal";
    span.style.textDecoration = flags.underline ? "underline" : "none";
    selected.replaceWith(span); span.appendChild(selected); spans.push(span);
  }
  const next = document.createRange(); next.setStart(spans[0].firstChild, 0); next.setEnd(spans.at(-1).firstChild, spans.at(-1).textContent.length);
  selection.removeAllRanges(); selection.addRange(next); return true;
}
function initRichText(el) {
  const toolbar = document.createElement("div"); toolbar.className = "rich-toolbar";
  const editor = document.createElement("div"); editor.className = "rich-content"; editor.contentEditable = "false"; editor.setAttribute("role", "textbox"); editor.setAttribute("aria-multiline", "true");
  for (const [key, text] of [["bold", "B"], ["italic", "I"], ["underline", "U"], ["paragraph", "Paragraph"], ["bullet", "Bullets"], ["number", "Numbers"]]) {
    const button = document.createElement("button"); button.type = "button"; button.textContent = text; button.title = key; button.dataset.richFormat = key;
    button.addEventListener("pointerdown", event => event.preventDefault());
    button.addEventListener("click", () => { if (editor.contentEditable === "true" && formatRichSelection(editor, key)) publishRichText(el, true); }); toolbar.appendChild(button);
  }
  editor.addEventListener("input", () => { if (editor.contentEditable === "true") publishRichText(el); });
  editor.addEventListener("drop", event => event.preventDefault());
  editor.addEventListener("paste", event => {
    if (editor.contentEditable !== "true") return;
    event.preventDefault(); const selection = window.getSelection(); if (!selection?.rangeCount) return;
    const range = selection.getRangeAt(0); if (!editor.contains(range.commonAncestorContainer)) return;
    range.deleteContents(); const text = document.createTextNode(event.clipboardData.getData("text/plain")); range.insertNode(text); range.setStartAfter(text); range.collapse(true); selection.removeAllRanges(); selection.addRange(range); publishRichText(el);
  });
  el.append(toolbar, editor);
}
function initCheck(el, kind) {
  const input = document.createElement("input"); input.type = kind;
  const caption = document.createElement("span"); caption.className = "control-caption";
  el.append(input, caption);
  input.addEventListener("change", () => window.forma.send({ type: "event", id: el.id,
    event: "checked", payload: { checked: input.checked } }));
}

function formatMaskedText(text, mask) {
  if (!mask) return text;
  let index = 0, result = "";
  for (const token of mask) {
    if (!["0", "L", "A", "*"].includes(token)) { result += token; if (text[index] === token) index++; continue; }
    let next = "_";
    while (index < text.length) {
      const candidate = text[index++]; if (candidate === "_") break;
      const valid = token === "0" ? /^[0-9]$/.test(candidate) : token === "L" ? /^[a-zA-Z]$/.test(candidate) : token === "A" ? /^[a-zA-Z0-9]$/.test(candidate) : candidate !== "_";
      if (valid) { next = candidate; break; }
    }
    result += next;
  }
  return result;
}

window.forma = {
  receive(message) {
    switch (message.type) {
      case "create":
        this.create(message);
        break;

      case "update":
        this.update(message);
        break;

      case "remove":
        this.remove(message);
        break;

      case "designer":
        window.formaDesigner?.receive(message);
        break;

      default:
        console.error("Unknown Forma command:", message.type);
    }
  },

  send(message) {
    // Posted as a structured object, not a string, so the host can read it
    // with WebMessageAsJson.
    window.chrome.webview.postMessage(message);
  },

  applyText(element, spec, value) {
    if (spec.text === "value") {
      // An input event echoes through C#. Avoid rewriting an unchanged value
      // so typing preserves the caret and selection.
      if (element.value !== (value ?? "")) {
        element.value = value ?? "";
      }
    } else if (spec.text === "content") {
      element.textContent = value ?? "";
    } else if (spec.text === "caption") element.querySelector(".control-caption").textContent = value ?? "";
    else if (spec.text === "header") element.querySelector(".layout-header").textContent = value ?? "";
    else if (spec.text === "alt") element.alt = value ?? "Image";
    else if (spec.text === "component") element.textContent = value ?? element.dataset.formaType;
  },

  applyData(element, properties) {
    window.formaSelections?.update(element, properties);
    if (["icon", "emptystate", "skeleton", "card", "badge", "avatar", "divider", "toast", "spinner", "loadingoverlay", "chip", "iconbutton", "floatingactionbutton", "commandbutton"].includes(element.dataset.formaType)) window.formaModern?.update(element, properties);
    if (element.dataset.formaType === "tooltip") window.formaTooltip?.update(element, properties);
    const kind = element.dataset.formaType;
    if (kind === "treeview") {
      const expanded = new Set(properties.expandedNodes ?? []);
      function branch(nodes) {
        const list = document.createElement("ul"); list.setAttribute("role", "group");
        for (const node of nodes) {
          const id = node.Id ?? node.id, text = node.Text ?? node.text, children = node.Children ?? node.children ?? [];
          const li = document.createElement("li"), row = document.createElement("div"); row.className = "tree-row";
          if (children.length) {
            const toggle = document.createElement("button"); toggle.textContent = expanded.has(id) ? '-' : '+'; toggle.setAttribute("aria-label", `Expand or collapse ${text}`); toggle.setAttribute("aria-expanded", String(expanded.has(id)));
            toggle.addEventListener("click", () => window.forma.send({ type: "event", id: element.id, event: "tree-expand", payload: { node: id, expanded: !expanded.has(id) } })); row.appendChild(toggle);
          }
          const select = document.createElement("button"); select.textContent = text; select.className = properties.selectedNode === id ? "tree-selected" : ""; select.setAttribute("aria-pressed", String(properties.selectedNode === id));
          select.addEventListener("click", () => window.forma.send({ type: "event", id: element.id, event: "tree-select", payload: { node: id } }));
          row.appendChild(select); li.appendChild(row); if (children.length && expanded.has(id)) li.appendChild(branch(children)); list.appendChild(li);
        }
        return list;
      }
      element.replaceChildren(branch(properties.nodes ?? []));
    }
    if (kind === "pagination") {
      const page = properties.page ?? 1, count = properties.pageCount ?? 1;
      const previous = document.createElement("button"), next = document.createElement("button"), label = document.createElement("span");
      previous.textContent = 'Previous'; next.textContent = 'Next'; label.textContent = `${page} / ${count}`;
      previous.dataset.boundary = String(page <= 1); next.dataset.boundary = String(page >= count);
      previous.disabled = page <= 1; next.disabled = page >= count;
      for (const [button, target] of [[previous, page - 1], [next, page + 1]]) button.addEventListener("click", () => window.forma.send({ type: "event", id: element.id, event: "page", payload: { page: target } }));
      element.replaceChildren(previous, label, next);
    }
    if (kind === "richtextbox") {
      const blocks = properties.document ?? [], signature = JSON.stringify(blocks), editor = element.querySelector(".rich-content");
      if (element.dataset.richDocument !== signature) {
        renderRichBlocks(editor, blocks);
        element.dataset.richDocument = signature;
      }
      editor.contentEditable = properties.readOnly ? "false" : "true";
    }
    if (kind === "linklabel") { element.href = properties.url ?? "https://example.com"; element.classList.toggle("link-visited", properties.visited ?? false); }
    if (kind === "maskedtextbox") {
      element.dataset.mask = properties.mask ?? "000-0000";
      if (element.value) element.value = formatMaskedText(element.value, element.dataset.mask);
    }
    if (kind === "checkedlistbox") {
      const items = properties.items ?? [], checked = new Set(properties.checkedIndices ?? []);
      // Reuse rows on check changes so keyboard focus stays on the current item.
      if (element.dataset.items !== JSON.stringify(items)) {
        element.replaceChildren(...items.map((text, index) => {
          const label = document.createElement("label"), input = document.createElement("input"), caption = document.createElement("span");
          input.type = "checkbox"; input.dataset.itemIndex = index; caption.textContent = text;
          input.addEventListener("change", () => window.forma.send({ type: "event", id: element.id, event: "item-check", payload: { index, checked: input.checked } }));
          label.append(input, caption); return label;
        }));
        element.dataset.items = JSON.stringify(items);
      }
      element.querySelectorAll("input").forEach(input => { input.checked = checked.has(Number(input.dataset.itemIndex)); });
    }
    if (["numericupdown", "slider", "progressbar", "circularprogress"].includes(kind)) {
      if (kind === "numericupdown" || kind === "slider") {
        element.min = properties.minimum ?? 0; element.max = properties.maximum ?? 100; element.step = properties.increment ?? 1;
        if (element.value !== String(properties.number ?? 0)) element.value = properties.number ?? 0;
      } else {
        const minimum = properties.minimum ?? 0, maximum = properties.maximum ?? 100;
        const ratio = maximum > minimum ? Math.max(0, Math.min(1, ((properties.number ?? 0) - minimum) / (maximum - minimum))) : 0;
        element.setAttribute("role", "progressbar"); element.setAttribute("aria-valuemin", minimum); element.setAttribute("aria-valuemax", maximum); element.setAttribute("aria-valuenow", properties.number ?? 0);
        if (kind === "progressbar") { element.max = Math.max(1, maximum - minimum); element.value = Math.max(0, (properties.number ?? 0) - minimum); }
        else { element.style.setProperty("--progress", `${ratio * 360}deg`); element.textContent = `${Math.round(ratio * 100)}%`; }
      }
    }
    if (["datepicker", "timepicker", "datetimepicker"].includes(kind)) element.value = properties.dateValue ?? "";
    if (kind === "colorpicker") element.value = properties.color ?? "#2878ff";
    if (["dialog", "confirmationdialog"].includes(kind)) applyDialog(element, properties);
    if (["contextmenu", "contextmenustrip"].includes(kind)) element.dataset.targetId = properties.targetId ?? "";
    if (["menustrip", "toolbar", "toolstrip", "contextmenu", "contextmenustrip", "dropdownbutton", "splitbutton"].includes(kind)) {
      const p = element._commandProperties = { ...element._commandProperties, ...properties };
      const items = p.commandItems ?? [], actionMenu = ["dropdownbutton", "splitbutton"].includes(kind);
      element.setAttribute("aria-label", actionMenu ? p.text ?? "Actions" : kind === "menustrip" ? "Menu" : "Toolbar");
      if (kind !== "menustrip") element.setAttribute("role", actionMenu ? "group" : "toolbar");
      element.dataset.orientation = p.orientation ?? "horizontal";
      const signature = JSON.stringify([items, actionMenu ? p.text : null, actionMenu ? p.primaryEnabled : null]);
      if (element.dataset.commands !== signature) {
        const activeId = document.activeElement?.dataset.commandItem;
        const get = (item, key) => item[key] ?? item[key[0].toUpperCase() + key.slice(1)];
        const render = (entries, parentEnabled = true) => entries.map(item => {
          if (get(item, "separator")) { const separator = document.createElement("hr"); separator.setAttribute("role", "separator"); return separator; }
          const children = get(item, "items") ?? [], enabled = parentEnabled && get(item, "enabled") !== false;
          if (children.length) {
            const details = document.createElement("details"), summary = document.createElement("summary"), popup = document.createElement("div");
            summary.textContent = get(item, "text"); summary.setAttribute("aria-disabled", String(!enabled));
            summary.dataset.itemDisabled = String(!enabled);
            details.className = "command-menu"; popup.className = "command-popup";
            details.addEventListener("toggle", () => {
              if (element.querySelector("details[open]")) {
                element.dataset.menuZIndex ??= element.style.zIndex;
                element.style.zIndex = "10000";
              } else if (element.dataset.menuZIndex != null) {
                element.style.zIndex = element.dataset.menuZIndex;
                delete element.dataset.menuZIndex;
              }
            });
            summary.addEventListener("click", event => { if (!enabled || element.dataset.disabled === "true") event.preventDefault(); });
            details.addEventListener("keydown", event => { if (event.key === "Escape") { details.open = false; summary.focus(); event.stopPropagation(); } });
            popup.append(...render(children, enabled)); details.append(summary, popup); return details;
          }
          const button = document.createElement("button"); button.type = "button";
          if (actionMenu) button.setAttribute("role", get(item, "checkOnClick") ? "menuitemcheckbox" : "menuitem");
          button.dataset.commandItem = get(item, "id"); button.dataset.itemDisabled = String(!enabled);
          button.disabled = !enabled || element.dataset.disabled === "true";
          button.textContent = `${get(item, "checked") ? "✓ " : ""}${get(item, "text")}`;
          if (get(item, "checkOnClick")) button.setAttribute("aria-pressed", String(get(item, "checked") ?? false));
          if (actionMenu && get(item, "checkOnClick")) button.setAttribute("aria-checked", String(get(item, "checked") ?? false));
          button.addEventListener("click", () => {
            if (button.disabled || (window.formaDesigner && !window.formaDesigner.preview)) return;
            element.querySelectorAll("details").forEach(menu => { menu.open = false; });
            closeContextPopup();
            window.forma.send({ type: "event", id: element.dataset.commandSource ?? element.id, event: "command-item", payload: { itemId: button.dataset.commandItem } });
          });
          return button;
        });
        if (actionMenu) {
          const menu = document.createElement("details"), trigger = document.createElement("summary"), popup = document.createElement("div");
          menu.className = "command-menu action-menu"; popup.className = "command-popup"; popup.id = `${element.id}-menu`;
          popup.setAttribute("role", "menu");
          trigger.textContent = kind === "splitbutton" ? "▾" : `${p.text ?? "Actions"} ▾`;
          trigger.setAttribute("aria-label", kind === "splitbutton" ? `More ${p.text ?? "actions"}` : p.text ?? "Actions");
          trigger.setAttribute("aria-haspopup", "menu"); trigger.setAttribute("aria-controls", popup.id);
          trigger.setAttribute("aria-disabled", String(element.dataset.disabled === "true"));
          trigger.addEventListener("click", event => {
            if (element.dataset.disabled === "true" || window.formaDesigner?.preview === false) event.preventDefault();
          });
          trigger.addEventListener("keydown", event => {
            if (event.key === "ArrowDown" && element.dataset.disabled !== "true" && window.formaDesigner?.preview !== false) {
              event.preventDefault(); menu.open = true; popup.querySelector('button:not(:disabled),summary:not([aria-disabled="true"])')?.focus();
            }
          });
          menu.addEventListener("toggle", () => {
            trigger.setAttribute("aria-expanded", String(menu.open));
            if (menu.open) { element.dataset.menuZIndex ??= element.style.zIndex; element.style.zIndex = "10000"; }
            else if (element.dataset.menuZIndex != null) { element.style.zIndex = element.dataset.menuZIndex; delete element.dataset.menuZIndex; }
          });
          trigger.setAttribute("aria-expanded", "false");
          menu.addEventListener("keydown", event => { if (event.key === "Escape") { menu.open = false; trigger.focus(); event.stopPropagation(); } });
          popup.addEventListener("keydown", event => {
            if (!["ArrowDown", "ArrowUp", "Home", "End"].includes(event.key)) return;
            const entries = [...popup.querySelectorAll(':scope > button:not(:disabled), :scope > .command-menu > summary:not([aria-disabled="true"])')];
            const index = entries.indexOf(event.target); if (index < 0 || !entries.length) return;
            event.preventDefault();
            const target = event.key === "Home" ? 0 : event.key === "End" ? entries.length - 1 : (index + (event.key === "ArrowDown" ? 1 : -1) + entries.length) % entries.length;
            entries[target].focus();
          });
          popup.append(...render(items)); menu.append(trigger, popup);
          if (kind === "splitbutton") {
            const primary = document.createElement("button"); primary.type = "button"; primary.className = "split-primary";
            primary.dataset.itemDisabled = String(p.primaryEnabled === false);
            primary.disabled = p.primaryEnabled === false || element.dataset.disabled === "true";
            primary.textContent = p.text ?? "Run";
            primary.addEventListener("click", () => {
              if (primary.disabled || window.formaDesigner?.preview === false) return;
              menu.open = false;
              window.forma.send({ type: "event", id: element.id, event: "primary-click", payload: {} });
              element.dispatchEvent(new CustomEvent("primary-click"));
            });
            element.replaceChildren(primary, menu);
          } else element.replaceChildren(menu);
        } else element.replaceChildren(...render(items));
        element.dataset.commands = signature;
        element.dataset.commandItems = JSON.stringify(items);
        if (activeId) Array.from(element.querySelectorAll("button")).find(button => button.dataset.commandItem === activeId)?.focus();
      }
    }
    if (kind === "statusbar") {
      if (!element.querySelector("span")) {
        const left = document.createElement("span"), right = document.createElement("span");
        left.className = "status-primary"; right.className = "status-secondary"; element.append(left, right);
        element.setAttribute("role", "status"); element.setAttribute("aria-live", "polite");
      }
      element.querySelector(".status-primary").textContent = properties.text ?? "";
      element.querySelector(".status-secondary").textContent = properties.rightText ?? "";
    }
    if (["filepicker", "folderpicker"].includes(kind)) {
      const input = element.querySelector("input"), button = element.querySelector("button");
      input.value = properties.selectedPath ?? "";
      input.title = input.value;
      input.setAttribute("aria-label", properties.dialogTitle || "Selected path");
      button.textContent = properties.text || "Browse…";
    }
    if (kind === "propertygrid") {
      const entries = properties.entries ?? [];
      const field = (entry, key) => entry[key] ?? entry[key[0].toUpperCase() + key.slice(1)];
      const schema = JSON.stringify(entries.map(entry => [field(entry, "name"), field(entry, "category"), field(entry, "readOnly")]));
      if (element.dataset.entrySchema !== schema) {
        const table = document.createElement("table"), body = document.createElement("tbody");
        table.setAttribute("aria-label", "Properties");
        let category;
        entries.forEach((entry, index) => {
          const nextCategory = field(entry, "category") || "General";
          if (category !== nextCategory) {
            const row = document.createElement("tr"), heading = document.createElement("th");
            heading.colSpan = 2; heading.textContent = nextCategory; heading.className = "property-category";
            row.appendChild(heading); body.appendChild(row); category = nextCategory;
          }
          const row = document.createElement("tr"), name = document.createElement("th"), value = document.createElement("td"), input = document.createElement("input");
          name.scope = "row"; name.textContent = field(entry, "name");
          input.type = "text"; input.maxLength = 32767; input.dataset.entryIndex = index;
          input.setAttribute("aria-label", name.textContent);
          input.addEventListener("change", () => {
            if (!input.disabled && !input.readOnly) window.forma.send({ type: "event", id: element.id, event: "property-value", payload: { index, value: input.value } });
          });
          value.appendChild(input); row.append(name, value); body.appendChild(row);
        });
        table.appendChild(body); element.replaceChildren(table); element.dataset.entrySchema = schema;
      }
      element.querySelectorAll("input").forEach((input, index) => {
        input.value = field(entries[index], "value") ?? "";
        input.dataset.entryReadOnly = String(field(entries[index], "readOnly") ?? false);
        input.readOnly = properties.readOnly || input.dataset.entryReadOnly === "true";
      });
    }
    if (kind === "togglebutton") { element.setAttribute("aria-pressed", String(properties.checked ?? false)); element.classList.toggle("toggle-active", properties.checked ?? false); }
    if (["textbox", "maskedtextbox", "searchbox", "passwordbox", "textarea"].includes(kind) && "placeholder" in properties) element.placeholder = properties.placeholder ?? "";
    if ("layoutSlot" in properties) element.dataset.slot = properties.layoutSlot;
    const parent = element.parentElement?.closest("[data-forma-type]");
    if (parent?.dataset.formaType === "tabcontrol") element.hidden = Number(element.dataset.slot ?? 1) !== Number(parent.dataset.activeTab ?? 0) + 1;
    if (["splitcontainer", "tablelayoutpanel"].includes(parent?.dataset.formaType)) {
      const host = childHost(parent, Number(element.dataset.slot ?? 1));
      if (host !== element.parentElement) host.appendChild(element);
      element.style.gridColumn = ""; element.style.gridRow = "";
    }
    if (["checkbox", "radiobutton", "toggleswitch"].includes(kind)) {
      const input = element.querySelector("input"); input.checked = properties.checked ?? false;
      if (kind === "radiobutton") input.name = `forma-radio-${parent?.id}`;
    }
    if (["combobox", "listbox", "listview"].includes(kind)) {
      if (properties.items) element.replaceChildren(...properties.items.map(text => {
        const option = document.createElement("option"); option.textContent = text; return option;
      }));
      if ("selectedIndex" in properties) element.selectedIndex = properties.selectedIndex;
    }
    if (kind === "image" || kind === "picturebox") {
      if (properties.source) element.src = properties.source; else element.removeAttribute("src");
      element.style.objectFit = properties.sizeMode ?? "contain";
    }
    if (["splitcontainer", "flowlayoutpanel", "tablelayoutpanel", "tabcontrol", "groupbox", "stackpanel", "hstack", "vstack", "wrappanel", "centerpanel", "scrollablepanel"].includes(kind)) {
      properties = element._layoutProperties = { ...element._layoutProperties, ...properties };
      const host = element.querySelector(".layout-content");
      element.dataset.orientation = properties.orientation ?? "horizontal";
      host.style.gap = `${properties.gap ?? 8}px`;
      if (["flowlayoutpanel", "stackpanel", "hstack", "vstack", "wrappanel", "centerpanel"].includes(kind)) {
        host.style.display = "flex"; host.style.flexDirection = properties.orientation === "vertical" ? "column" : "row";
        host.style.flexWrap = ["flowlayoutpanel", "wrappanel"].includes(kind) ? "wrap" : "nowrap";
        host.style.justifyContent = kind === "centerpanel" ? "center" : "flex-start";
        host.style.alignItems = kind === "centerpanel" ? "center" : "flex-start"; host.style.alignContent = "flex-start";
      }
      if (kind === "scrollablepanel") {
        host.style.overflowX = properties.scrollDirection === "vertical" ? "hidden" : "auto";
        host.style.overflowY = properties.scrollDirection === "horizontal" ? "hidden" : "auto";
      }
      if (kind === "splitcontainer" || kind === "tablelayoutpanel") {
        element.dataset.orientation = properties.orientation ?? "horizontal";
        element.dataset.columns = properties.columns ?? 2;
        element.dataset.rowCount = properties.rowCount ?? 2;
        ensureLayoutCells(element);
      }
      if (kind === "tabcontrol") {
        element.dataset.orientation = properties.orientation ?? "horizontal";
        const bar = element.querySelector(".layout-header");
        bar.setAttribute("role", "tablist"); bar.setAttribute("aria-orientation", element.dataset.orientation);
        bar.replaceChildren(...(properties.tabs ?? ["Tab 1"]).map((text, index) => {
          const button = document.createElement("button"); button.textContent = text; button.dataset.tabIndex = index;
          button.type = "button"; button.setAttribute("role", "tab");
          button.setAttribute("aria-selected", String(index === (properties.selectedTab ?? 0)));
          button.className = index === (properties.selectedTab ?? 0) ? "tab-active" : "";
          button.addEventListener("click", () => window.forma.send({ type: "event", id: element.id, event: "tab", payload: { selectedTab: index } }));
          return button;
        }));
        const addTab = document.createElement("button"); addTab.type = "button"; addTab.textContent = "+";
        addTab.dataset.tabAdd = "true"; addTab.setAttribute("aria-label", "Add tab");
        addTab.addEventListener("click", () => { if (window.formaDesigner?.preview === false) window.forma.send({ type: "designer", id: element.id, event: "command", payload: { command: "add-tab" } }); });
        bar.appendChild(addTab);
        element.dataset.activeTab = properties.selectedTab ?? 0;
        for (const child of host.children) child.hidden = Number(child.dataset.slot ?? 1) !== Number(element.dataset.activeTab) + 1;
      }
    }
    if (kind === "datagridview") {
      window.formaDataGrid?.render(element, properties);
    }
  },

  applyPosition(element, properties) {
    if (!("x" in properties) && !("y" in properties)) return;
    const positioned = Number.isFinite(properties.x) || Number.isFinite(properties.y);
    element.style.position = positioned ? "absolute" : "";
    element.style.left = Number.isFinite(properties.x) ? `${properties.x}px` : "";
    element.style.top = Number.isFinite(properties.y) ? `${properties.y}px` : "";
    const parent = element.parentElement?.closest("[data-forma-type]");
    if (["flowlayoutpanel", "tablelayoutpanel", "stackpanel", "hstack", "vstack", "wrappanel", "centerpanel"].includes(parent?.dataset.formaType))
      Object.assign(element.style, { position: "relative", left: "", top: "" });
    if (["flowlayoutpanel", "stackpanel", "hstack", "vstack", "wrappanel", "centerpanel"].includes(parent?.dataset.formaType)) element.style.flexShrink = "0";
  },

  create(message) {
    const spec = CONTROL_TYPES[message.control];

    if (!spec) {
      console.error("Unknown control:", message.control);

      return;
    }

    // A named parent that is missing means the tree arrived out of order.
    // Falling back to the body would silently misplace the control, so bail.
    const parent = message.parentId
      ? document.getElementById(message.parentId)
      : document.body;

    if (!parent) {
      console.error(
        `Cannot create '${message.id}': parent '${message.parentId}' not found.`,
      );

      return;
    }

    const element = document.createElement(spec.tag);

    element.id = message.id;

    // Recorded so update() knows how to apply text without being told the
    // control kind again.
    element.dataset.formaType = message.control;

    spec.init?.(element);

    const properties = message.properties ?? {};
    element._formaRevision = (element._formaRevision ?? 0) + 1;

    if ("text" in properties) {
      this.applyText(element, spec, properties.text);
    }

    const host = childHost(parent, properties.layoutSlot ?? 1);
    const siblings = Array.from(host.children).filter(child => child.dataset?.formaType);
    if (host.insertBefore) host.insertBefore(element, siblings[message.index] ?? null);
    else host.appendChild(element);
    this.applyPosition(element, properties);
    this.applyData(element, properties);

    if (message.control === "linklabel") element.addEventListener("click", event => {
      event.preventDefault();
      if (element.getAttribute("aria-disabled") === "true") return;
      window.forma.send({ type: "event", id: element.id, event: "link", payload: {} });
    });
    if (["button", "iconbutton", "floatingactionbutton", "commandbutton"].includes(message.control)) {
      element.addEventListener("click", () => {
        window.forma.send({
          type: "event",
          id: message.id,
          event: "click",
          payload: {},
        });
      });
    }

    if (["textbox", "maskedtextbox", "searchbox", "passwordbox", "textarea"].includes(message.control)) {
      element.addEventListener("input", () => {
        if (message.control === "maskedtextbox") {
          const raw = element.value, cursor = element.selectionStart ?? raw.length;
          const prefix = formatMaskedText(raw.slice(0, cursor), element.dataset.mask);
          const position = prefix.includes("_") ? prefix.indexOf("_") : prefix.length;
          element.value = formatMaskedText(raw, element.dataset.mask); element.setSelectionRange(position, position);
        }
        window.forma.send({
          type: "event",
          id: message.id,
          event: "input",
          payload: { text: element.value },
        });
      });
    }
    if (message.control === "togglebutton") element.addEventListener("click", () =>
      window.forma.send({ type: "event", id: element.id, event: "checked", payload: { checked: element.getAttribute("aria-pressed") !== "true" } }));
    if (["numericupdown", "slider", "datepicker", "timepicker", "datetimepicker", "colorpicker"].includes(message.control))
      element.addEventListener("input", () => {
        const numeric = ["numericupdown", "slider"].includes(message.control);
        const value = numeric ? element.valueAsNumber : element.value;
        if (numeric && !Number.isFinite(value)) return;
        window.forma.send({ type: "event", id: element.id, event: "value", payload: { value } });
      });
    if (["combobox", "listbox", "listview"].includes(message.control)) element.addEventListener("change", () =>
      window.forma.send({ type: "event", id: element.id, event: "selection", payload: { selectedIndex: element.selectedIndex } }));
  },

  update(message) {
    const element = document.getElementById(message.id);

    if (!element) return;

    const spec = CONTROL_TYPES[element.dataset.formaType];

    if (!spec) return;

    const properties = message.properties ?? {};
    element._formaRevision = (element._formaRevision ?? 0) + 1;

    if ("text" in properties) {
      this.applyText(element, spec, properties.text);
    }
    this.applyPosition(element, properties);
    this.applyData(element, properties);
  },

  remove(message) {
    const element = document.getElementById(message.id);
    window.formaTooltip?.removing(element);
    window.formaModern?.removing(element);
    document.querySelectorAll("[data-dialog-source]").forEach(modal => {
      const source = document.getElementById(modal.dataset.dialogSource);
      if (source === element || element?.contains(source)) modal.remove();
    });
    if (contextPopup) {
      const source = document.getElementById(contextPopup.dataset.commandSource);
      if (source === element || element?.contains(source)) closeContextPopup();
    }

    // Detaching an element takes its descendants with it, which is why the
    // host only sends one remove per subtree.
    element?.remove();
  },
};

window.forma.closeTransientUi = () => {
  window.formaModern?.clear();
  window.formaTooltip?.close();
  closeContextPopup();
  document.querySelectorAll("[data-dialog-source]").forEach(modal => modal.remove());
};
document.addEventListener("contextmenu", event => {
  if (window.formaDesigner?.preview === false) return;
  let target = event.target.closest?.("[data-forma-type]");
  const menus = Array.from(document.querySelectorAll('[data-forma-type="contextmenu"], [data-forma-type="contextmenustrip"]'));
  let menu;
  while (target && !menu) {
    menu = menus.find(source => source.dataset.targetId === target.id && source.dataset.disabled !== "true");
    target = target.parentElement?.closest?.("[data-forma-type]");
  }
  if (!menu) return;
  event.preventDefault(); closeContextPopup();
  contextPopup = document.createElement("div"); contextPopup.id = `context-popup-${menu.id}`;
  contextPopup.dataset.formaType = "contextmenu"; contextPopup.dataset.commandSource = menu.id;
  contextPopup.dataset.componentSource = menu.id;
  contextPopup.className = "forma-context-popup"; document.body.appendChild(contextPopup);
  window.forma.applyData(contextPopup, { commandItems: JSON.parse(menu.dataset.commandItems ?? "[]") });
  const rect = contextPopup.getBoundingClientRect();
  contextPopup.style.left = `${Math.max(0, Math.min(event.clientX, window.innerWidth - rect.width))}px`;
  contextPopup.style.top = `${Math.max(0, Math.min(event.clientY, window.innerHeight - rect.height))}px`;
  contextPopup.querySelector("button,summary")?.focus();
});
document.addEventListener("pointerdown", event => {
  document.querySelectorAll('[data-forma-type="dropdownbutton"], [data-forma-type="splitbutton"]').forEach(element => {
    if (!element.contains(event.target)) element.querySelectorAll("details[open]").forEach(menu => { menu.open = false; });
  });
  if (contextPopup && !contextPopup.contains(event.target)) closeContextPopup();
});
document.addEventListener("keydown", event => { if (event.key === "Escape") closeContextPopup(); });

// The host delivers commands through PostWebMessageAsJson, which surfaces here
// as a message event carrying the already-parsed object.
window.chrome.webview.addEventListener("message", (event) => {
  window.forma.receive(event.data);
});

window.chrome.webview.addEventListener("load", (event) => {
  console.log("Loaded the form succesfully");
});
