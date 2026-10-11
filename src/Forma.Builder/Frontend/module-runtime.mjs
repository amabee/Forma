import { transform } from "sucrase";

// Each Preview owns its modules. Only imported files execute; no network or disk imports.
export function createModuleSession(entries = []) {
  const sources = new Map(entries.map(entry => [entry.path, entry.content]));
  const cache = new Map();
  let sharedApi, sharedComponent;
  function resolve(specifier, owner, localSources) {
    if (!/^\.\.?\//.test(specifier) || /[\\?#]/.test(specifier))
      throw new Error(`Import '${specifier}' must be a relative project file path.`);
    const parts = owner.split("/").slice(0, -1);
    for (const part of specifier.split("/")) {
      if (part === "." || !part) continue;
      if (part === "..") { if (!parts.length) throw new Error("Import escapes the project files."); parts.pop(); }
      else parts.push(part);
    }
    const path = parts.join("/");
    const resolved = [path, path + ".js", path + "/index.js"].find(candidate => localSources?.has(candidate) || sources.has(candidate));
    if (!resolved) throw new Error(`Cannot find project module '${specifier}' imported by ${owner}.`);
    return resolved;
  }
  function run(source, path, module, api, component, requireModule = specifier => load(resolve(specifier, path))) {
    const code = transform(source, { transforms: ["imports"], disableESTransforms: true, filePath: path }).code;
    new Function("require", "module", "exports", "forma", "component", "api", code)(
      requireModule, module, module.exports, api, component, api);
  }
  function load(path) {
    if (cache.has(path)) return cache.get(path).exports;
    const module = { exports: {} };
    cache.set(path, module);
    try {
      if (path.endsWith(".json")) module.exports = JSON.parse(sources.get(path));
      else if (path.endsWith(".js")) run(sources.get(path), path, module, sharedApi, sharedComponent);
      else throw new Error(`Cannot import ${path}; import JavaScript or JSON files.`);
      return module.exports;
    } catch (error) { cache.delete(path); throw error; }
  }
  return {
    execute(source, api, component, path = "main.js", localEntries = []) {
      sharedApi ??= api; sharedComponent ??= component;
      // Component event modules get their own API and cache; shared project
      // providers keep the session-wide cache initialized by main.js.
      const localSources = new Map(localEntries.map(entry => [entry.path, entry.content]));
      const localCache = new Map();
      function loadScoped(resolved) {
        if (!localSources.has(resolved)) return load(resolved);
        if (localCache.has(resolved)) return localCache.get(resolved).exports;
        const module = { exports: {} }; localCache.set(resolved, module);
        try {
          run(localSources.get(resolved), resolved, module, api, component, specifier => loadScoped(resolve(specifier, resolved, localSources)));
          return module.exports;
        } catch (error) { localCache.delete(resolved); throw error; }
      }
      run(source, path, { exports: {} }, api, component, specifier => loadScoped(resolve(specifier, path, localSources)));
    }
  };
}

if (typeof window !== "undefined") window.formaModuleRuntime = { createModuleSession };
