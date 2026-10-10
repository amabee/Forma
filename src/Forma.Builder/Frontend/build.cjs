const fs = require("node:fs");
const path = require("node:path");
const destination = path.join(__dirname, "../DesignerWeb/icons");
const source = path.join(__dirname, "node_modules/lucide-static");
const names = [
  "file-plus",
  "folder-open",
  "save",
  "undo-2",
  "redo-2",
  "play",
  "monitor",
  "search",
  "chevron-down",
  "chevron-up",
  "chevron-right",
  "house",
  "x",
  "panel-top",
  "group",
  "columns-2",
  "panels-top-left",
  "list",
  "grid-2x2",
  "rectangle-horizontal",
  "text-cursor-input",
  "type",
  "square-check",
  "circle",
  "chevrons-up-down",
  "list-ordered",
  "image",
  "timer",
  "settings",
  "table",
  "sliders-horizontal",
  "gauge",
  "toggle-left",
  "toggle-right",
  "calendar",
  "clock",
  "calendar-clock",
  "palette",
  "lock",
  "link",
  "list-checks",
  "toolbox",
  "sun",
  "moon",
];
fs.mkdirSync(destination, { recursive: true });
for (const name of names)
  fs.copyFileSync(
    path.join(source, "icons", `${name}.svg`),
    path.join(destination, `${name}.svg`),
  );
fs.copyFileSync(
  path.join(source, "LICENSE"),
  path.join(destination, "LICENSE-LUCIDE.txt"),
);
fs.writeFileSync(
  path.join(destination, "README.md"),
  "# Lucide icons\n\nDownloaded from the pinned lucide-static npm package. Source: https://lucide.dev/\nLicense: LICENSE-LUCIDE.txt. Rebuild with npm run build from ../Frontend.\n",
);

require("esbuild").buildSync({ entryPoints: [path.join(__dirname, "code-editor.mjs")], bundle: true, minify: true, format: "iife", target: "es2022", outfile: path.join(__dirname, "../DesignerWeb/code-editor.bundle.js"), legalComments: "external" });

require("esbuild").buildSync({ entryPoints: [path.join(__dirname, "module-runtime.mjs")], bundle: true, minify: true, format: "iife", platform: "browser", target: "es2022", outfile: path.join(__dirname, "../DesignerWeb/module-runtime.bundle.js"), legalComments: "external" });
fs.copyFileSync(path.join(__dirname, "node_modules/sucrase/LICENSE"), path.join(__dirname, "../DesignerWeb/LICENSE-SUCRASE.txt"));

// Bundle compiler declarations locally; workers never fetch libraries from a CDN.
const tsLibraryFolder = path.join(__dirname, "node_modules/typescript/lib");
const libraries = Object.fromEntries(fs.readdirSync(tsLibraryFolder).filter(name => /^lib.*\.d\.ts$/.test(name)).map(name => [name, fs.readFileSync(path.join(tsLibraryFolder, name), "utf8")]));
require("esbuild").buildSync({
  entryPoints: [path.join(__dirname, "javascript-worker.mjs")], bundle: true, minify: true,
  format: "iife", platform: "browser", target: "es2022", legalComments: "external",
  define: { FORMA_TS_LIBRARIES: JSON.stringify(libraries) },
  outfile: path.join(__dirname, "../DesignerWeb/javascript-worker.bundle.js")
});

fs.copyFileSync(path.join(tsLibraryFolder, "../LICENSE.txt"), path.join(__dirname, "../DesignerWeb/LICENSE-TYPESCRIPT.txt"));
