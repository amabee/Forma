import { defineConfig } from 'astro/config';
import { unified } from '@astrojs/markdown-remark';
import tailwindcss from '@tailwindcss/vite';

export default defineConfig({
  output: 'static',
  site: process.env.DOCS_SITE || undefined,
  base: process.env.DOCS_BASE || '/',
  trailingSlash: 'always',
  markdown: { processor: unified({ shikiConfig: { themes: { light: 'github-light', dark: 'github-dark' }, defaultColor: false }, smartypants: false }) },
  vite: { plugins: [tailwindcss()] },
});
