// The web workspace presents the design; C# owns controls and committed properties.
const supportedKinds = new Set([
  "icon", "emptystate", "skeleton",
  "tooltip", "card", "badge", "avatar", "divider", "toast", "spinner", "loadingoverlay",
  "contextmenu",
  "contextmenustrip",
  "dialog",
  "confirmationdialog",
  "menustrip",
  "toolbar",
  "toolstrip",
  "statusbar",
  "filepicker",
  "folderpicker",
  "propertygrid",
  "listview",
  "treeview",
  "pagination",
  "richtextbox",
  "picturebox",
  "linklabel",
  "maskedtextbox",
  "checkedlistbox",
  "numericupdown",
  "slider",
  "progressbar",
  "circularprogress",
  "toggleswitch",
  "togglebutton",
  "datepicker",
  "timepicker",
  "datetimepicker",
  "colorpicker",
  "searchbox",
  "passwordbox",
  "textarea",

  "button",
  "label",
  "textbox",
  "panel",
  "groupbox",
  "splitcontainer",
  "tabcontrol",
  "flowlayoutpanel",
  "tablelayoutpanel",
  "checkbox",
  "radiobutton",
  "combobox",
  "listbox",
  "image",
  "timer",
  "backgroundworker",
  "datagridview",
]);
const containerKinds = new Set([
  "card",
  "form",
  "panel",
  "groupbox",
  "splitcontainer",
  "tabcontrol",
  "flowlayoutpanel",
  "tablelayoutpanel",
]);
const managedKinds = new Set([
  "flowlayoutpanel",
  "tablelayoutpanel",
]);
const byId = (id) => document.getElementById(id);
const send = (event, id, payload = {}) =>
  window.forma.send({ type: "designer", id, event, payload });
const gridDrafts = new Map();
const gridFields = new Set(["gridColumns", "gridRows"]);
function gridValue(property, value) {
  if (property === "gridColumns") return value.split("\n").map(line => line.replace(/\r$/, "")).filter(Boolean).join("\n");
  const rows = JSON.parse(value);
  if (!Array.isArray(rows) || rows.some(row => !Array.isArray(row) || row.some(cell => typeof cell !== "string")))
    throw new Error('Use an array of string rows, for example [["Alice", "Engineering"]].');
  return JSON.stringify(rows);
}
function gridError(input, message) {
  input.setCustomValidity(message); input.setAttribute("aria-invalid", String(!!message));
  let hint = byId(`${input.id}-error`);
  if (!hint) {
    hint = document.createElement("small"); hint.id = `${input.id}-error`; hint.className = "property-error";
    input.parentElement.appendChild(hint); input.setAttribute("aria-describedby", hint.id);
  }
  hint.textContent = message; hint.hidden = !message;
}
function canSaveGridDrafts() {
  const invalid = [...gridDrafts.values()].find(draft => draft.error);
  if (!invalid) return true;
  const d = window.formaDesigner;
  d.select(invalid.id); send("select", invalid.id); d.inspector();
  byId(`prop-${invalid.property}`)?.focus();
  if (byId("status")) byId("status").textContent = "Fix the grid Rows JSON before saving. Your draft has been kept.";
  return false;
}

function containerHost(container, x, y) {
  const host = container.querySelector?.(":scope > .layout-content") ?? container;
  if (["splitcontainer", "tablelayoutpanel"].includes(container.dataset.formaType)) {
    const cells = Array.from(host.children).filter(child => child.dataset.layoutSlot);
    return cells.find(cell => {
      const rect = cell.getBoundingClientRect();
      return x >= rect.left && x <= rect.right && y >= rect.top && y <= rect.bottom;
    }) ?? cells[0] ?? host;
  }
  return host;
}
function flowInsertion(container, movingId, x, y) {
  const vertical = container.dataset.orientation === "vertical";
  const siblings = Array.from(containerHost(container).children).filter(child => child.dataset.formaType && child.id !== movingId);
  const index = siblings.findIndex(child => {
    const r = child.getBoundingClientRect();
    return vertical ? x < r.right && (x < r.left || y < r.top + r.height / 2)
      : y < r.bottom && (y < r.top || x < r.left + r.width / 2);
  });
  return { index: index < 0 ? siblings.length : index, element: index < 0 ? null : siblings[index] };
}

// Pointer capture keeps event.target on the dragged control. Hit-test underneath it.
function containerAtPoint(x, y, excluded = null, fallback = null) {
  const d = window.formaDesigner,
    root = d.root();
  const hits = document.elementsFromPoint?.(x, y) ?? [fallback];
  for (const hit of hits) {
    if (!hit || (excluded && (hit === excluded || excluded.contains?.(hit))))
      continue;
    let candidate = hit.closest?.("[data-forma-type]");
    while (candidate && candidate !== root) {
      if (
        containerKinds.has(candidate.dataset.formaType) &&
        candidate !== excluded &&
        !excluded?.contains?.(candidate)
      ) {
        const host = containerHost(candidate, x, y),
          rect = host.getBoundingClientRect();
        const right = rect.right ?? rect.left + host.clientWidth * d.zoom;
        const bottom = rect.bottom ?? rect.top + host.clientHeight * d.zoom;
        if (x >= rect.left && x <= right && y >= rect.top && y <= bottom)
          return candidate;
      }
      candidate = candidate.parentElement?.closest?.("[data-forma-type]");
    }
  }
  return root;
}

// Work in unscaled parent coordinates; the threshold stays six screen pixels.
function snapPosition(item, x, y, width, height, siblings, threshold = 6) {
  const guides = [];
  function axis(position, size, limit, start, extent, axisName) {
    let best = threshold + 1,
      result = position,
      guide;
    const consider = (target, offset, label = "", from = null) => {
      const candidate = target - offset,
        distance = Math.abs(candidate - position);
      if (
        candidate >= 0 &&
        candidate <= limit - size &&
        distance <= threshold &&
        distance < best
      ) {
        best = distance;
        result = candidate;
        guide = { axis: axisName, at: target, label, from };
      }
    };
    for (const edge of [0, limit / 2, limit])
      for (const offset of [0, size / 2, size]) consider(edge, offset);
    for (const sibling of siblings) {
      const origin = sibling[start] ?? 0,
        length = sibling[extent];
      for (const edge of [origin, origin + length / 2, origin + length])
        for (const offset of [0, size / 2, size]) consider(edge, offset);
      for (const gap of [8, 16]) {
        consider(origin + length + gap, 0, `${gap}px`, origin + length);
        consider(origin - gap, size, `${gap}px`, origin);
      }
    }
    if (guide) guides.push(guide);
    return Math.round(result);
  }
  return {
    x: axis(x, item.width, width, "x", "width", "x"),
    y: axis(y, item.height, height, "y", "height", "y"),
    guides,
  };
}

