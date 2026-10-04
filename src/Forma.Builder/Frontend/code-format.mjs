import { format } from "prettier/standalone";
import babel from "prettier/plugins/babel";
import estree from "prettier/plugins/estree";
import postcss from "prettier/plugins/postcss";

export async function formatCode(language, source) {
  if (!source.trim() && language !== "json") return source;
  return format(source, {
    parser: { javascript: "babel", css: "css", json: "json" }[language],
    plugins: [babel, estree, postcss], tabWidth: 2, printWidth: 100,
  });
}
