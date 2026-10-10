import { createJavaScriptService } from "./javascript-service.mjs";
const service = createJavaScriptService(FORMA_TS_LIBRARIES);
self.onmessage = ({ data }) => {
  try { self.postMessage({ id: data.id, result: service[data.method](data.request) }); }
  catch (error) { self.postMessage({ id: data.id, error: error.message }); }
};