function resizeBounds(
  item,
  direction,
  dx,
  dy,
  parentWidth,
  parentHeight,
  isRoot = false,
) {
  const clamp = (v, lo, hi) => Math.max(lo, Math.min(hi, Math.round(v)));
  const minWidth = clamp(
    item.minimumWidth || (isRoot ? 240 : 24),
    isRoot ? 240 : 24,
    isRoot ? 1600 : parentWidth,
  );
  const minHeight = clamp(
    item.minimumHeight || (isRoot ? 160 : 20),
    isRoot ? 160 : 20,
    isRoot ? 1200 : parentHeight,
  );
  const maxWidth = clamp(
    item.maximumWidth || (isRoot ? 1600 : parentWidth),
    minWidth,
    isRoot ? 1600 : parentWidth,
  );
  const maxHeight = clamp(
    item.maximumHeight || (isRoot ? 1200 : parentHeight),
    minHeight,
    isRoot ? 1200 : parentHeight,
  );
  if (isRoot)
    return {
      x: 0,
      y: 0,
      width: direction.includes("e")
        ? clamp(item.width + dx, minWidth, maxWidth)
        : item.width,
      height: direction.includes("s")
        ? clamp(item.height + dy, minHeight, maxHeight)
        : item.height,
    };
  let left = item.x,
    top = item.y,
    right = item.x + item.width,
    bottom = item.y + item.height;
  if (direction.includes("w"))
    left = clamp(left + dx, Math.max(0, right - maxWidth), right - minWidth);
  if (direction.includes("e"))
    right = clamp(
      right + dx,
      left + minWidth,
      Math.min(parentWidth, left + maxWidth),
    );
  if (direction.includes("n"))
    top = clamp(top + dy, Math.max(0, bottom - maxHeight), bottom - minHeight);
  if (direction.includes("s"))
    bottom = clamp(
      bottom + dy,
      top + minHeight,
      Math.min(parentHeight, top + maxHeight),
    );
  return { x: left, y: top, width: right - left, height: bottom - top };
}

