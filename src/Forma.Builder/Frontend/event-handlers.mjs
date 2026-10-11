import { parse } from "acorn";

export const eventGroups = {
  Lifecycle: ["Created", "Mounted", "Ready", "Load", "Updated", "Destroyed"],
  Mouse: ["Click", "DoubleClick", "MouseDown", "MouseUp", "MouseMove", "MouseEnter", "MouseLeave", "MouseOver", "MouseOut", "MouseWheel"],
  Keyboard: ["KeyDown", "KeyUp", "KeyPress"],
  Focus: ["Focus", "Blur", "FocusIn", "FocusOut"],
  Input: ["Input", "Change", "BeforeInput", "Validating", "Validated"],
  Layout: ["Resize", "Move", "Layout"],
  "Drag and drop": ["DragStart", "Drag", "DragEnd", "DragEnter", "DragOver", "DragLeave", "Drop"],
  Clipboard: ["Copy", "Cut", "Paste"],
  Form: ["Submit", "Reset"],
  State: ["PropertyChanged"],
};
export const normalizeEvent = name => ({ doubleclick: "dblclick", mousewheel: "wheel" })[String(name).toLowerCase()] ?? String(name).toLowerCase();
const special = { timer: ["Tick"], datagridview: ["row-selection"], chip: ["chip-remove"], menustrip: ["command-item"], contextmenu: ["command-item"], contextmenustrip: ["command-item"], dropdownbutton: ["command-item"], splitbutton: ["primary-click", "command-item"], breadcrumb: ["navigate"], sidenavigation: ["navigate"] };
export function eventsFor(control) {
  const groups = control.component ? { Lifecycle: eventGroups.Lifecycle, State: eventGroups.State } : { ...eventGroups };
  if (special[control.kind]) groups.Component = special[control.kind];
  return groups;
}
function walk(node, visit) {
  if (!node || typeof node !== "object") return;
  if (node.type) visit(node);
  for (const [key, value] of Object.entries(node)) {
    if (key === "start" || key === "end") continue;
    if (Array.isArray(value)) value.forEach(child => walk(child, visit));
    else if (value && typeof value === "object") walk(value, visit);
  }
}
export function inspectHandlers(source) {
  const handlers = [], identifiers = new Set(), functions = new Map();
  try {
    const ast = parse(source, { ecmaVersion: "latest", sourceType: "module" });
    walk(ast, node => {
      if (node.type === "Identifier") identifiers.add(node.name);
      if (node.type === "FunctionDeclaration" && node.id) functions.set(node.id.name, node);
      if (node.type === "VariableDeclarator" && node.id.type === "Identifier" && ["ArrowFunctionExpression", "FunctionExpression"].includes(node.init?.type)) functions.set(node.id.name, node.init);
      if (node.type !== "CallExpression" || node.callee.type !== "MemberExpression") return;
      const callee = node.callee;
      const method = callee.computed ? callee.property.value : callee.property.name;
      if (!["forma", "api"].includes(callee.object.name) || method !== "on" || typeof node.arguments[0]?.value !== "string") return;
      const callback = node.arguments[1];
      const member = callback?.type === "MemberExpression" && callback.object.type === "Identifier" && !callback.computed ? { owner: callback.object.name, method: callback.property.name } : null;
      handlers.push({ event: node.arguments[0].value, key: normalizeEvent(node.arguments[0].value), from: callback?.start ?? node.start, callback, member, name: member ? `${member.owner}.${member.method}` : callback?.type === "Identifier" ? callback.name : "Inline handler" });
    });
    for (const handler of handlers) {
      const fn = handler.callback?.type === "Identifier" ? functions.get(handler.callback.name) : handler.callback;
      if (fn?.body?.type === "BlockStatement") handler.from = fn.body.start + 1;
      delete handler.callback;
    }
    return { handlers, identifiers, error: null };
  } catch (error) { return { handlers, identifiers, error: error.message }; }
}
export function prepareHandler(source, event, componentName, events = "", handlerClass = "ButtonEventHandler") {
  const inspected = inspectHandlers(source);
  if (inspected.error) return { error: `script.js: ${inspected.error}` };
  const existing = inspected.handlers.find(handler => handler.key === normalizeEvent(event));
  try {
    const scriptAst = parse(source, { ecmaVersion: "latest", sourceType: "module" });
    const eventAst = parse(events, { ecmaVersion: "latest", sourceType: "module" });
    const imported = scriptAst.body.filter(node => node.type === "ImportDeclaration" && ["./event.js", "./event"].includes(node.source.value))
      .flatMap(node => node.specifiers).find(specifier => specifier.type === "ImportSpecifier" && specifier.imported.name === handlerClass);
    const declaration = eventAst.body.find(node => node.type === "ExportNamedDeclaration" && node.declaration?.type === "ClassDeclaration" && node.declaration.id.name === handlerClass)?.declaration;
    if (existing) {
      const method = existing.member?.owner === imported?.local.name ? declaration?.body.body.find(node => node.static && node.key.name === existing.member.method) : null;
      return { source, events, document: method ? "events" : "behavior", from: method ? method.value.body.start + 1 : existing.from, created: false };
    }
    if (events.trim() && !declaration) return { error: `event.js must export class ${handlerClass} before handlers can be added.` };
    let alias = imported?.local.name ?? handlerClass, suffix = 2;
    if (!imported) while (inspected.identifiers.has(alias)) alias = `${handlerClass}${suffix++}`;
    const stem = `${componentName || "component"}_${event}`.replace(/[^\w$]/g, "_");
    const base = /^[$A-Z_a-z]/.test(stem) ? stem : `_${stem}`;
    let name = base; suffix = 2;
    const methodNames = new Set(declaration?.body.body.map(node => node.key?.name ?? node.key?.value) ?? []);
    while (methodNames.has(name)) name = `${base}${suffix++}`;
    const eventType = `FormaEvent<${JSON.stringify(event)}>`;
    const method = `\n  /** @param {${eventType}} event */\n  static ${name}(event) {\n    // Add your code here.\n  }\n`;
    let eventChange;
    if (declaration) eventChange = { from: declaration.body.end - 1, insert: method };
    else eventChange = { from: events.length, insert: `${events && !events.endsWith("\n") ? "\n" : ""}export class ${handlerClass} {${method}}\n` };
    const nextEvents = events.slice(0, eventChange.from) + eventChange.insert + events.slice(eventChange.from);
    const changes = [];
    if (!imported) {
      const offset = source.startsWith("#!") ? (source.indexOf("\n") < 0 ? source.length : source.indexOf("\n") + 1) : 0;
      changes.push({ from: offset, insert: `${offset === source.length && offset ? "\n" : ""}import { ${handlerClass}${alias === handlerClass ? "" : ` as ${alias}`} } from "./event.js";\n` });
    }
    const subscription = `${source && !source.endsWith("\n") ? "\n\n" : "\n"}forma.on(${JSON.stringify(event)}, ${alias}.${name});\n`;
    // Combine insertions if the script is empty (both are at offset zero).
    if (changes[0]?.from === source.length) changes[0].insert += subscription;
    else changes.push({ from: source.length, insert: subscription });
    let nextSource = source;
    for (const change of [...changes].reverse()) nextSource = nextSource.slice(0, change.from) + change.insert + nextSource.slice(change.from);
    return { source: nextSource, events: nextEvents, scriptChanges: changes, eventChange, document: "events", from: eventChange.from + eventChange.insert.indexOf("// Add"), created: true };
  } catch (error) { return { error: `event.js: ${error.message}` }; }
}
