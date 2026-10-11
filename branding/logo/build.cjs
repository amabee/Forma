// Builds the Forma logo files from forma-mark.svg, then renders PNG sizes.
// Usage: node branding/logo/build.cjs   (needs Microsoft Edge + Builder Frontend's Playwright)
const fs = require("node:fs");
const path = require("node:path");
const { pathToFileURL } = require("node:url");

const folder = __dirname;
const mark = fs.readFileSync(path.join(folder, "forma-mark.svg"), "utf8");
const defs = mark.match(/<defs>[\s\S]*?<\/defs>/)[0];
const shapes = mark.match(/<path[\s\S]*?\/>/g).join("\n    ");
// The mark's artwork spans x 30–358, y 30–400; center it by its middle point.
const markCenter = { x: 194, y: 215 };

function placedMark(cx, cy, scale) {
  const tx = cx - markCenter.x * scale, ty = cy - markCenter.y * scale;
  return `<g transform="translate(${tx.toFixed(2)} ${ty.toFixed(2)}) scale(${scale})">\n    ${shapes}\n  </g>`;
}

// Rounded app icon: the mark on a rounded-square tile (radius ≈ 22.5% like OS app icons).
function icon(background, border) {
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" role="img" aria-label="Forma">
  ${defs}
  <rect width="512" height="512" rx="116" fill="${background}"/>
  ${border ? `<rect x="1" y="1" width="510" height="510" rx="115" fill="none" stroke="${border}" stroke-width="2"/>` : ""}
  ${placedMark(256, 256, 0.78)}
</svg>
`;
}

// Wordmark FORMA as stroked geometry, so it renders the same everywhere (no font needed).
// Letters are 80 units tall with an 11-unit stroke; the A has no crossbar.
function wordmark(x, y, color) {
  const s = 11, h = 80, i = s / 2;
  const letters = [
    { w: 44, d: `M${i} 0V${h}M${i} ${i}H44M${i} 41H40` },
    { w: 90, d: `M45 ${i}A${45 - i} ${40 - i} 0 1 1 44.9 ${i}Z` },
    { w: 58, d: `M${i} ${h}V${i}H33A19 19 0 0 1 33 43.5H${i}M31 43.5L58 ${h}` },
    { w: 84, d: `M${i} ${h}V2L42 56L78.5 2V${h}` },
    { w: 88, d: `M0 ${h}L44 2L88 ${h}` },
  ];
  const gap = 56;
  let cursor = x;
  const paths = letters.map(letter => {
    const g = `<path transform="translate(${cursor} ${y})" d="${letter.d}"/>`;
    cursor += letter.w + gap;
    return g;
  });
  return { svg: `<g fill="none" stroke="${color}" stroke-width="${s}" stroke-linejoin="miter">\n    ${paths.join("\n    ")}\n  </g>`, width: cursor - gap - x };
}

function logo(color, background) {
  const word = wordmark(0, 0, color);
  const width = 760, height = 640;
  const wx = (width - word.width) / 2;
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${width} ${height}" role="img" aria-label="Forma">
  ${defs}
  ${background ? `<rect width="${width}" height="${height}" fill="${background}"/>` : ""}
  ${placedMark(width / 2, 230, 1)}
  <g transform="translate(${wx.toFixed(2)} 520)">
  ${word.svg}
  </g>
</svg>
`;
}

const files = {
  "forma-icon.svg": icon("#fdfcfb", "#ece8f4"),
  "forma-icon-dark.svg": icon("#17141f", "#2c2838"),
  "forma-logo.svg": logo("#14125c", null),
  "forma-logo-light.svg": logo("#f2eff8", null),
};
for (const [name, content] of Object.entries(files)) fs.writeFileSync(path.join(folder, name), content);
console.log("wrote", Object.keys(files).join(", "));

(async () => {
  const { chromium } = require("../../src/Forma.Builder/Frontend/node_modules/playwright");
  const browser = await chromium.launch({ channel: "msedge" });
  const page = await browser.newPage();
  const render = async (file, out, width, height) => {
    await page.setViewportSize({ width, height });
    await page.goto(pathToFileURL(path.join(folder, file)).href);
    await page.screenshot({ path: path.join(folder, "png", out), omitBackground: true });
  };
  fs.mkdirSync(path.join(folder, "png"), { recursive: true });
  const icoSizes = [16, 24, 32, 48, 64, 256];
  for (const size of new Set([1024, 512, 256, 128, 64, 48, 32, 24, 16])) await render("forma-icon.svg", `forma-icon-${size}.png`, size, size);
  await render("forma-icon-dark.svg", "forma-icon-dark-512.png", 512, 512);
  await render("forma-logo.svg", "forma-logo.png", 1520, 1280);
  await render("forma-logo-light.svg", "forma-logo-light.png", 1520, 1280);
  await browser.close();
  // Windows .ico with PNG-compressed frames (supported since Vista) for the Builder's exe and windows.
  const frames = icoSizes.map(size => ({ size, data: fs.readFileSync(path.join(folder, "png", `forma-icon-${size}.png`)) }));
  const header = Buffer.alloc(6 + 16 * frames.length);
  header.writeUInt16LE(0, 0); header.writeUInt16LE(1, 2); header.writeUInt16LE(frames.length, 4);
  let offset = header.length;
  frames.forEach((frame, index) => {
    const entry = 6 + index * 16;
    header.writeUInt8(frame.size >= 256 ? 0 : frame.size, entry);
    header.writeUInt8(frame.size >= 256 ? 0 : frame.size, entry + 1);
    header.writeUInt16LE(1, entry + 4); header.writeUInt16LE(32, entry + 6);
    header.writeUInt32LE(frame.data.length, entry + 8); header.writeUInt32LE(offset, entry + 12);
    offset += frame.data.length;
  });
  fs.writeFileSync(path.join(folder, "forma.ico"), Buffer.concat([header, ...frames.map(frame => frame.data)]));
  console.log("rendered png/ and forma.ico");
})().catch(error => { console.error(error); process.exit(1); });
