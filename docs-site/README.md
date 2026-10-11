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
TypeScript sources. Local edits are published only after they are committed and
pushed to the connected GitHub repository.

## Automatic deployments on Vercel

The production site is https://forma-documentation.vercel.app/. Vercel's native
GitHub integration handles deployment; GitHub Actions validates the site. No
Vercel API token or duplicate deployment action is needed.

Verify these settings on the existing Vercel project:

- Connected Git repository: `amabee/Forma`.
- Production branch: `master`. Pushes/merges here update the public site;
  pushes to other branches create preview deployments.
- Root Directory: `docs-site`, with **Include source files outside of the Root
  Directory in the Build Step** enabled. The generator reads `../docs/` and
  `../src/`; omitting them causes the build to fail.
- Node.js version: `24.x`.
- Environment variable `DOCS_SITE`: `https://forma-documentation.vercel.app`
  for Production and Preview. Leave `DOCS_BASE` unset for this domain.
- If an Ignored Build Step is configured, remove any rule that only watches
  `docs-site/`. Canonical guides and component metadata outside that folder also
  affect the site. The default build-on-push behavior is suitable here.

`vercel.json` fixes the Astro preset, `npm ci`, output directory, and build
command. Every Vercel build runs `npm run check` before building and validating
generated pages. A failed build does not replace the existing production site.

`.github/workflows/docs-site.yml` runs on pushes and pull requests affecting
the site, canonical guides, or model/inspector/completion sources, and can also
be started manually. It installs locked dependencies, checks Astro, builds all
references, verifies links/assets, and tests desktop/mobile interactions with
Playwright Chromium. It does not modify files or deploy independently.

GitHub CI and Vercel run independently. To require browser checks before a
production update, require **Docs build and browser checks** on pull requests
to `master` in GitHub branch rules and merge only after it passes. This workflow
does not change repository branch protection or Vercel deployment checks.

After pushing, check GitHub Actions and the Vercel deployment for that commit.
Uncommitted local edits never reach the public site.

## Other static hosts

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

Upload the contents of `dist/` or connect the branch to your static host. The
Astro site/base remain configurable through environment variables.

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
mobile navigation, the four source tabs, and the event reference on desktop and
mobile. Screenshots are saved to ignored `dist/qa/`.