window.formaDesigner = {
  rootId: null,
  selectedId: null,
  state: null,
  zoom: 1,
  preview: false,
  drag: null,
  root() {
    return byId(this.rootId);
  },
  resizeBounds,
  snapPosition,
  clearGuides() {
    document.querySelectorAll(".snap-guide").forEach((el) => el.remove());
  },
  showGuides(guides, parent) {
    this.clearGuides();
    const root = this.root(),
      frame = byId("form-frame");
    if (!frame || !root) return;
    const origin = root.getBoundingClientRect(),
      rect = parent.getBoundingClientRect();
    const left = (rect.left - origin.left) / this.zoom,
      top = (rect.top - origin.top) / this.zoom + 34;
    for (const guide of guides) {
      const el = document.createElement("div");
      el.className = `snap-guide ${guide.axis === "x" ? "vertical" : "horizontal"}`;
      el.setAttribute("aria-hidden", "true");
      Object.assign(
        el.style,
        guide.axis === "x"
          ? {
              left: `${left + guide.at}px`,
              top: `${top}px`,
              height: `${parent.clientHeight}px`,
            }
          : {
              left: `${left}px`,
              top: `${top + guide.at}px`,
              width: `${parent.clientWidth}px`,
            },
      );
      if (guide.from != null && this.drag) {
        el.className = `snap-guide dimension ${guide.axis === "x" ? "horizontal" : "vertical"}`;
        Object.assign(
          el.style,
          guide.axis === "x"
            ? {
                left: `${left + Math.min(guide.from, guide.at)}px`,
                top: `${top + this.drag.top + this.drag.height / 2}px`,
                height: "0px",
                width: `${Math.abs(guide.at - guide.from)}px`,
              }
            : {
                left: `${left + this.drag.left + this.drag.width / 2}px`,
                top: `${top + Math.min(guide.from, guide.at)}px`,
                width: "0px",
                height: `${Math.abs(guide.at - guide.from)}px`,
              },
        );
      }
      if (guide.label) {
        const label = document.createElement("span");
        label.textContent = guide.label;
        el.appendChild(label);
      }
      frame.appendChild(el);
    }
  },
  layoutFrame() {
    const root = this.root();
    if (!root) return;
    const width = parseFloat(root.style.width) || 640,
      height = parseFloat(root.style.height) || 440;
    if (byId("form-frame"))
      Object.assign(byId("form-frame").style, {
        width: `${width + 2}px`,
        transform: `scale(${this.zoom})`,
      });
    if (byId("scaled-frame"))
      Object.assign(byId("scaled-frame").style, {
        width: `${(width + 2) * this.zoom}px`,
        height: `${(height + 36) * this.zoom}px`,
      });
    const device = byId("device-caption");
    if (device) device.textContent = `Desktop (${width} × ${height})`;
  },
  receive(message) {
    if (message.action === "component-event") {
      window.formaCustomization?.event(message.id, message.event);
      return;
    }
    if (message.action === "runtime-preview") {
      this.standalone = true;
      this.preview = true;
      this.rootId = message.id;
      this.state = message;
      document.body.classList.add("preview-mode");
      for (const item of message.controls) {
        this.applyAppearance(item);
        if (item.component) byId(item.id).hidden = true;
      }
      window.formaCustomization?.apply(message, true);
      return;
    }
    if (message.action === "return-to-design") {
      this.preview = false;
      this.updatePreview();
      return;
    }
    if (message.action === "initialize") {
      gridDrafts.clear();
      window.formaCustomization?.clear();
      this.cancelDrag();
      this.rootId = message.id;
      this.preview = false;
      document.body.classList.remove("preview-mode");
      const root = this.root();
      byId("canvas-host")?.appendChild(root);
      root?.setAttribute("aria-label", message.title ?? "Form1");
      const tray = byId("component-tray");
      if (tray) {
        tray.replaceChildren();
        tray.hidden = true;
      }
      this.updatePreview();
    }
    if (message.action === "title")
      this.root()?.setAttribute("aria-label", message.title);
    if (message.action === "select") this.select(message.id);
    if (message.action === "state") {
      this.state = message;
      for (const [key, draft] of gridDrafts) {
        const item = message.controls.find(control => control.id === draft.id);
        if (!item) { gridDrafts.delete(key); continue; }
        try { if (!draft.error && gridValue(draft.property, item[draft.property]) === gridValue(draft.property, draft.value)) gridDrafts.delete(key); } catch {}
      }
      document.querySelectorAll('[data-command="undo"]').forEach((button) => {
        button.disabled = !message.canUndo;
      });
      document.querySelectorAll('[data-command="redo"]').forEach((button) => {
        button.disabled = !message.canRedo;
      });
      this.root()?.setAttribute("aria-label", message.title);
      for (const name of ["canvas-title", "form-name"])
        if (byId(name)) byId(name).textContent = message.title;
      if (byId("status")) byId("status").textContent = message.status;
      for (const item of message.controls) this.applyAppearance(item);
      window.formaCustomization?.apply(message);
      this.select(message.selectedId);
      this.inspector();
    }
  },
  select(id) {
    if (id !== this.selectedId) {
      const previous = byId(this.selectedId);
      if (previous?.dataset.formaType === "richtextbox") {
        previous.dataset.editing = "false";
        previous.querySelector(".rich-content").contentEditable = "false";
        previous.querySelectorAll("[data-rich-format]").forEach((button) => {
          button.disabled = true;
        });
      }
    }
    this.selectedId = id;
    document
      .querySelectorAll(".forma-selected")
      .forEach((el) => el.classList.remove("forma-selected"));
    byId(id)?.classList.add("forma-selected");
    this.outline();
  },
  controlById(id) {
    const controls = this.state?.controls;
    if (!controls) return undefined;
    if (this._indexedControls !== controls) {
      this._indexedControls = controls;
      this._controlsIndex = new Map(controls.map(item => [item.id, item]));
    }
    return this._controlsIndex.get(id);
  },
  applyAppearance(item) {
    const el = byId(item.id);
    if (!el) return;
    const parentState = this.controlById(item.parentId);
    const signature = JSON.stringify([item, this.preview, parentState?.selectedTab, el.dataset.editing, el._formaRevision]);
    if (el._appearanceSignature === signature) return;
    el._appearanceSignature = signature;
    if (item.component) {
      el.dataset.disabled = String(!item.enabled);
      if (item.kind === "tooltip" && !item.enabled) window.formaTooltip?.removing(el);
      const tray = byId("component-tray");
      if (tray) {
        tray.appendChild(el);
        tray.hidden = false;
      }
      el.hidden = false;
      el.textContent = item.name;
      el.dataset.component = "true";
      Object.assign(el.style, {
        position: "",
        left: "",
        top: "",
        width: "",
        height: "",
      });
      window.formaModern?.refresh(el, item);
      return;
    }
    if (el.style.removeProperty)
      for (const name of (el.dataset.customProperties ?? "")
        .split(",")
        .filter(Boolean))
        el.style.removeProperty(name);
    el.dataset.tag = item.tag ?? "";
    for (const cls of (el.dataset.userClasses ?? "")
      .split(/\s+/)
      .filter(Boolean))
      el.classList.remove(cls);
    const classes = (item.cssClass ?? "")
      .split(/\s+/)
      .filter(
        (c) =>
          /^[a-zA-Z_][\w-]*$/.test(c) &&
          !c.startsWith("forma-") &&
          !c.startsWith("design-"),
      );
    classes.forEach((c) => el.classList.add(c));
    el.dataset.userClasses = classes.join(" ");
    Object.assign(el.style, {
      width: `${item.width}px`,
      height: `${item.height}px`,
      color: item.foreColor,
      backgroundColor: item.backColor,
      fontSize: `${item.fontSize}px`,
      fontFamily: item.fontFamily,
      fontWeight: item.fontWeight,
      fontStyle: item.fontStyle,
      textAlign: item.textAlign,
    });
    if (item.id === this.rootId) {
      this.layoutFrame();
      if (byId("canvas-title"))
        Object.assign(byId("canvas-title").style, {
          fontFamily: item.fontFamily,
          fontWeight: item.fontWeight,
          fontStyle: item.fontStyle,
          fontSize: `${item.fontSize}px`,
          color: item.foreColor,
        });
    }
    if (item.id !== this.rootId) {
      Object.assign(el.style, {
        borderStyle: item.borderStyle,
        borderColor: item.borderColor,
        borderWidth: `${item.borderWidth}px`,
        borderRadius: `${item.borderRadius}px`,
        padding: `${item.padding}px`,
        opacity: String(
          ((item.opacity ?? 100) / 100) *
            (!this.preview && (!item.enabled || !item.visible) ? 0.5 : 1),
        ),
      });
      if (item.kind === "label")
        el.style.justifyContent = {
          left: "flex-start",
          center: "center",
          right: "flex-end",
        }[item.textAlign];
      Object.assign(el.style, {
        lineHeight: String(item.lineHeight ?? 1.5),
        letterSpacing: `${item.letterSpacing ?? 0}px`,
        marginTop: `${item.marginTop ?? 0}px`,
        marginRight: `${item.marginRight ?? 0}px`,
        marginBottom: `${item.marginBottom ?? 0}px`,
        marginLeft: `${item.marginLeft ?? 0}px`,
        paddingTop: `${item.paddingTop ?? 8}px`,
        paddingRight: `${item.paddingRight ?? 8}px`,
        paddingBottom: `${item.paddingBottom ?? 8}px`,
        paddingLeft: `${item.paddingLeft ?? 8}px`,
        zIndex: String(item.zIndex ?? 0),
        boxShadow: {
          None: "none",
          Small: "0 1px 3px #0002",
          Medium: "0 4px 12px #0003",
          Large: "0 8px 24px #0003",
        }[item.shadow ?? "None"],
        cursor: this.preview ? item.cursor : item.locked ? "default" : "move",
      });
      if (window.formaTooltip) window.formaTooltip.setTitle(el, item.toolTip ?? "");
      else el.title = item.toolTip ?? "";
      el.tabIndex =
        this.preview && item.focusable && item.enabled
          ? (item.tabIndex ?? 0)
          : -1;
      // Parse visual declarations while retaining C# geometry and designer state.
      if (el.style.setProperty) {
        const parser = document.createElement("div");
        parser.style.cssText = item.customCss ?? "";
        const applied = [];
        for (const name of parser.style) {
          if (
            /^(color|background-color|font-|line-height|letter-spacing|text-|border-|box-shadow)/.test(
              name,
            )
          ) {
            el.style.setProperty(name, parser.style.getPropertyValue(name));
            applied.push(name);
          }
        }
        el.dataset.customProperties = applied.join(",");
      }
      // Hidden and disabled controls stay selectable in design mode.
      el.classList.toggle("design-muted", !item.enabled || !item.visible);
      const parent = this.controlById(item.parentId);
      el.hidden =
        (this.preview && !item.visible) ||
        (parent?.kind === "tabcontrol" &&
          item.layoutSlot !== parent.selectedTab + 1);
      if (["treeview", "pagination"].includes(item.kind))
        el.querySelectorAll("button").forEach((button) => {
          button.disabled =
            (this.preview && !item.enabled) ||
            button.dataset.boundary === "true";
        });
      if (["filepicker", "folderpicker"].includes(item.kind))
        el.querySelectorAll("button").forEach((button) => {
          button.disabled = this.preview && !item.enabled;
        });
      if (["menustrip", "toolbar", "toolstrip"].includes(item.kind)) {
        el.dataset.disabled = String(this.preview && !item.enabled);
        el.querySelectorAll("button").forEach((button) => {
          button.disabled =
            el.dataset.disabled === "true" ||
            button.dataset.itemDisabled === "true";
        });
        if (el.dataset.disabled === "true" || !this.preview)
          el.querySelectorAll("details").forEach((menu) => {
            menu.open = false;
          });
      }
      if (item.kind === "propertygrid")
        el.querySelectorAll("input").forEach((input) => {
          input.readOnly =
            !this.preview ||
            item.readOnly ||
            input.dataset.entryReadOnly === "true";
        });
      if (item.kind === "richtextbox") {
        const editing = this.preview || el.dataset.editing === "true";
        const allowed =
          editing &&
          !item.readOnly &&
          (this.preview ? item.enabled : !item.locked);
        el.querySelector(".rich-content").contentEditable = allowed
          ? "true"
          : "false";
        el.querySelectorAll("[data-rich-format]").forEach((button) => {
          button.disabled = !allowed;
        });
      }
      if (item.kind === "linklabel")
        el.setAttribute("aria-disabled", String(this.preview && !item.enabled));
      for (const field of el.querySelectorAll?.(
        "input,select,[contenteditable]",
      ) ?? []) {
        field.disabled = this.preview && !item.enabled;
        if (
          item.kind !== "richtextbox" &&
          field.hasAttribute?.("contenteditable")
        )
          field.contentEditable =
            this.preview && item.enabled && !item.readOnly ? "true" : "false";
      }
      if (["combobox", "listbox", "listview"].includes(item.kind))
        el.disabled = this.preview && !item.enabled;
      if (
        [
          "button",
          "textbox",
          "maskedtextbox",
          "searchbox",
          "passwordbox",
          "textarea",
          "numericupdown",
          "slider",
          "datepicker",
          "timepicker",
          "datetimepicker",
          "colorpicker",
          "togglebutton",
        ].includes(item.kind)
      )
        el.disabled = this.preview && !item.enabled;
      if (
        [
          "textbox",
          "maskedtextbox",
          "searchbox",
          "passwordbox",
          "textarea",
        ].includes(item.kind)
      ) {
        el.placeholder = item.placeholder;
        el.readOnly = item.readOnly;
        if (item.kind !== "textarea")
          el.type = item.password
            ? "password"
            : item.kind === "searchbox"
              ? "search"
              : "text";
        el.maxLength = item.maxLength;
      }
      if (item.kind === "datagridview") {
        el.dataset.disabled = String(!item.enabled);
        window.formaDataGrid?.render(el, { readOnly: item.readOnly });
      }
      window.formaModern?.refresh(el, item);
    }
  },
  inspector() {
    const selected = this.state?.controls.find((c) => c.id === this.selectedId);
    if (!selected) return;
    const selection = byId("selection");
    if (selection) {
      const optionsSignature = JSON.stringify(this.state.controls.map(c => [c.id, c.name]));
      if (selection._optionsSignature !== optionsSignature) {
        selection._optionsSignature = optionsSignature;
        selection.replaceChildren(
        ...this.state.controls.map((c) => {
          const option = document.createElement("option");
          option.value = c.id;
          option.textContent = c.name;
          return option;
        }),
      );
      }
      selection.value = selected.id;
    }
    const schema = this.state.propertySchema ?? [];
    this.buildEditors(schema);
    byId("property-editors")
      ?.querySelectorAll(".property-actions button")
      .forEach((button) => {
        button.disabled =
          button.dataset.command === "show-dialog"
            ? !this.preview || !selected.enabled
            : this.preview || selected.locked;
      });
    for (const descriptor of schema) {
      const property = descriptor.id;
      const input = byId(
        descriptor.category === "Advanced" && property === "id"
          ? "prop-cssId"
          : `prop-${property}`,
      );
      if (!input) continue;
      const sameOwner = input.dataset.controlId === selected.id;
      input.dataset.controlId = selected.id;
      if (gridFields.has(property)) {
        const draft = gridDrafts.get(`${selected.id}:${property}`);
        if (draft) { if (input.value !== draft.value) input.value = draft.value; }
        else if (!(sameOwner && document.activeElement === input)) input.value = selected[property];
        gridError(input, draft?.error ?? "");
      }
      else if (input.type === "checkbox") input.checked = selected[property];
      else if (input.value !== String(selected[property]))
        input.value = selected[property];
      input.disabled =
        this.preview ||
        descriptor.readOnly ||
        (selected.locked && property !== "locked");
      if (property === "source" && selected.source?.startsWith("data:")) {
        input.value = "Embedded image";
        input.disabled = true;
      }
      if (
        ["x", "y"].includes(property) &&
        managedKinds.has(
          this.state.controls.find((c) => c.id === selected.parentId)?.kind,
        )
      )
        input.disabled = true;
      if (["x", "width"].includes(property))
        input.max =
          selected.id === this.rootId ? 1600 : this.root().clientWidth;
      if (["y", "height"].includes(property))
        input.max =
          selected.id === this.rootId ? 1200 : this.root().clientHeight;
    }
    if (byId("selection-status"))
      byId("selection-status").textContent =
        selected.id === this.rootId ? "Form selected" : `Selected ${selected.name}`;
    this.coordinates(selected.x, selected.y);
  },
  buildEditors(schema) {
    const container = byId("property-editors");
    if (!container) return;
    const signature =
      JSON.stringify(schema) +
      (schema.some((property) => property.editor === "target")
        ? JSON.stringify(
            this.state?.controls
              .filter((item) => !item.component)
              .map((item) => [item.id, item.name]),
          )
        : "");
    if (container.dataset.schema === signature) return;
    const openCategories = new Map(
      Array.from(container.querySelectorAll("details")).map((group) => [
        group.dataset.category,
        group.open,
      ]),
    );
    container.replaceChildren();
    container.dataset.schema = signature;
    const groups = new Map();
    for (const property of schema) {
      let group = groups.get(property.category);
      if (!group) {
        group = document.createElement("details");
        group.className = "property-group";
        group.dataset.category = property.category;
        group.open =
          openCategories.get(property.category) ??
          property.category !== "Advanced";
        const summary = document.createElement("summary");
        summary.textContent = property.category;
        group.appendChild(summary);
        groups.set(property.category, group);
        container.appendChild(group);
      }
      const label = document.createElement("label");
      label.appendChild(document.createTextNode(property.label));
      const input = document.createElement(
        ["select", "target"].includes(property.editor)
          ? "select"
          : property.editor === "textarea"
            ? "textarea"
            : "input",
      );
      // ID is shown in General and Advanced; both are read-only.
      input.id =
        property.category === "Advanced" && property.id === "id"
          ? "prop-cssId"
          : `prop-${property.id}`;
      input.dataset.property = property.id;
      if (property.editor === "target") {
        const choices = [
          { id: "", name: "Form (default)" },
          ...(this.state?.controls.filter((item) => !item.component) ?? []),
        ];
        for (const choice of choices) {
          const option = document.createElement("option");
          option.value = choice.id;
          option.textContent = choice.name;
          input.appendChild(option);
        }
      } else if (property.editor === "select")
        for (const choice of property.options) {
          const option = document.createElement("option");
          option.value = choice;
          option.textContent = choice;
          input.appendChild(option);
        }
      else if (property.editor !== "textarea") input.type = property.editor;
      if (property.min != null) input.min = property.min;
      if (property.max != null) input.max = property.max;
      if (property.editor === "number") input.step = "any";
      if (property.editor === "checkbox") input.setAttribute("role", "switch");
      if (property.id === "gridRows") input.placeholder = '[["Alice", "Engineering"], ["Bob", "Sales"]]';
      if (property.id === "customCss") {
        input.placeholder = "letter-spacing: 0.5px;";
        input.title =
          "Visual CSS declarations; geometry remains controlled by Layout.";
      }
      if (property.editor === "checkbox") {
        const wrapper = document.createElement("span");
        wrapper.className = "switch";
        const track = document.createElement("span");
        track.className = "switch-track";
        track.setAttribute("aria-hidden", "true");
        wrapper.appendChild(input);
        wrapper.appendChild(track);
        label.appendChild(wrapper);
      } else label.appendChild(input);
      group.appendChild(label);
    }
    const selected = this.state?.controls.find((c) => c.id === this.selectedId);
    if (selected) {
      const actions = document.createElement("div");
      actions.className = "property-actions";
      const commands = ["image", "picturebox", "avatar"].includes(selected.kind)
        ? [["choose-image", "Choose image…"]]
        : [];
      if (selected.kind === "richtextbox")
        commands.push(["edit-rich", "Edit content / Finish"]);
      if (["filepicker", "folderpicker"].includes(selected.kind))
        commands.push([
          "choose-path",
          selected.kind === "filepicker" ? "Choose file…" : "Choose folder…",
        ]);
      if (["dialog", "confirmationdialog"].includes(selected.kind))
        commands.push(["show-dialog", "Show dialog (Preview)"]);
      if (selected.kind === "tabcontrol")
        commands.push(["add-tab", "Add tab"]);
      if (!selected.component && selected.id !== this.rootId)
        commands.push(
          ["bring-front", "Bring to front"],
          ["send-back", "Send to back"],
        );
      for (const [command, text] of commands) {
        const button = document.createElement("button");
        button.dataset.command = command;
        button.textContent = text;
        actions.appendChild(button);
      }
      if (commands.length) container.prepend(actions);
      let advanced = groups.get("Advanced");
      if (!advanced) {
        advanced = document.createElement("details");
        advanced.className = "property-group";
        advanced.dataset.category = "Advanced";
        advanced.open = openCategories.get("Advanced") ?? false;
        const summary = document.createElement("summary");
        summary.textContent = "Advanced";
        advanced.appendChild(summary);
        container.appendChild(advanced);
      }
      const customActions = document.createElement("div");
      customActions.className = "property-actions";
      const customButton = document.createElement("button");
      customButton.dataset.command = "edit-custom-properties";
      customButton.textContent = "Custom Properties…";
      customActions.appendChild(customButton);
      advanced.appendChild(customActions);
    }
  },
  coordinates(x, y) {
    if (byId("coordinates"))
      byId("coordinates").textContent = `X: ${x}   Y: ${y}`;
  },
  outline() {
    const root = this.root(),
      selected = byId(this.selectedId);
    const item = this.state?.controls.find((c) => c.id === this.selectedId);
    let outline = byId("selection-outline");
    if (!root || !selected || this.preview || item?.component) {
      if (outline) outline.hidden = true;
      return;
    }
    if (!outline) {
      outline = document.createElement("div");
      outline.id = "selection-outline";
      outline.setAttribute("aria-hidden", "true");
      for (const direction of ["nw", "n", "ne", "e", "se", "s", "sw", "w"]) {
        const handle = document.createElement("i");
        handle.dataset.handle = direction;
        handle.setAttribute("title", `Resize ${direction}`);
        outline.appendChild(handle);
      }
      (byId("form-frame") ?? root).appendChild(outline);
    }
    outline.hidden = false;
    const isRoot = selected === root;
    Object.assign(outline.style, {
      left: isRoot
        ? "-1px"
        : `${(parseFloat(selected.style.left) || 0) + (parseFloat(selected.style.marginLeft) || 0)}px`,
      top: isRoot
        ? "-1px"
        : `${(parseFloat(selected.style.top) || 0) + (parseFloat(selected.style.marginTop) || 0) + 34}px`,
      width: `${parseFloat(selected.style.width) + (isRoot ? 2 : 0)}px`,
      height: `${parseFloat(selected.style.height) + (isRoot ? 36 : 0)}px`,
    });
    if (!isRoot && selected.getBoundingClientRect) {
      const rect = selected.getBoundingClientRect(),
        origin = root.getBoundingClientRect();
      Object.assign(outline.style, {
        left: `${(rect.left - origin.left) / this.zoom}px`,
        top: `${(rect.top - origin.top) / this.zoom + 34}px`,
        width: `${rect.width / this.zoom}px`,
        height: `${rect.height / this.zoom}px`,
      });
    }
    outline.querySelectorAll?.("[data-handle]").forEach((h) => {
      h.hidden =
        item?.locked ||
        (isRoot && !["e", "s", "se"].includes(h.dataset.handle));
    });
  },
  cancelDrag() {
    this.clearGuides();
    if (!this.drag) return;
    const drag = this.drag;
    this.drag = null;
    drag.dropParent?.classList.remove("drop-target");
    drag.insertionTarget?.classList.remove("layout-insertion");
    if (drag.managed) drag.element.style.transform = drag.originalTransform ?? "";
    Object.assign(drag.element.style, {
      left: drag.managed ? drag.originalLeft ?? "" : `${drag.x}px`,
      top: drag.managed ? drag.originalTop ?? "" : `${drag.y}px`,
    });
    if (drag.mode === "resize")
      Object.assign(drag.element.style, {
        width: `${drag.width}px`,
        height: `${drag.height}px`,
      });
    const capture = drag.capture ?? drag.element;
    if (capture.hasPointerCapture?.(drag.pointerId))
      capture.releasePointerCapture(drag.pointerId);
    this.coordinates(drag.x, drag.y);
    for (const [key, value] of [
      ["x", drag.x],
      ["y", drag.y],
    ])
      if (byId(`prop-${key}`)) byId(`prop-${key}`).value = value;
    this.outline();
    this.layoutFrame();
    this.inspector();
  },
  updatePreview() {
    if (!this.preview) window.forma.closeTransientUi?.();
    document.body.classList.toggle("preview-mode", this.preview);
    if (byId("mode-label"))
      byId("mode-label").textContent = this.preview ? "Preview" : "Design";
    if (byId("preview-caption"))
      byId("preview-caption").textContent = this.preview
        ? "Back to design"
        : "Preview";
    this.state?.controls.forEach((item) => this.applyAppearance(item));
    this.inspector();
    this.outline();
  },
};

