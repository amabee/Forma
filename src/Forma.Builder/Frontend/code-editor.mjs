import { EditorView, basicSetup } from "codemirror";
import { EditorState, Compartment, StateField, StateEffect } from "@codemirror/state";
import { keymap, hoverTooltip, showTooltip } from "@codemirror/view";
import { indentWithTab } from "@codemirror/commands";
import { javascript, localCompletionSource } from "@codemirror/lang-javascript";
import { css } from "@codemirror/lang-css";
import { json } from "@codemirror/lang-json";
import { HighlightStyle, syntaxHighlighting } from "@codemirror/language";
import { tags } from "@lezer/highlight";
import { formatCode } from "./code-format.mjs";
import { autocompletion } from "@codemirror/autocomplete";
import { linter, lintGutter, openLintPanel, forEachDiagnostic, forceLinting } from "@codemirror/lint";
import { syntaxTree } from "@codemirror/language";
import { diagnose, formaCompletions } from "./code-assistance.mjs";
import { createJavaScriptCompletionClient } from "./javascript-completion.mjs";
const semanticCompletion = createJavaScriptCompletionClient();
let completionContext = { controls: [] };
document.body.classList.toggle("embedded-editor", new URLSearchParams(window.location.search).has("embedded"));

const definitions = { css: { title: "CSS", extension: css() }, behavior: { title: "JavaScript", extension: javascript() }, characteristics: { title: "JSON", extension: json() } };
const language = key => ({ css: "css", behavior: "javascript", characteristics: "json" })[key];
const send = message => window.chrome?.webview?.postMessage({ type: "editor", ...message });
const status = text => { document.getElementById("editor-message").textContent = text; };
// VS Code "Dark Modern" / "Light Modern" workbench colors with Dark+ / Light+ token colors.
const editorBase = {
  ".cm-scroller": { fontFamily: "Consolas, 'Cascadia Mono', 'Courier New', monospace", overflow: "auto", lineHeight: "19px" },
  ".cm-content": { padding: "4px 0 40vh" },
  ".cm-gutters": { border: "none" },
  ".cm-lineNumbers .cm-gutterElement": { padding: "0 10px 0 18px", minWidth: "40px" },
  ".cm-foldGutter .cm-gutterElement": { padding: "0 4px", opacity: "0", transition: "opacity .15s" },
  ".cm-gutters:hover .cm-foldGutter .cm-gutterElement": { opacity: "1" },
  ".cm-line": { padding: "0 0 0 6px" },
  ".cm-tooltip": { borderRadius: "4px", boxShadow: "0 2px 8px rgba(0, 0, 0, 0.36)" },
  ".cm-tooltip.cm-tooltip-autocomplete > ul": { fontFamily: "Consolas, 'Cascadia Mono', monospace", fontSize: "13px", maxHeight: "16em" },
  ".cm-tooltip.cm-tooltip-autocomplete > ul > li": { padding: "2px 8px", lineHeight: "20px" },
  ".cm-completionIcon": { width: "1.2em", opacity: "0.85" },
  ".cm-completionDetail": { marginLeft: "1em", fontStyle: "normal", opacity: "0.7" },
  ".cm-panels": { fontFamily: "'Segoe UI', system-ui, sans-serif", fontSize: "13px" },
  ".cm-panel.cm-search": { padding: "6px 8px" },
  ".cm-panel.cm-search input, .cm-panel.cm-search button": { borderRadius: "2px", fontSize: "13px" },
  ".cm-panel.cm-search label": { fontSize: "12px" },
  ".cm-panel.cm-panel-lint ul": { maxHeight: "160px" },
  ".cm-panel.cm-panel-lint ul > li": { padding: "2px 12px" },
  ".cm-diagnostic": { padding: "4px 8px" },
};
const theme = EditorView.theme({
  ...editorBase,
  "&": { height: "100%", color: "#cccccc", backgroundColor: "#1f1f1f", fontSize: "14px" },
  ".cm-content": { ...editorBase[".cm-content"], caretColor: "#aeafad" },
  ".cm-cursor, .cm-dropCursor": { borderLeftColor: "#aeafad", borderLeftWidth: "2px" },
  ".cm-gutters": { backgroundColor: "#1f1f1f", color: "#6e7681", border: "none" },
  // CodeMirror draws selections behind the text, so the current line is a border, not a fill.
  ".cm-activeLine": { backgroundColor: "transparent", boxShadow: "inset 0 0 0 2px #282828" },
  ".cm-activeLineGutter": { backgroundColor: "transparent", color: "#cccccc" },
  ".cm-selectionBackground": { backgroundColor: "#3a3d41" },
  "&.cm-focused > .cm-scroller > .cm-selectionLayer .cm-selectionBackground": { backgroundColor: "#264f78" },
  ".cm-selectionMatch": { backgroundColor: "rgba(173, 214, 255, 0.15)" },
  "&.cm-focused .cm-matchingBracket": { backgroundColor: "rgba(0, 100, 0, 0.1)", outline: "1px solid #888888" },
  ".cm-searchMatch": { backgroundColor: "rgba(234, 92, 0, 0.33)" },
  ".cm-searchMatch.cm-searchMatch-selected": { backgroundColor: "#9e6a03" },
  ".cm-foldPlaceholder": { backgroundColor: "rgba(255, 255, 255, 0.08)", border: "none", color: "#cccccc" },
  ".cm-panels": { ...editorBase[".cm-panels"], backgroundColor: "#181818", color: "#cccccc" },
  ".cm-panels.cm-panels-top": { borderBottom: "1px solid #2b2b2b" },
  ".cm-panels.cm-panels-bottom": { borderTop: "1px solid #2b2b2b" },
  ".cm-panel.cm-search input": { ...editorBase[".cm-panel.cm-search input, .cm-panel.cm-search button"], backgroundColor: "#313131", color: "#cccccc", border: "1px solid #3c3c3c" },
  ".cm-panel.cm-search button": { ...editorBase[".cm-panel.cm-search input, .cm-panel.cm-search button"], backgroundColor: "#313131", backgroundImage: "none", color: "#cccccc", border: "1px solid #3c3c3c" },
  ".cm-panel.cm-panel-lint ul [aria-selected]": { backgroundColor: "#04395e", color: "#ffffff" },
  ".cm-tooltip": { ...editorBase[".cm-tooltip"], backgroundColor: "#202020", color: "#cccccc", border: "1px solid #454545" },
  ".cm-tooltip-autocomplete ul li[aria-selected]": { backgroundColor: "#04395e", color: "#ffffff" },
  ".cm-completionMatchedText": { color: "#2aaaff", textDecoration: "none", fontWeight: "600" },
  ".cm-tooltip .cm-tooltip-arrow:before": { borderTopColor: "#454545", borderBottomColor: "#454545" },
  ".cm-tooltip .cm-tooltip-arrow:after": { borderTopColor: "#202020", borderBottomColor: "#202020" },
}, { dark: true });
const highlighting = syntaxHighlighting(HighlightStyle.define([
  { tag: [tags.keyword, tags.modifier, tags.bool, tags.null, tags.atom, tags.self], color: "#569cd6" },
  { tag: [tags.controlKeyword, tags.moduleKeyword, tags.operatorKeyword], color: "#c586c0" },
  { tag: [tags.string, tags.special(tags.string), tags.regexp], color: "#ce9178" },
  { tag: [tags.number, tags.unit], color: "#b5cea8" },
  { tag: tags.comment, color: "#6a9955" },
  { tag: [tags.function(tags.variableName), tags.function(tags.propertyName)], color: "#dcdcaa" },
  { tag: [tags.variableName, tags.propertyName, tags.attributeName], color: "#9cdcfe" },
  { tag: [tags.definition(tags.variableName), tags.local(tags.variableName)], color: "#9cdcfe" },
  { tag: [tags.typeName, tags.className, tags.namespace], color: "#4ec9b0" },
  { tag: [tags.tagName, tags.heading], color: "#569cd6" },
  { tag: [tags.constant(tags.variableName), tags.color], color: "#4fc1ff" },
  { tag: [tags.labelName, tags.special(tags.variableName)], color: "#d7ba7d" },
  { tag: [tags.operator, tags.punctuation, tags.separator], color: "#d4d4d4" },
  { tag: tags.invalid, color: "#f44747" },
]));
const lightTheme = EditorView.theme({
  ...editorBase,
  "&": { height: "100%", color: "#3b3b3b", backgroundColor: "#ffffff", fontSize: "14px" },
  ".cm-content": { ...editorBase[".cm-content"], caretColor: "#000000" },
  ".cm-cursor, .cm-dropCursor": { borderLeftColor: "#000000", borderLeftWidth: "2px" },
  ".cm-gutters": { backgroundColor: "#ffffff", color: "#6e7681", border: "none" },
  ".cm-activeLine": { backgroundColor: "transparent", boxShadow: "inset 0 0 0 2px #eeeeee" },
  ".cm-activeLineGutter": { backgroundColor: "transparent", color: "#171184" },
  ".cm-selectionBackground": { backgroundColor: "#e5ebf1" },
  "&.cm-focused > .cm-scroller > .cm-selectionLayer .cm-selectionBackground": { backgroundColor: "#add6ff" },
  ".cm-selectionMatch": { backgroundColor: "rgba(173, 214, 255, 0.5)" },
  "&.cm-focused .cm-matchingBracket": { backgroundColor: "rgba(0, 100, 0, 0.1)", outline: "1px solid #b9b9b9" },
  ".cm-searchMatch": { backgroundColor: "rgba(234, 92, 0, 0.33)" },
  ".cm-searchMatch.cm-searchMatch-selected": { backgroundColor: "#a8ac94" },
  ".cm-foldPlaceholder": { backgroundColor: "rgba(0, 0, 0, 0.06)", border: "none", color: "#3b3b3b" },
  ".cm-panels": { ...editorBase[".cm-panels"], backgroundColor: "#f8f8f8", color: "#3b3b3b" },
  ".cm-panels.cm-panels-top": { borderBottom: "1px solid #e5e5e5" },
  ".cm-panels.cm-panels-bottom": { borderTop: "1px solid #e5e5e5" },
  ".cm-panel.cm-search input": { ...editorBase[".cm-panel.cm-search input, .cm-panel.cm-search button"], backgroundColor: "#ffffff", color: "#3b3b3b", border: "1px solid #cecece" },
  ".cm-panel.cm-search button": { ...editorBase[".cm-panel.cm-search input, .cm-panel.cm-search button"], backgroundColor: "#f8f8f8", backgroundImage: "none", color: "#3b3b3b", border: "1px solid #cecece" },
  ".cm-panel.cm-panel-lint ul [aria-selected]": { backgroundColor: "#e8e8e8", color: "#000000" },
  ".cm-tooltip": { ...editorBase[".cm-tooltip"], backgroundColor: "#f8f8f8", color: "#3b3b3b", border: "1px solid #c8c8c8", boxShadow: "0 2px 8px rgba(0, 0, 0, 0.16)" },
  ".cm-tooltip-autocomplete ul li[aria-selected]": { backgroundColor: "#e8e8e8", color: "#000000" },
  ".cm-completionMatchedText": { color: "#0066bf", textDecoration: "none", fontWeight: "600" },
  ".cm-tooltip .cm-tooltip-arrow:before": { borderTopColor: "#c8c8c8", borderBottomColor: "#c8c8c8" },
  ".cm-tooltip .cm-tooltip-arrow:after": { borderTopColor: "#f8f8f8", borderBottomColor: "#f8f8f8" },
}, { dark: false });
const lightHighlighting = syntaxHighlighting(HighlightStyle.define([
  { tag: [tags.keyword, tags.modifier, tags.bool, tags.null, tags.atom, tags.self], color: "#0000ff" },
  { tag: [tags.controlKeyword, tags.moduleKeyword, tags.operatorKeyword], color: "#af00db" },
  { tag: [tags.string, tags.special(tags.string), tags.regexp], color: "#a31515" },
  { tag: [tags.number, tags.unit], color: "#098658" },
  { tag: tags.comment, color: "#008000" },
  { tag: [tags.function(tags.variableName), tags.function(tags.propertyName)], color: "#795e26" },
  { tag: [tags.variableName, tags.propertyName, tags.attributeName], color: "#001080" },
  { tag: [tags.definition(tags.variableName), tags.local(tags.variableName)], color: "#001080" },
  { tag: [tags.typeName, tags.className, tags.namespace], color: "#267f99" },
  { tag: [tags.tagName, tags.heading], color: "#800000" },
  { tag: [tags.constant(tags.variableName), tags.color], color: "#0070c1" },
  { tag: [tags.labelName, tags.special(tags.variableName)], color: "#800000" },
  { tag: [tags.operator, tags.punctuation, tags.separator], color: "#3b3b3b" },
  { tag: tags.invalid, color: "#cd3131" },
]));
const themeSlot = new Compartment(); let editorTheme = "dark";
const themeExtensions = () => editorTheme === "light" ? [lightTheme, lightHighlighting] : [theme, highlighting];
const savedSources = {};
let globalDocument = false, singleDocument = null, singleFileName = null;
const fileName = key => singleFileName ?? ({ css: "component.css", behavior: globalDocument ? "main.js" : "script.js", characteristics: "custom-properties.json" })[key];
const views = new Map(); let active = "css", busy = false, dirty = false, populating = false;
function source() { if (singleDocument) return { css: "", behavior: views.get(singleDocument).state.doc.toString(), characteristics: "{}" }; return Object.fromEntries([...views].map(([key, view]) => [key, view.state.doc.toString()])); }
function markDirty(value) {
  for (const [key, view] of views) {
    const tab = document.querySelector(`[data-tab="${key}"]`);
    const changed = singleDocument ? key === singleDocument && view.state.doc.toString() !== (savedSources.behavior ?? "") : view.state.doc.toString() !== (savedSources[key] ?? "");
    tab.querySelector(".file-dirty").hidden = !changed;
    tab.setAttribute("aria-label", `${fileName(key)}${changed ? ", unsaved changes" : ""}`);
  }
  if (dirty === value) return;
  dirty = value; document.getElementById("editor-dirty").textContent = dirty ? "Unsaved changes" : "Saved";
  send({ event: "dirty", dirty });
}
function cursor() {
  const view = views.get(active), pos = view.state.selection.main.head, line = view.state.doc.lineAt(pos);
  document.getElementById("editor-cursor").textContent = `Ln ${line.number}, Col ${pos - line.from + 1}`;
  const languageLabel = document.getElementById("editor-language"); if (languageLabel) languageLabel.textContent = definitions[active].title;
}
const signatureEffect = StateEffect.define();
const signatureField = StateField.define({
  create: () => null,
  update(value, transaction) {
    for (const effect of transaction.effects) if (effect.is(signatureEffect)) return effect.value;
    return transaction.docChanged || transaction.selection ? null : value;
  },
  provide: field => showTooltip.from(field),
});
function completionData() {
  let custom = {}; try { custom = JSON.parse(views.get("characteristics")?.state.doc.toString() ?? "{}"); } catch {}
  return { context: completionContext, custom, globalScript: globalDocument };
}
function informationPanel(info) {
  const dom = document.createElement("div"); dom.className = "completion-information";
  const pre = document.createElement("pre"); pre.textContent = info.signature; dom.append(pre);
  if (info.documentation) { const text = document.createElement("p"); text.textContent = info.documentation; dom.append(text); }
  return dom;
}
async function updateSignature(view) {
  const source = view.state.doc.toString(), position = view.state.selection.main.head;
  // Avoid language-service requests for ordinary navigation outside calls.
  if (!source.slice(0, position).includes("(")) return;
  const info = await semanticCompletion.signature({ ...completionData(), source, position });
  if (view.state.doc.toString() !== source || view.state.selection.main.head !== position) return;
  const tooltip = info ? { pos: position, above: true, create() {
    const dom = informationPanel({ signature: info.prefix, documentation: info.documentation });
    const pre = dom.querySelector("pre");
    info.parameters.forEach((parameter, index) => {
      if (index) pre.append(info.separator);
      if (index === info.active) { const strong = document.createElement("strong"); strong.textContent = parameter; pre.append(strong); }
      else pre.append(parameter);
    }); pre.append(info.suffix);
    return { dom };
  } } : null;
  view.dispatch({ effects: signatureEffect.of(tooltip) });
}
function extensions(key) {
  return [basicSetup, definitions[key].extension, themeSlot.of(themeExtensions()), EditorState.tabSize.of(2),
    lintGutter(), linter(view => diagnose(language(key), view.state.doc.toString(), completionContext.controls, !singleDocument), { delay: 400 }),
    ...(key === "behavior" ? [signatureField, hoverTooltip(async (view, position) => {
      const source = view.state.doc.toString();
      const info = await semanticCompletion.hover({ ...completionData(), source, position });
      if (!info || view.state.doc.toString() !== source) return null;
      return { pos: info.from, end: info.to, above: true, create: () => ({ dom: informationPanel(info) }) };
    }), autocompletion({ override: [async context => {
      if (/Comment/.test(syntaxTree(context.state).resolveInner(context.pos, -1).name)) return null;
      let values = {}; try { values = JSON.parse(views.get("characteristics")?.state.doc.toString() ?? "{}"); } catch {}
      const fast = formaCompletions(context, completionContext.controls, values, completionContext.modules);
      // Literal component names, events and runtime keys retain catalog validation.
      if (fast && /["']/.test(context.state.doc.sliceString(Math.max(0, fast.from - 1), fast.from))) return fast;
      const semantic = await semanticCompletion.complete(context, { context: completionContext, custom: values, globalScript: globalDocument });
      if (!semantic) return fast ?? localCompletionSource(context);
      if (fast && fast.from === semantic.from) {
        const labels = new Set(semantic.options.map(option => option.label));
        semantic.options.push(...fast.options.filter(option => !labels.has(option.label)));
      }
      return semantic;
    }] })] : []),
    keymap.of([indentWithTab, { key: "Mod-s", run: () => { command("save"); return true; } },
      { key: "Mod-Shift-f", run: () => { command("format"); return true; } }]),
    EditorView.updateListener.of(update => {
      if (update.docChanged && !populating) markDirty(singleDocument ? views.get(singleDocument).state.doc.toString() !== (savedSources.behavior ?? "") : [...views].some(([name, view]) => view.state.doc.toString() !== (savedSources[name] ?? "")));
      if (update.selectionSet || update.docChanged) { cursor(); if (key === "behavior" && !populating) updateSignature(update.view); }
      let count = 0; for (const [name, view] of views) {
        let fileCount = 0; forEachDiagnostic(view.state, () => { count++; fileCount++; });
        const tab = document.querySelector(`[data-tab="${name}"]`);
        if (fileCount) tab.dataset.problems = String(fileCount); else delete tab.dataset.problems;
      }
      const indicator = document.getElementById("editor-problems");
      if (indicator) { (indicator.querySelector("span") ?? indicator).textContent = `${count} problem${count === 1 ? "" : "s"}`; indicator.classList.toggle("has-problems", count > 0); }
    })];
}
for (const key of Object.keys(definitions)) views.set(key, new EditorView({ state: EditorState.create({ extensions: extensions(key) }), parent: document.getElementById(`editor-${key}`) }));
function activate(key) {
  if (!views.has(key) || (globalDocument && key !== "behavior") || (singleDocument && key !== singleDocument)) return;
  active = key;
  document.body.dataset.document = key;
  document.getElementById("editor-file-name").textContent = fileName(key);
  send({ event: "document", document: key });
  for (const [name, view] of views) {
    view.dom.parentElement.hidden = name !== key;
    const tab = document.querySelector(`[data-tab="${name}"]`); tab.setAttribute("aria-selected", String(name === key)); tab.tabIndex = name === key ? 0 : -1;
    if (name === key) { view.requestMeasure(); view.focus(); }
  }
  cursor();
}
async function formatKeys(keys) {
  // Parse every requested document before replacing any, so one invalid file cannot partly format a save.
  const originals = new Map(keys.map(key => [key, views.get(key).state.doc.toString()]));
  const changes = await Promise.all(keys.map(async key => [key, await formatCode(language(key), originals.get(key))]));
  if (keys.some(key => views.get(key).state.doc.toString() !== originals.get(key)))
    throw new Error("Document changed while formatting; try again");
  for (const [key, text] of changes) {
    const view = views.get(key);
    if (text !== view.state.doc.toString()) view.dispatch({ changes: { from: 0, to: view.state.doc.length, insert: text } });
  }
}
function setBusy(value) { busy = value; document.querySelectorAll('[data-command="save"],[data-command="format"],[data-command="external"]').forEach(button => button.disabled = value); }
async function command(event) {
  if (busy) return;
  if (event === "problems") {
    let activeCount = 0; forEachDiagnostic(views.get(active).state, () => activeCount++);
    if (!activeCount) for (const [key, view] of views) {
      let count = 0; forEachDiagnostic(view.state, () => count++);
      if (count) { activate(key); break; }
    }
    openLintPanel(views.get(active)); return;
  }
  if (["choose-editor", "close"].includes(event)) { send({ event }); return; }
  setBusy(true);
  try {
    if (event === "format") { await formatKeys([active]); status("Document formatted. Ctrl+S saves and applies."); setBusy(false); return; }
    if (document.getElementById("format-on-save").checked) await formatKeys(singleDocument ? [singleDocument] : Object.keys(definitions));
    send({ event, source: source() }); // Host acknowledges save/external or returns an error.
  } catch (error) { status(`Format failed: ${error.message}. Fix syntax or turn off Format on save.`); setBusy(false); }
}
function receive(message) {
  if (message.action === "editor-active" && !message.active) semanticCompletion.dispose();
  if (message.theme) {
    editorTheme = message.theme === "light" ? "light" : "dark";
    document.documentElement.dataset.theme = editorTheme;
    for (const view of views.values()) view.dispatch({ effects: themeSlot.reconfigure(themeExtensions()) });
  }
  if (message.context && JSON.stringify(completionContext) !== JSON.stringify(message.context)) {
    completionContext = message.context;
    if (message.action === "context") for (const view of views.values()) forceLinting(view);
  }
  if (message.name) document.getElementById("editor-component-name").textContent = message.name;
  if (message.action === "file-name") { singleFileName = message.singleFileName; document.querySelector(`[data-tab="${singleDocument}"] span`).textContent = singleFileName; document.getElementById("editor-file-name").textContent = singleFileName; }
  if (message.action === "activate") activate(message.document);
  if (message.action === "source") {
    globalDocument = !!message.globalScript; singleDocument = message.singleDocument ?? null; singleFileName = message.singleFileName ?? null;
    Object.assign(savedSources, message.source);
    if (message.document && views.has(message.document)) active = message.document;
    document.querySelectorAll('[data-tab]').forEach(tab => { tab.hidden = singleDocument ? tab.dataset.tab !== singleDocument : !!message.globalScript && tab.dataset.tab !== "behavior"; });
    document.querySelector('#tab-behavior span').textContent = message.globalScript ? "main.js" : "script.js";
    if (message.globalScript) { active = "behavior"; document.getElementById("editor-component-name").textContent = "main.js"; }
    populating = true;
    try { for (const [key, view] of views) view.setState(EditorState.create({ doc: (singleDocument ? key === singleDocument ? message.source.behavior : key === "characteristics" ? "{}" : "" : message.source[key]) ?? "", extensions: extensions(key) })); }
    finally { populating = false; }
    if (singleDocument) { active = singleDocument; document.querySelector(`[data-tab="${singleDocument}"] span`).textContent = singleFileName; }
    markDirty(false); activate(active);
  }
  if (message.action === "saved") { Object.assign(savedSources, message.source); const current = source(); markDirty(Object.keys(definitions).some(key => current[key] !== message.source[key])); setBusy(false); }
  if (message.action === "error") setBusy(false);
  if (message.status) status(message.status);
}
document.querySelectorAll("[data-tab]").forEach(tab => tab.addEventListener("click", () => activate(tab.dataset.tab)));
document.querySelector(".editor-tabs").addEventListener("keydown", event => {
  if (!["ArrowLeft", "ArrowRight", "Home", "End"].includes(event.key)) return;
  event.preventDefault();
  const tabs = [...document.querySelectorAll("[data-tab]")].filter(tab => !tab.hidden);
  const index = tabs.findIndex(tab => tab.dataset.tab === active);
  const next = event.key === "Home" ? 0 : event.key === "End" ? tabs.length - 1 : (index + (event.key === "ArrowRight" ? 1 : -1) + tabs.length) % tabs.length;
  activate(tabs[next].dataset.tab); tabs[next].focus();
});
document.querySelectorAll("[data-command]").forEach(button => button.addEventListener("click", () => { command(button.dataset.command); document.querySelector(".editor-tools").open = false; }));
document.addEventListener("pointerdown", event => { const tools = document.querySelector(".editor-tools"); if (!tools.contains(event.target)) tools.open = false; });
document.addEventListener("keydown", event => { if (event.key === "Escape") document.querySelector(".editor-tools").open = false; });
window.chrome?.webview?.addEventListener("message", event => receive(event.data));
window.addEventListener("keydown", event => {
  if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "s") { event.preventDefault(); command("save"); }
});
window.addEventListener("pagehide", () => { semanticCompletion.dispose(); for (const view of views.values()) view.destroy(); });
window.formaCodeEditor = { command, receive, source, views, activate };
send({ event: "ready" }); activate("css");
