import ts from "typescript";
import { formaDeclarations } from "./javascript-types.mjs";

// Parse source for static export names. Never execute user JavaScript to infer types.
function globalExports(source) {
  const file = ts.createSourceFile("__forma_main__.js", source, ts.ScriptTarget.Latest, true, ts.ScriptKind.JS);
  const modules = new Map(), shared = new Map();
  function visit(node) {
    if (ts.isFunctionLike(node)) return;
    if (ts.isCallExpression(node) && ts.isPropertyAccessExpression(node.expression)
        && ["forma", "api"].includes(node.expression.expression.getText(file))
        && node.expression.name.text === "provide" && node.arguments.length >= 2
        && ts.isStringLiteral(node.arguments[0])) modules.set(node.arguments[0].text, node.arguments[1].getText(file));
    if (ts.isPropertyAccessExpression(node) && ["forma.shared", "api.shared"].includes(node.expression.getText(file))) { if (!shared.has(node.name.text)) shared.set(node.name.text, "undefined"); }
    if (ts.isBinaryExpression(node) && node.operatorToken.kind === ts.SyntaxKind.EqualsToken && ts.isPropertyAccessExpression(node.left) && ["forma.shared", "api.shared"].includes(node.left.expression.getText(file))) shared.set(node.left.name.text, node.right.getText(file));
    ts.forEachChild(node, visit);
  }
  file.statements.forEach(statement => {
    // Function-local provide calls cannot safely be lifted into a module export.
    if (!ts.isFunctionDeclaration(statement)) visit(statement);
  });
  return { suffix: `\nexport const __formaModules = {${[...modules].map(([key, value]) => `${JSON.stringify(key)}: ${value}`).join(",")}};\nexport const __formaShared = {${[...shared].map(([key, value]) => `${JSON.stringify(key)}: ${value}`).join(",")}};`, shared: [...shared.keys()] };
}

const modulePrefix = "export {};\n";
export function createJavaScriptService(libraries) {
  const files = new Map(Object.entries(libraries).map(([name, text]) => [`/${name}`, { text, version: 0 }]));
  const settings = { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ESNext, moduleResolution: ts.ModuleResolutionKind.Node10, allowJs: true, resolveJsonModule: true, checkJs: true, noEmit: true, strict: true, lib: ["lib.es2022.full.d.ts"] };
  const host = {
    getCompilationSettings: () => settings,
    getScriptFileNames: () => [...files.keys()].filter(name => !name.startsWith("/lib.")),
    getScriptVersion: name => String(files.get(name)?.version ?? 0),
    getScriptSnapshot: name => files.has(name) ? ts.ScriptSnapshot.fromString(files.get(name).text) : undefined,
    getCurrentDirectory: () => "/", getDefaultLibFileName: () => "/lib.es2022.full.d.ts",
    fileExists: name => files.has(name), readFile: name => files.get(name)?.text,
    readDirectory: () => [], directoryExists: () => true, getDirectories: () => [], useCaseSensitiveFileNames: () => true,
  };
  const service = ts.createLanguageService(host);
  function write(name, text) {
    const previous = files.get(name);
    if (previous?.text !== text) files.set(name, { text, version: (previous?.version ?? 0) + 1 });
  }
  let projectPaths = new Set();
  function update({ source, globalScript, context = {}, custom = {} }) {
    const nextPaths = new Set((context.projectFiles ?? []).map(file => "/" + file.path));
    for (const path of projectPaths) if (!nextPaths.has(path)) files.delete(path);
    projectPaths = nextPaths;
    for (const file of context.projectFiles ?? []) {
      write("/" + file.path, file.path.endsWith(".json") ? file.content : modulePrefix + file.content);
    }
    let entryPath = "/__forma_main__.js";
    while (projectPaths.has(entryPath)) entryPath = "/_" + entryPath.slice(1);
    let scriptPath = "/__forma_component__.js";
    while (projectPaths.has(scriptPath)) scriptPath = "/_" + scriptPath.slice(1);
    if (context.filePath && !globalScript) scriptPath = "/" + context.filePath;
    const global = globalScript ? source : context.globalSource ?? "";
    const exports = globalExports(global);
    write(entryPath, modulePrefix + global + exports.suffix);
    write(scriptPath, modulePrefix + (globalScript ? "" : source));
    write("/forma.d.ts", formaDeclarations(context.controls, custom, exports.shared).replaceAll("./global-script", "." + entryPath.slice(0, -3)));
    return globalScript ? entryPath : scriptPath;
  }
  return {
    complete(request) {
      const file = update(request), position = request.position + modulePrefix.length;
      const completion = service.getCompletionsAtPosition(file, position, { includeCompletionsForModuleExports: false, includeCompletionsWithInsertText: true });
      if (!completion) return null;
      const prefix = request.source.slice(0, request.position), word = /[\w$]*$/.exec(prefix)[0];
      const from = completion.optionalReplacementSpan ? completion.optionalReplacementSpan.start - modulePrefix.length : request.position - word.length;
      return { from, options: completion.entries.filter(entry => !["__formaModules", "__formaShared"].includes(entry.name)).map(entry => ({
        label: entry.name, type: entry.kind === "method" || entry.kind === "function" ? "function" : entry.kind === "class" ? "class" : entry.kind === "property" ? "property" : entry.kind === "keyword" ? "keyword" : "variable",
        apply: entry.insertText && !entry.isSnippet ? entry.insertText : entry.name,
        entry: { name: entry.name, source: entry.source, data: entry.data },
      })) };
    },
    details(request) {
      const file = update(request);
      const details = service.getCompletionEntryDetails(file, request.position + modulePrefix.length, request.entry.name, {}, request.entry.source, {}, request.entry.data);
      return details ? { signature: ts.displayPartsToString(details.displayParts), documentation: ts.displayPartsToString(details.documentation), tags: details.tags?.map(tag => `${tag.name}: ${ts.displayPartsToString(tag.text)}`).join("\n") ?? "" } : null;
    },
    hover(request) {
      const info = service.getQuickInfoAtPosition(update(request), request.position + modulePrefix.length);
      return info ? { from: info.textSpan.start - modulePrefix.length, to: info.textSpan.start + info.textSpan.length - modulePrefix.length, signature: ts.displayPartsToString(info.displayParts), documentation: ts.displayPartsToString(info.documentation) } : null;
    },
    signature(request) {
      const help = service.getSignatureHelpItems(update(request), request.position + modulePrefix.length, undefined);
      if (!help) return null;
      const item = help.items[help.selectedItemIndex];
      return { prefix: ts.displayPartsToString(item.prefixDisplayParts), separator: ts.displayPartsToString(item.separatorDisplayParts), suffix: ts.displayPartsToString(item.suffixDisplayParts), parameters: item.parameters.map(parameter => ts.displayPartsToString(parameter.displayParts)), active: help.argumentIndex, documentation: ts.displayPartsToString(item.documentation) };
    },
    dispose: () => service.dispose(),
  };
}