function canvasPoint(event) {
  const d = window.formaDesigner,
    root = d.root(),
    rect = root.getBoundingClientRect();
  return {
    x: Math.round((event.clientX - rect.left) / d.zoom - root.clientLeft),
    y: Math.round((event.clientY - rect.top) / d.zoom - root.clientTop),
  };
}

document.addEventListener("dragstart", (event) => {
  const tool = event.target.closest("[data-kind]");
  if (
    !tool ||
    !supportedKinds.has(tool.dataset.kind) ||
    window.formaDesigner.preview
  )
    return;
  event.dataTransfer.setData("text/plain", `forma:${tool.dataset.kind}`);
  event.dataTransfer.effectAllowed = "copy";
});
document.addEventListener("dragover", (event) => {
  const d = window.formaDesigner,
    root = d.root();
  if (d.preview || !root || !root.contains(event.target)) return;
  if (!Array.from(event.dataTransfer.types).includes("text/plain")) return;
  event.preventDefault();
  event.dataTransfer.dropEffect = "copy";
  root.classList.add("drop-target");
});
document.addEventListener("dragleave", (event) => {
  const root = window.formaDesigner.root();
  if (root && !root.contains(event.relatedTarget))
    root.classList.remove("drop-target");
});
document.addEventListener("drop", (event) => {
  const d = window.formaDesigner,
    root = d.root();
  root?.classList.remove("drop-target");
  if (d.preview || !root || !root.contains(event.target)) return;
  event.preventDefault();
  const text = event.dataTransfer.getData("text/plain");
  if (!text.startsWith("forma:")) return;
  const kind = text.slice(6);
  const parent = containerAtPoint(
    event.clientX,
    event.clientY,
    null,
    event.target,
  );
  const host = containerHost(parent, event.clientX, event.clientY);
  const rect = host.getBoundingClientRect();
  if (supportedKinds.has(kind))
    send("drop", parent.id, {
      control: kind,
      ...(host.dataset.layoutSlot ? { layoutSlot: Number(host.dataset.layoutSlot) } : {}),
      ...(parent.dataset.formaType === "flowlayoutpanel" ? { index: flowInsertion(parent, null, event.clientX, event.clientY).index } : {}),
      x: Math.max(
        0,
        Math.round(
          (event.clientX - rect.left) / d.zoom - (host.clientLeft || 0),
        ),
      ),
      y: Math.max(
        0,
        Math.round((event.clientY - rect.top) / d.zoom - (host.clientTop || 0)),
      ),
    });
});

