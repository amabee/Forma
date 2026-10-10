// Each open component keeps its inference state in an isolated worker.
export function createJavaScriptCompletionClient() {
  let worker, sequence = 0, unavailable = false;
  const pending = new Map();
  function dispose() {
    worker?.terminate(); worker = null;
    for (const entry of pending.values()) { clearTimeout(entry.timeout); entry.resolve(null); }
    pending.clear();
  }
  async function request(method, data) {
    if (unavailable || typeof Worker === "undefined") return null;
    try {
      if (!worker) {
        worker = new Worker(new URL("javascript-worker.bundle.js", window.location.href));
        worker.onmessage = ({ data }) => {
          const entry = pending.get(data.id); if (!entry) return;
          clearTimeout(entry.timeout); pending.delete(data.id); entry.resolve(data.error ? null : data.result);
        };
        worker.onerror = () => { unavailable = true; dispose(); };
      }
      const id = ++sequence;
      return await new Promise(resolve => {
        const timeout = setTimeout(() => { pending.delete(id); resolve(null); }, 10000);
        pending.set(id, { resolve, timeout }); worker.postMessage({ id, method, request: data });
      });
    } catch { unavailable = true; dispose(); return null; }
  }
  return {
    async complete(context, data) {
      const query = { ...data, source: context.state.doc.toString(), position: context.pos };
      const result = await request("complete", query);
      if (!result || context.aborted) return null;
      return { from: result.from, options: result.options.map(option => ({
        ...option,
        info: async () => {
          const details = await request("details", { ...query, entry: option.entry });
          if (!details) return "";
          const panel = document.createElement("div"); panel.className = "completion-information";
          const signature = document.createElement("pre"); signature.textContent = details.signature; panel.append(signature);
          const docs = document.createElement("p"); docs.textContent = [details.documentation, details.tags].filter(Boolean).join("\n"); panel.append(docs);
          return panel;
        },
      })) };
    }, hover: data => request("hover", data), signature: data => request("signature", data), dispose,
  };
}
