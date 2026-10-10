import { parse } from "acorn";
import { formatCode } from "./code-format.mjs";

const common = ["text", "enabled", "visible"];
const images = ["image", "picturebox", "avatar"];
const numeric = ["numericupdown", "slider", "progressbar", "circularprogress", "rating"];
const dates = ["datepicker", "timepicker", "datetimepicker", "colorpicker"];
export const eventNames = [
  "Click", "DoubleClick", "MouseDown", "MouseUp", "MouseMove", "MouseEnter", "MouseLeave", "MouseOver", "MouseOut", "MouseWheel",
  "KeyDown", "KeyUp", "KeyPress", "Input", "Change", "BeforeInput", "Focus", "Blur", "FocusIn", "FocusOut",
  "Load", "Ready", "Created", "Mounted", "Updated", "Destroyed", "Resize", "Move", "Layout",
  "DragStart", "Drag", "DragEnd", "DragEnter", "DragOver", "DragLeave", "Drop", "Copy", "Cut", "Paste",
  "Validating", "Validated", "Submit", "Reset", "tick", "row-selection", "chip-remove", "command-item", "primary-click", "navigate"
];
export function propertiesFor(kind, method) {
  const keys = [...common];
  if (kind === "timer") keys.push("interval");
  if (numeric.includes(kind)) keys.push("minimum", "maximum", "increment");
  if (kind === "spinner") keys.push("speed");
  if (kind === "skeleton") keys.push("shape", "lines", "isActive");
  if (!["form", "timer", "backgroundworker", "toast", "loadingoverlay", "tooltip", "contextmenu", "contextmenustrip", "dialog", "confirmationdialog"].includes(kind)) keys.push("dock", "anchor");
  if (images.includes(kind)) keys.push("source");
  if (["checkbox", "radiobutton", "toggleswitch", "togglebutton", "chip"].includes(kind)) keys.push("checked");
  if (numeric.includes(kind) || dates.includes(kind) || method === "get" && ["textbox", "searchbox", "passwordbox", "textarea", "maskedtextbox"].includes(kind)) keys.push("value");
  if (["combobox", "listbox", "radiogroup", "segmentedcontrol", "buttongroup", "breadcrumb", "sidenavigation"].includes(kind) || method === "set" && kind === "listview") keys.push("selectedIndex");
  if (["combobox", "listbox", "listview", "checkedlistbox", "radiogroup", "checkboxgroup", "segmentedcontrol", "chipgroup", "buttongroup", "breadcrumb", "sidenavigation"].includes(kind)) keys.push("items");
  if (["checkedlistbox", "checkboxgroup", "chipgroup"].includes(kind)) keys.push("checkedIndices");
  if (kind === "rating") keys.push("readOnly");
  if (kind === "chip") keys.push("variant", "removable", "isRemoved");
  if (["iconbutton", "floatingactionbutton", "commandbutton"].includes(kind)) keys.push("iconName", "showText");
  if (["menustrip", "toolbar", "toolstrip", "contextmenu", "contextmenustrip", "dropdownbutton", "splitbutton"].includes(kind)) keys.push("commandItems");
  if (kind === "splitbutton") keys.push("primaryEnabled");
  if (kind === "commandbutton") keys.push("description");
  if (["accordion", "sidebar", "appshell", "responsivepanel", "stackpanel", "hstack", "vstack", "wrappanel", "centerpanel", "scrollablepanel", "flowlayoutpanel", "tablelayoutpanel", "tabcontrol", "splitcontainer"].includes(kind)) keys.push("orientation", "gap");
  if (["sidenavigation", "radiogroup", "segmentedcontrol", "buttongroup"].includes(kind)) keys.push("orientation");
  if (["appshell", "responsivepanel"].includes(kind)) keys.push("breakpoint");
  if (kind === "accordion") keys.push("tabs", "selectedTab", "expanded");
  if (kind === "scrollablepanel") keys.push("scrollDirection");
  if (kind === "tabcontrol") keys.push("tabs", "selectedTab");
  if (kind === "datagridview") keys.push("columns", "rows", "readOnly", "sortingEnabled", "filteringEnabled", "selectedRow", "filterText", "sortColumn", "sortDirection");
  if (kind === "toast") { keys.push("variant", "position", "duration", "dismissible"); if (method === "get") keys.push("isOpen"); }
  if (["spinner", "loadingoverlay"].includes(kind)) keys.push("isActive");
  if (method === "get" && ["filepicker", "folderpicker"].includes(kind)) keys.push("selectedPath");
  return keys;
}
const methods = {
  on: "on(event, callback) — listen to this component's events",
  get: "get(name, property) — read a supported runtime property",
  set: "set(name, property, value) — change a supported runtime property",
  find: "find(name) — get the component DOM element",
  cleanup: "cleanup(callback) — dispose listeners and timers",
  validate: "validate() — run cancellable validation and check HTML input constraints",
  submit: "submit() — validate and raise Submit; returns whether it was accepted",
  reset: "reset() — raise cancellable Reset for your reset handler",
  showToast: "showToast(name, { text, variant, position, duration, dismissible })", closeToast: "closeToast(name)", showDialog: "showDialog(name)",
  addRow: "addRow(gridName, stringCells) — append a row",
  updateRow: "updateRow(gridName, rowIndex, stringCells) — replace a source row",
  removeRow: "removeRow(gridName, rowIndex) — remove a source row",
  clearRows: "clearRows(gridName) — remove all rows",
  setCell: "setCell(gridName, rowIndex, columnIndex, stringValue) — edit one cell",
  bind: "bind(name, property) — a live control reference; read/write .value or subscribe",
  ref: "ref(initialValue) — local reactive value with .value and subscribe",
  reactive: "reactive(object) — shallow reactive state; track property assignments",
  computed: "computed(() => expression) — read-only derived .value",
  effect: "effect(callback) — run now and when reactive dependencies change; returns stop",
  watch: "watch(refOrGetter, callback, options) — observe changes; returns stop",
};
const gridMethods = ["addRow", "updateRow", "removeRow", "clearRows", "setCell"];
export function formaCompletions(context, controls = [], customValues = {}) {
  const prefix = context.state.doc.sliceString(0, context.pos);
  const result = (from, options) => ({ from, options, validFor: /^[\w$-]*$/ });
  const layoutValue = /(?:forma|api)\.set\(\s*["'][^"']+["']\s*,\s*["'](dock|anchor)["']\s*,\s*["']([^"']*)$/.exec(prefix);
  if (layoutValue) return result(context.pos - layoutValue[2].length,
    (layoutValue[1] === "dock" ? ["none", "top", "bottom", "left", "right", "fill"]
      : ["none", "top,left", "top,right", "bottom,left", "bottom,right", "top,left,right", "bottom,left,right", "top,bottom,left", "top,bottom,right", "top,bottom,left,right", "top", "bottom", "left", "right", "top,bottom", "left,right"])
    .map(label => ({ label, type: "text" })));
  const variant = /(?:forma|api)\.set\(\s*["'][^"']+["']\s*,\s*["']variant["']\s*,\s*["']([^"']*)$/.exec(prefix)
    ?? /(?:forma|api)\.showToast\(\s*["'][^"']+["']\s*,\s*\{[^}]*?\bvariant\s*:\s*["']([^"']*)$/.exec(prefix);
  if (variant) return result(context.pos - variant[1].length, ["neutral", "info", "success", "warning", "caution", "error", "danger"].map(label => ({ label, type: "text" })));
  const call = /(?:forma|api)\.(get|set|bind|find|showToast|closeToast|showDialog|addRow|updateRow|removeRow|clearRows|setCell)\(\s*(["'])([^"']*)$/.exec(prefix);
  if (call) {
    const kind = gridMethods.includes(call[1]) ? "datagridview" : call[1].includes("Toast") ? "toast" : call[1] === "showDialog" ? "dialog" : null;
    return result(context.pos - call[3].length, controls.filter(c => c.name && (!kind || (kind === "dialog" ? ["dialog", "confirmationdialog"].includes(c.kind) : c.kind === kind)))
      .map(c => ({ label: c.name, type: "variable", detail: c.kind })));
  }
  const prop = /(?:forma|api)\.(get|set|bind)\(\s*["']([^"']+)["']\s*,\s*["']([^"']*)$/.exec(prefix);
  if (prop) {
    const control = controls.find(c => c.name === prop[2] || c.id === prop[2]);
    return control ? result(context.pos - prop[3].length, propertiesFor(control.kind, prop[1] === "bind" ? "get" : prop[1]).map(label => ({ label, type: "property", detail: `${control.kind} · forma.${prop[1]}` }))) : null;
  }
  const api = /\b(?:forma|api)\.([\w$]*)$/.exec(prefix);
  if (api) return result(context.pos - api[1].length, Object.entries(methods).map(([label, detail]) => ({ label, detail, type: "function" })));
  const custom = /\bcomponent\.(?:properties|characteristics)\.([\w$]*)$/.exec(prefix);
  if (custom) return result(context.pos - custom[1].length, Object.keys(customValues).map(label => ({ label, type: "property", detail: "Custom value" })));
  const component = /\bcomponent\.([\w$]*)$/.exec(prefix);
  if (component) return result(context.pos - component[1].length, ["id", "name", "element", "properties"].map(label => ({ label, type: "property" })));
  const event = /(?:forma|api)\.on\(\s*["']([^"']*)$/.exec(prefix);
  if (event) return result(context.pos - event[1].length, [...eventNames, "click", "input", "change", "keydown", "focus", "blur"].map(label => ({ label, type: "text" })));
  const member = /\b([\w$]+)\.([\w$]*)$/.exec(prefix);
  if (member) {
    const name = member[1].replace(/\$/g, "\\$");
    const declaration = new RegExp(`\\b(?:const|let|var)\\s+${name}\\s*=\\s*(?:forma|api)\\.(bind|ref|computed)\\(`).exec(prefix);
    if (declaration) return result(context.pos - member[2].length, [
      { label: "value", type: "property", detail: declaration[1] === "computed" ? "Read-only derived value" : "Current reactive value" },
      { label: "subscribe", type: "function", detail: "subscribe(callback, { immediate }) — returns unsubscribe" }
    ]);
  }
  return null;
}
function diagnostic(source, error) {
  const start = error.loc?.start ?? error.loc;
  let pos = error.pos ?? 0;
  if (start?.line) pos = source.split("\n").slice(0, start.line - 1).reduce((sum, line) => sum + line.length + 1, 0) + (start.column ?? 0);
  const offset = /position (\d+)/.exec(error.message);
  if (offset) pos = Number(offset[1]);
  pos = Math.min(pos, source.length);
  return { from: pos, to: Math.min(source.length, pos + 1), severity: "error", message: error.message.split("\n")[0] };
}
export async function diagnose(language, source, controls = []) {
  let ast;
  try {
    if (language === "javascript") ast = parse(source, { ecmaVersion: "latest", sourceType: "script", allowReturnOutsideFunction: true });
    else if (language === "json") {
      const value = JSON.parse(source);
      if (!value || Array.isArray(value) || typeof value !== "object") throw new Error("Custom values must be a JSON object.");
    } else if (source.trim()) await formatCode("css", source);
  } catch (error) { return [diagnostic(source, error)]; }
  if (!ast) return [];
  const diagnostics = [];
  function walk(node) {
    if (!node || typeof node !== "object") return;
    if (node.type === "CallExpression" && node.callee.type === "MemberExpression" && ["forma", "api"].includes(node.callee.object.name) && !node.callee.computed) {
      const method = node.callee.property.name, args = node.arguments;
      const warn = (arg, message) => diagnostics.push({ from: arg.start, to: arg.end, severity: "warning", message });
      if (!methods[method]) warn(node.callee.property, `Unknown Forma API method '${method}'.`);
      if (["get", "set", "bind", "find", "showToast", "closeToast", "showDialog", ...gridMethods].includes(method) && args[0]?.type === "Literal" && typeof args[0].value === "string" && controls.length) {
        const control = controls.find(c => c.name === args[0].value || c.id === args[0].value);
        if (!control) warn(args[0], `Component '${args[0].value}' does not exist in this form.`);
        else if (gridMethods.includes(method) && control.kind !== "datagridview") warn(args[0], `'${method}' requires a DataGridView.`);
        else if (["get", "set", "bind"].includes(method) && args[1]?.type === "Literal" && !propertiesFor(control.kind, method === "bind" ? "get" : method).includes(args[1].value))
          warn(args[1], `'${args[1].value}' is not supported by ${node.callee.object.name}.${method} for ${control.kind}.`);
        else if (method === "set" && control.kind === "timer" && args[1]?.value === "interval" && args[2]?.type === "Literal" && !Number.isInteger(args[2].value))
          warn(args[2], "Timer interval must be an integer number of milliseconds, for example 1000 without quotes.");
      }
    }
    for (const [key, value] of Object.entries(node)) if (key !== "start" && key !== "end") {
      if (Array.isArray(value)) value.forEach(walk); else if (value && typeof value === "object") walk(value);
    }
  }
  walk(ast); return diagnostics;
}