document.addEventListener(
  "pointerdown",
  (event) => {
    const d = window.formaDesigner,
      root = d.root();
    document.querySelectorAll(".command-menu[open]").forEach((menu) => {
      if (!menu.contains(event.target)) menu.open = false;
    });
    if (d.preview && root?.contains(event.target)) {
      const control = event.target.closest("[data-forma-type]");
      const item = d.state?.controls.find((c) => c.id === control?.id);
      if (item && item.focusable === false) event.preventDefault();
      return;
    }
    const component = event.target.closest("[data-component]");
    if (component) {
      d.select(component.id);
      send("select", component.id);
      return;
    }
    if (event.target.closest("[data-tab-index], [data-tab-add]")) return;
    const richEditor = event.target.closest('[data-forma-type="richtextbox"]');
    if (richEditor?.dataset.editing === "true") return;
    const handle = event.target.closest("[data-handle]");
    if (handle && !d.preview && event.button === 0) {
      const item = d.state?.controls.find((c) => c.id === d.selectedId);
      if (!item || item.locked) return;
      event.preventDefault();
      event.stopImmediatePropagation();
      d.drag = {
        ...item,
        element: byId(item.id),
        capture: handle,
        mode: "resize",
        direction: handle.dataset.handle,
        pointerId: event.pointerId,
        startX: event.clientX,
        startY: event.clientY,
        x: item.x,
        y: item.y,
        width: item.width,
        height: item.height,
        moved: false,
      };
      handle.setPointerCapture(event.pointerId);
      return;
    }
    if (
      !d.preview &&
      event.button === 0 &&
      event.target.closest(".form-titlebar")
    ) {
      d.select(d.rootId);
      send("select", d.rootId);
      return;
    }
    if (d.preview || event.button !== 0 || !root?.contains(event.target))
      return;
    const control = event.target.closest("[data-forma-type]");
    if (!control) return;
    event.preventDefault();
    event.stopImmediatePropagation();
    d.select(control.id);
    send("select", control.id);
    if (control === root) return;
    const item = d.state?.controls.find((c) => c.id === control.id);
    if (!item || item.locked) return;
    const managed = managedKinds.has(d.state?.controls.find(c => c.id === item.parentId)?.kind);
    const originalHost = control.parentElement ?? root;
    const origin = originalHost.getBoundingClientRect();
    const rect = control.getBoundingClientRect?.();
    d.drag = {
      ...item,
      element: control,
      originalHost, origin, managed,
      originalLeft: control.style.left, originalTop: control.style.top, originalTransform: control.style.transform,
      visualX: managed && rect ? (rect.left - origin.left) / d.zoom - (originalHost.clientLeft || 0) : item.x,
      visualY: managed && rect ? (rect.top - origin.top) / d.zoom - (originalHost.clientTop || 0) : item.y,
      pointerId: event.pointerId,
      startX: event.clientX,
      startY: event.clientY,
      x: item.x,
      y: item.y,
      width: item.width,
      height: item.height,
      moved: false,
      left: item.x,
      top: item.y,
    };
    control.setPointerCapture(event.pointerId);
  },
  true,
);
document.addEventListener("pointermove", (event) => {
  const d = window.formaDesigner,
    drag = d.drag;
  if (!drag || drag.pointerId !== event.pointerId) return;
  let parent = drag.element.parentElement ?? d.root();
  const dx = (event.clientX - drag.startX) / d.zoom,
    dy = (event.clientY - drag.startY) / d.zoom;
  if (!drag.moved && Math.abs(dx) < 3 && Math.abs(dy) < 3) return;
  drag.moved = true;
  if (drag.mode === "resize") {
    drag.bounds = resizeBounds(
      drag,
      drag.direction,
      dx,
      dy,
      parent.clientWidth - (drag.marginLeft ?? 0) - (drag.marginRight ?? 0),
      parent.clientHeight - (drag.marginTop ?? 0) - (drag.marginBottom ?? 0),
      drag.element === d.root(),
    );
    Object.assign(drag.element.style, {
      left: `${drag.bounds.x}px`,
      top: `${drag.bounds.y}px`,
      width: `${drag.bounds.width}px`,
      height: `${drag.bounds.height}px`,
    });
    for (const key of ["x", "y", "width", "height"])
      if (byId(`prop-${key}`)) byId(`prop-${key}`).value = drag.bounds[key];
    d.layoutFrame();
    d.outline();
    d.coordinates(drag.bounds.x, drag.bounds.y);
    return;
  }
  let proposedX = (drag.visualX ?? drag.x) + dx,
    proposedY = (drag.visualY ?? drag.y) + dy;
  drag.insertionTarget?.classList.remove("layout-insertion");
  drag.insertionTarget = null;
  drag.destination = null;
  if (document.elementsFromPoint && drag.originalHost && drag.origin) {
    drag.dropParent?.classList.remove("drop-target");
    drag.dropParent = null;
    const target = containerAtPoint(event.clientX, event.clientY, drag.element);
    const item = d.state?.controls.find((c) => c.id === target?.id);
    if (target && !item?.locked) {
      drag.dropParent = target;
      parent = containerHost(target, event.clientX, event.clientY);
      drag.destination = { ...(target.id !== drag.parentId ? { parentId: target.id } : {}),
        ...(parent.dataset.layoutSlot ? { layoutSlot: Number(parent.dataset.layoutSlot) } : {}) };
      if (target.dataset.formaType === "flowlayoutpanel") {
        const insertion = flowInsertion(target, drag.id, event.clientX, event.clientY);
        drag.destination.index = insertion.index;
        drag.insertionTarget = insertion.element;
        drag.insertionTarget?.classList.add("layout-insertion");
      }
      const rect = parent.getBoundingClientRect();
      proposedX +=
        (drag.origin.left - rect.left) / d.zoom +
        (drag.originalHost.clientLeft || 0) -
        (parent.clientLeft || 0);
      proposedY +=
        (drag.origin.top - rect.top) / d.zoom +
        (drag.originalHost.clientTop || 0) -
        (parent.clientTop || 0);
      if (parent !== drag.originalHost) target.classList.add("drop-target");
    }
  }
  drag.left = Math.max(
    0,
    Math.min(
      parent.clientWidth -
        drag.width -
        (drag.marginLeft ?? 0) -
        (drag.marginRight ?? 0),
      Math.round(proposedX),
    ),
  );
  drag.top = Math.max(
    0,
    Math.min(
      parent.clientHeight -
        drag.height -
        (drag.marginTop ?? 0) -
        (drag.marginBottom ?? 0),
      Math.round(proposedY),
    ),
  );
  if (!event.altKey) {
    const siblings = (d.state?.controls ?? []).filter(
      (c) =>
        c.id !== drag.id &&
        c.parentId === (drag.dropParent?.id ?? drag.parentId) &&
        !c.component &&
        c.visible !== false &&
        !byId(c.id)?.hidden,
    );
    const snapped = snapPosition(
      drag,
      drag.left,
      drag.top,
      parent.clientWidth,
      parent.clientHeight,
      siblings,
      6 / d.zoom,
    );
    drag.left = snapped.x;
    drag.top = snapped.y;
    d.showGuides(snapped.guides, parent);
  } else d.clearGuides();
  if (drag.managed) {
    drag.element.style.transform = `translate(${dx}px, ${dy}px)`;
    d.outline();
    return;
  }
  const destinationRect = parent.getBoundingClientRect();
  const originalRect =
    drag.originalHost?.getBoundingClientRect() ?? destinationRect;
  Object.assign(drag.element.style, {
    left: `${
      drag.left +
      (destinationRect.left - originalRect.left) / d.zoom +
      (parent.clientLeft || 0) -
      (drag.originalHost?.clientLeft ?? parent.clientLeft ?? 0)
    }px`,
    top: `${
      drag.top +
      (destinationRect.top - originalRect.top) / d.zoom +
      (parent.clientTop || 0) -
      (drag.originalHost?.clientTop ?? parent.clientTop ?? 0)
    }px`,
  });
  d.coordinates(drag.left, drag.top);
  d.outline();
  if (byId("prop-x")) byId("prop-x").value = drag.left;
  if (byId("prop-y")) byId("prop-y").value = drag.top;
});
document.addEventListener("pointerup", (event) => {
  const d = window.formaDesigner,
    drag = d.drag;
  if (!drag || drag.pointerId !== event.pointerId) return;
  d.drag = null;
  drag.dropParent?.classList.remove("drop-target");
  drag.insertionTarget?.classList.remove("layout-insertion");
  if (drag.managed) drag.element.style.transform = drag.originalTransform ?? "";
  d.clearGuides();
  const capture = drag.capture ?? drag.element;
  if (capture.hasPointerCapture(event.pointerId))
    capture.releasePointerCapture(event.pointerId);
  if (drag.moved)
    send(
      drag.mode === "resize" ? "resize" : "move",
      drag.element.id,
      drag.mode === "resize"
        ? drag.bounds
        : {
            x: drag.left,
            y: drag.top,
            ...(drag.destination ?? {}),
          },
    );
});
document.addEventListener("pointercancel", () =>
  window.formaDesigner.cancelDrag(),
);
document.addEventListener("lostpointercapture", () =>
  window.formaDesigner.cancelDrag(),
);

