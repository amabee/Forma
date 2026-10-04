import { parse } from "acorn";
import { formatCode } from "./code-format.mjs";

const common = ["text", "enabled", "visible"];
const images = ["image", "picturebox", "avatar"];
const numeric = ["numericupdown", "slider", "progressbar", "circularprogress"];
const dates = ["datepicker", "timepicker", "datetimepicker", "colorpicker"];
export const eventNames = [
  "Click", "DoubleClick", "MouseDown", "MouseUp", "MouseMove", "MouseEnter", "MouseLeave", "MouseOver", "MouseOut", "MouseWheel",
  "KeyDown", "KeyUp", "KeyPress", "Input", "Change", "BeforeInput", "Focus", "Blur", "FocusIn", "FocusOut",
  "Load", "Ready", "Created", "Mounted", "Updated", "Destroyed", "Resize", "Move", "Layout",
  "DragStart", "Drag", "DragEnd", "DragEnter", "DragOver", "DragLeave", "Drop", "Copy", "Cut", "Paste",
  "Validating", "Validated", "Submit", "Reset", "tick", "row-selection"
];
export function propertiesFor(kind, method) {
  const keys = [...common];
  if (images.includes(kind)) keys.push("source");
  if (["checkbox", "radiobutton", "toggleswitch", "togglebutton"].includes(kind)) keys.push("checked");
  if (numeric.includes(kind) || dates.includes(kind) || method === "get" && ["textbox", "searchbox", "passwordbox", "textarea", "maskedtextbox"].includes(kind)) keys.push("value");
  if (["combobox", "listbox"].includes(kind) || method === "set" && kind === "listview") keys.push("selectedIndex");
  if (kind === "tabcontrol") keys.push("selectedTab");
  if (kind === "datagridview") keys.push("selectedRow", "filterText", "sortColumn", "sortDirection");
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
  showToast: "showToast(name)", closeToast: "closeToast(name)", showDialog: "showDialog(name)",
};
export function formaCompletions(context, controls = [], customValues = {}) {
  const prefix = context.state.doc.sliceString(0, context.pos);
  const result = (from, options) => ({ from, options, validFor: /^[\w$-]*$/ });
  const call = /api\.(get|set|find|showToast|closeToast|showDialog)\(\s*(["'])([^"']*)$/.exec(prefix);
  if (call) {
    const kind = call[1].includes("Toast") ? "toast" : call[1] === "showDialog" ? "dialog" : null;
    return result(context.pos - call[3].length, controls.filter(c => c.name && (!kind || (kind === "dialog" ? ["dialog", "confirmationdialog"].includes(c.kind) : c.kind === kind)))
      .map(c => ({ label: c.name, type: "variable", detail: c.kind })));
  }
  const prop = /api\.(get|set)\(\s*["']([^"']+)["']\s*,\s*["']([^"']*)$/.exec(prefix);
  if (prop) {
    const control = controls.find(c => c.name === prop[2] || c.id === prop[2]);
    return control ? result(context.pos - prop[3].length, propertiesFor(control.kind, prop[1]).map(label => ({ label, type: "property", detail: `${control.kind} · api.${prop[1]}` }))) : null;
  }
  const api = /\bapi\.([\w$]*)$/.exec(prefix);
  if (api) return result(context.pos - api[1].length, Object.entries(methods).map(([label, detail]) => ({ label, detail, type: "function" })));
  const custom = /\bcomponent\.(?:properties|characteristics)\.([\w$]*)$/.exec(prefix);
  if (custom) return result(context.pos - custom[1].length, Object.keys(customValues).map(label => ({ label, type: "property", detail: "Custom value" })));
  const component = /\bcomponent\.([\w$]*)$/.exec(prefix);
  if (component) return result(context.pos - component[1].length, ["id", "name", "element", "properties"].map(label => ({ label, type: "property" })));
  const event = /api\.on\(\s*["']([^"']*)$/.exec(prefix);
  if (event) return result(context.pos - event[1].length, [...eventNames, "click", "input", "change", "keydown", "focus", "blur"].map(label => ({ label, type: "text" })));
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
    if (node.type === "CallExpression" && node.callee.type === "MemberExpression" && node.callee.object.name === "api" && !node.callee.computed) {
      const method = node.callee.property.name, args = node.arguments;
      const warn = (arg, message) => diagnostics.push({ from: arg.start, to: arg.end, severity: "warning", message });
      if (!methods[method]) warn(node.callee.property, `Unknown Forma API method '${method}'.`);
      if (["get", "set", "find", "showToast", "closeToast", "showDialog"].includes(method) && args[0]?.type === "Literal" && typeof args[0].value === "string" && controls.length) {
        const control = controls.find(c => c.name === args[0].value || c.id === args[0].value);
        if (!control) warn(args[0], `Component '${args[0].value}' does not exist in this form.`);
        else if (["get", "set"].includes(method) && args[1]?.type === "Literal" && !propertiesFor(control.kind, method).includes(args[1].value))
          warn(args[1], `'${args[1].value}' is not supported by api.${method} for ${control.kind}.`);
      }
    }
    for (const [key, value] of Object.entries(node)) if (key !== "start" && key !== "end") {
      if (Array.isArray(value)) value.forEach(walk); else if (value && typeof value === "object") walk(value);
    }
  }
  walk(ast); return diagnostics;
}
