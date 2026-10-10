# Forma documentation website

An Astro + Tailwind static documentation site for the current Forma Builder.
Every implemented Toolbox component and Form has its own reference page, with
setup, a JavaScript recipe, inspector fields, runtime get/set/bind support, and
C# constructor defaults. Guides cover the workspace, events, reactive state,
providers/imports, project files, troubleshooting, and implementation limits.

## Run locally

Requires Node 22.12 or later.

```powershell
cd docs-site
npm ci
npm run dev
```

Open the local URL printed by Astro. `npm run build` creates `dist/` and checks
all internal links, fragments, images, and component reference sections.
`npm run preview` serves the production output. `npm run check` checks Astro and
TypeScript sources. Nothing is published automatically.

## Publish later

Use this folder as the hosting provider's project/root directory, `npm run build`
as the build command, and `dist` as the output directory. This is static output;
no server adapter or application backend is needed.

For a custom domain, set `DOCS_SITE` to the final HTTPS origin before building.
For a repository subdirectory such as GitHub Pages, also set `DOCS_BASE`:

```powershell
$env:DOCS_SITE = "https://amabee.github.io"
$env:DOCS_BASE = "/Forma/"
npm run build
```

Set these environment variables on the host's build configuration. Do not set
them only after the build: generated content, search results, assets, navigation,
and canonical links need the same base. To return to local root hosting:

```powershell
Remove-Item Env:DOCS_SITE -ErrorAction SilentlyContinue
Remove-Item Env:DOCS_BASE -ErrorAction SilentlyContinue
```

Upload the contents of `dist/` or connect the branch to your static host. No
domain or deployment is hardcoded and no publish workflow runs on push.

## Maintain the content

- Existing Markdown in `../docs/` supplies the long-form guides and detailed
  behavior notes. Edit those files to update a guide.
- `content/components.mjs` supplies component descriptions and practical
  JavaScript recipes. Missing entries fail the build rather than silently
  dropping a component.
- `content/catalog.json` is a committed snapshot of actual InspectorCatalog
  metadata and Core constructor defaults. After changing the model/inspector,
  regenerate it with `.NET 10` using `npm run catalog`. Source fingerprints in
  `catalog-sources.json` make the build fail if those sources change without a
  refreshed snapshot. This prevents silently publishing stale property tables.
- `scripts/generate-content.mjs` reads the live Toolbox, runtime property
  catalog, completion property/type functions, and canonical guides at build
  time. The docs follow the application's supported property keys.
- Generated Markdown in `src/generated/` and `public/search-index.json` are
  ignored. Do not edit them; dev/build regenerates them.
- `src/layouts/`, `src/pages/`, `src/styles/`, and `src/scripts/` hold the site
  shell, homepage, library, themes, search, code-copy buttons, and navigation.

The interactive greeting on the homepage illustrates input-to-label behavior;
it is a browser demo, not the Windows Builder runtime. The source tabs illustrate actual component file names and supported APIs. No analytics, CDN fonts, or hosted search service
is required. Search loads a local index only when opened. Native file/folder
pickers and other desktop-specific behavior are documented rather than mocked.

## Browser verification

Serve the production build with `npm run preview`, then in a second terminal:

```powershell
npm run test:browser
```

The browser check uses installed Chrome by default. Set `DOCS_BROWSER_CHANNEL`
to `msedge` to use installed Edge, or install Playwright Chromium with
`npx playwright install chromium` and set it to `chromium`. It checks the
greeting demo, component filtering, full-text search, themes, code copying, and
mobile navigation. Screenshots are saved to ignored `dist/qa/`.