document.addEventListener(
  "click",
  (event) => {
    const d = window.formaDesigner,
      root = d.root();
    if (event.target.closest("[data-handle]")) {
      event.preventDefault();
      event.stopImmediatePropagation();
      return;
    }
    if (root?.contains(event.target)) {
      if (
        d.preview ||
        event.target.closest("[data-tab-index], [data-tab-add]") ||
        event.target.closest('[data-forma-type="richtextbox"]')?.dataset
          .editing === "true"
      )
        return;
      const control = event.target.closest("[data-forma-type]");
      if (control) {
        event.preventDefault();
        event.stopImmediatePropagation();
        send("select", control.id);
      }
      return;
    }
    const command = event.target.closest("[data-command]");
    if (command?.dataset.command === "edit-rich" && !command.disabled) {
      const el = byId(d.selectedId),
        item = d.state?.controls.find((c) => c.id === d.selectedId);
      if (el && item) {
        el.dataset.editing = el.dataset.editing === "true" ? "false" : "true";
        d.applyAppearance(item);
        if (el.dataset.editing === "true")
          el.querySelector(".rich-content").focus();
      }
      return;
    }
    if (command && ["save", "save-as"].includes(command.dataset.command) && !canSaveGridDrafts()) return;
    if (command && !command.disabled)
      send("command", command.dataset.command === "add-tab" ? d.selectedId : d.rootId, { command: command.dataset.command });
    const toggle = event.target.closest("[data-toggle]");
    if (toggle) {
      const panel = byId(toggle.dataset.toggle);
      panel.hidden = !panel.hidden;
    }
    if (event.target.closest("[data-preview]")) {
      d.cancelDrag();
      d.preview = !d.preview;
      d.updatePreview();
      send("preview", d.rootId, { enabled: d.preview });
    }
    document.querySelectorAll(".menubar details").forEach((menu) => {
      if (!menu.contains(event.target) || command || toggle) menu.open = false;
    });
  },
  true,
);
document.addEventListener("dblclick", (event) => {
  const d = window.formaDesigner,
    tool = event.target.closest("[data-kind]");
  if (tool && !d.preview)
    send("drop", d.rootId, { control: tool.dataset.kind, x: 32, y: 32 });
});
document.addEventListener("change", (event) => {
  const d = window.formaDesigner,
    input = event.target;
  if (input.id === "zoom") {
    d.cancelDrag();
    d.zoom = Number(input.value);
    d.layoutFrame();
  } else if (input.id === "selection") send("select", input.value);
  else if (input.dataset.property && !d.preview) {
    if (gridFields.has(input.dataset.property)) return; // Valid grid drafts commit on input, before selection or Save.
    const value =
      input.type === "checkbox"
        ? input.checked
        : input.type === "number"
          ? Number(input.value)
          : input.value;
    send("property", d.selectedId, { property: input.dataset.property, value });
  }
});
document.addEventListener("input", (event) => {
  const d = window.formaDesigner;
  const input = event.target;
  if (gridFields.has(input.dataset.property) && !d.preview && !input.disabled) {
    const id = input.dataset.controlId ?? d.selectedId, property = input.dataset.property;
    let error = "";
    try { gridValue(property, input.value); } catch (issue) { error = issue.message; }
    gridDrafts.set(`${id}:${property}`, { id, property, value: input.value, error });
    gridError(input, error);
    if (!error) send("property", id, { property, value: input.value });
    return;
  }
  if (event.target.id === "prop-text" && !d.preview) {
    send("property", d.selectedId, {
      property: "text",
      value: event.target.value,
    });
    return;
  }
  if (event.target.id !== "search") return;
  const query = event.target.value.trim().toLowerCase();
  document.querySelectorAll(".tool-group button").forEach((tool) => {
    tool.hidden = !tool.textContent.toLowerCase().includes(query);
  });
});
document.addEventListener("keydown", (event) => {
  const d = window.formaDesigner;
  if (d.standalone) return;
  if (event.key === "Escape") {
    d.cancelDrag();
    if (d.preview) {
      d.preview = false;
      d.updatePreview();
      send("preview", d.rootId, { enabled: false });
    }
    return;
  }
  if (event.ctrlKey && ["s", "o"].includes(event.key.toLowerCase())) {
    event.preventDefault();
    if (event.key.toLowerCase() === "s" && !canSaveGridDrafts()) return;
    d.cancelDrag();
    send("command", d.rootId, {
      command:
        event.key.toLowerCase() === "o"
          ? "open"
          : event.shiftKey
            ? "save-as"
            : "save",
    });
    return;
  }
  if (event.target.closest("input,select,textarea,[contenteditable=true]"))
    return;
  if (event.ctrlKey && ["z", "y"].includes(event.key.toLowerCase())) {
    if (d.preview) return;
    event.preventDefault();
    d.cancelDrag();
    send("command", d.rootId, {
      command:
        event.key.toLowerCase() === "y" || event.shiftKey ? "redo" : "undo",
    });
    return;
  }
  if (event.ctrlKey && event.key.toLowerCase() === "n") {
    event.preventDefault();
    send("command", d.rootId, { command: "new" });
  }
  if (d.preview) return;
  if (event.key === "Delete") {
    event.preventDefault();
    send("command", d.rootId, { command: "delete" });
  }
  const delta = {
    ArrowLeft: [-1, 0],
    ArrowRight: [1, 0],
    ArrowUp: [0, -1],
    ArrowDown: [0, 1],
  }[event.key];
  const item = d.state?.controls.find((c) => c.id === d.selectedId);
  if (
    delta &&
    item &&
    !item.locked &&
    item.id !== d.rootId &&
    !item.component &&
    !managedKinds.has(
      d.state?.controls.find((c) => c.id === item.parentId)?.kind,
    )
  ) {
    event.preventDefault();
    const step = event.shiftKey ? 10 : 1;
    send("move", item.id, {
      x: item.x + delta[0] * step,
      y: item.y + delta[1] * step,
    });
  }
});
