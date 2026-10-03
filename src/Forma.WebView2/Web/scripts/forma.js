// How each control kind maps onto the DOM.
//
// `text` decides what the control's Text property means:
//   "content" - written to textContent
//   "value"   - written to the value property (form fields)
//   "none"    - ignored, so containers never overwrite their own children
const CONTROL_TYPES = {
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

function initContainer(el) {
  const header = document.createElement("div"); header.className = "layout-header";
  const content = document.createElement("div"); content.className = "layout-content";
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
    if (kind === "togglebutton") { element.setAttribute("aria-pressed", String(properties.checked ?? false)); element.classList.toggle("toggle-active", properties.checked ?? false); }
    if (["textbox", "maskedtextbox", "searchbox", "passwordbox", "textarea"].includes(kind) && "placeholder" in properties) element.placeholder = properties.placeholder ?? "";
    if ("layoutSlot" in properties) element.dataset.slot = properties.layoutSlot;
    const parent = element.parentElement?.closest("[data-forma-type]");
    if (parent?.dataset.formaType === "tabcontrol") element.hidden = Number(element.dataset.slot ?? 1) !== Number(parent.dataset.activeTab ?? 0) + 1;
    if (parent?.dataset.formaType === "splitcontainer") {
      element.style.gridColumn = parent.querySelector(".layout-content").style.gridTemplateColumns === "1fr" ? "1" : String(Math.min(2, properties.layoutSlot ?? 1));
      element.style.gridRow = parent.querySelector(".layout-content").style.gridTemplateColumns === "1fr" ? String(Math.min(2, properties.layoutSlot ?? 1)) : "1";
    }
    if (parent?.dataset.formaType === "tablelayoutpanel") {
      const columns = Number(parent.dataset.columns ?? 2), slot = Number(element.dataset.slot ?? 1) - 1;
      element.style.gridColumn = String(slot % columns + 1);
      element.style.gridRow = String(Math.floor(slot / columns) + 1);
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
    if (["splitcontainer", "flowlayoutpanel", "tablelayoutpanel", "tabcontrol", "groupbox"].includes(kind)) {
      const host = element.querySelector(".layout-content");
      host.style.gap = `${properties.gap ?? 8}px`;
      if (kind === "flowlayoutpanel") { host.style.display = "flex"; host.style.flexDirection = properties.orientation === "vertical" ? "column" : "row"; host.style.flexWrap = "wrap"; host.style.alignContent = "flex-start"; host.style.alignItems = "flex-start"; }
      if (kind === "splitcontainer") {
        host.style.display = "grid";
        host.style.gridTemplateColumns = properties.orientation === "vertical" ? "1fr" : "1fr 1fr";
        host.style.gridTemplateRows = properties.orientation === "vertical" ? "1fr 1fr" : "1fr";
        for (const child of host.children) {
          child.style.gridColumn = properties.orientation === "vertical" ? "1" : String(Math.min(2, Number(child.dataset.slot ?? 1)));
          child.style.gridRow = properties.orientation === "vertical" ? String(Math.min(2, Number(child.dataset.slot ?? 1))) : "1";
        }
      }
      if (kind === "tablelayoutpanel") {
        const columns = properties.columns ?? 2; element.dataset.columns = columns;
        host.style.display = "grid"; host.style.gridTemplateColumns = `repeat(${columns}, minmax(0, 1fr))`; host.style.gridAutoRows = "minmax(36px, auto)";
        for (const child of host.children) {
          const slot = Number(child.dataset.slot ?? 1) - 1;
          child.style.gridColumn = String(slot % columns + 1); child.style.gridRow = String(Math.floor(slot / columns) + 1);
        }
      }
      if (kind === "tabcontrol") {
        const bar = element.querySelector(".layout-header");
        bar.replaceChildren(...(properties.tabs ?? ["Tab 1"]).map((text, index) => {
          const button = document.createElement("button"); button.textContent = text; button.dataset.tabIndex = index;
          button.className = index === (properties.selectedTab ?? 0) ? "tab-active" : "";
          button.addEventListener("click", () => window.forma.send({ type: "event", id: element.id, event: "tab", payload: { selectedTab: index } }));
          return button;
        }));
        element.dataset.activeTab = properties.selectedTab ?? 0;
        for (const child of host.children) child.hidden = Number(child.dataset.slot ?? 1) !== Number(element.dataset.activeTab) + 1;
      }
    }
    if (kind === "datagridview") {
      const table = document.createElement("table"), head = document.createElement("thead"), body = document.createElement("tbody");
      const heading = document.createElement("tr");
      for (const text of properties.columns ?? []) { const cell = document.createElement("th"); cell.textContent = text; heading.appendChild(cell); }
      head.appendChild(heading);
      (properties.rows ?? []).forEach((row, r) => {
        const tr = document.createElement("tr");
        (properties.columns ?? []).forEach((_, c) => {
          const cell = document.createElement("td"); cell.textContent = row[c] ?? "";
          cell.contentEditable = properties.readOnly ? "false" : "true";
          cell.addEventListener("blur", () => window.forma.send({ type: "event", id: element.id,
            event: "cell", payload: { row: r, column: c, value: cell.textContent } }));
          tr.appendChild(cell);
        }); body.appendChild(tr);
      });
      table.append(head, body); element.replaceChildren(table);
    }
  },

  applyPosition(element, properties) {
    if (!("x" in properties) && !("y" in properties)) return;
    const positioned = Number.isFinite(properties.x) || Number.isFinite(properties.y);
    element.style.position = positioned ? "absolute" : "";
    element.style.left = Number.isFinite(properties.x) ? `${properties.x}px` : "";
    element.style.top = Number.isFinite(properties.y) ? `${properties.y}px` : "";
    const parent = element.parentElement?.closest("[data-forma-type]");
    if (["flowlayoutpanel", "tablelayoutpanel", "splitcontainer"].includes(parent?.dataset.formaType))
      Object.assign(element.style, { position: "relative", left: "", top: "" });
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

    if ("text" in properties) {
      this.applyText(element, spec, properties.text);
    }

    (parent.querySelector?.(":scope > .layout-content") ?? parent).appendChild(element);
    this.applyPosition(element, properties);
    this.applyData(element, properties);

    if (message.control === "linklabel") element.addEventListener("click", event => {
      event.preventDefault();
      if (element.getAttribute("aria-disabled") === "true") return;
      window.forma.send({ type: "event", id: element.id, event: "link", payload: {} });
    });
    if (message.control === "button") {
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

    if ("text" in properties) {
      this.applyText(element, spec, properties.text);
    }
    this.applyPosition(element, properties);
    this.applyData(element, properties);
  },

  remove(message) {
    const element = document.getElementById(message.id);

    // Detaching an element takes its descendants with it, which is why the
    // host only sends one remove per subtree.
    element?.remove();
  },
};

// The host delivers commands through PostWebMessageAsJson, which surfaces here
// as a message event carrying the already-parsed object.
window.chrome.webview.addEventListener("message", (event) => {
  window.forma.receive(event.data);
});

window.chrome.webview.addEventListener("load", (event) => {
  console.log("Loaded the form succesfully");
});
