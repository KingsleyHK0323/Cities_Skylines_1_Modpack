import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';
import { fileURLToPath } from 'node:url';

// Use YAML's ESM browser entry for Astro content sync and keep Vite cache in .tools/tmp — OpenAI-Codex.
const yamlBrowserEntrypoint = fileURLToPath(new URL('./node_modules/yaml/browser/index.js', import.meta.url));
const siteViteCacheDir = fileURLToPath(new URL('../../../.tools/tmp/site-vite-cache/', import.meta.url));

export default defineConfig({
	site: 'https://kingsleyhk0323.github.io',
	base: '/Cities_Skylines_1_Modpack',
	trailingSlash: 'always',
	vite: {
		cacheDir: siteViteCacheDir,
		resolve: {
			alias: [{ find: /^yaml$/, replacement: yamlBrowserEntrypoint }],
		},
	},
	integrations: [
		starlight({
			title: 'Cities: Skylines 1 Modpack',
			defaultLocale: 'root',
			locales: {
				root: { label: 'English', lang: 'en' },
				'zh-tw': { label: '繁體中文', lang: 'zh-TW' },
			},
			logo: { src: './src/assets/icons/icon-192.png', alt: 'Cities: Skylines 1 Modpack' },
			favicon: '/icons/favicon-32.png',
			customCss: ['./src/styles/starlight.css'],
			components: { Header: './src/components/DocsHeader.astro', ThemeProvider: './src/components/DocsThemeProvider.astro', ThemeSelect: './src/components/DocsThemeSelect.astro' },
			social: [{ icon: 'github', label: 'GitHub', href: 'https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack' }],
			head: [
				{ tag: 'meta', attrs: { property: 'og:type', content: 'website' } },
				{ tag: 'meta', attrs: { property: 'og:image', content: 'https://kingsleyhk0323.github.io/Cities_Skylines_1_Modpack/social-preview.webp' } },
				{ tag: 'meta', attrs: { name: 'twitter:card', content: 'summary_large_image' } },
			],
			sidebar: [
				// Localize download and status destinations through matching content slugs.
				// OpenAI-Codex_Responsible: Modpack_Website.
				{ label: 'Downloads', translations: { 'zh-TW': '下載' }, items: [{ label: 'Development downloads', translations: { 'zh-TW': '開發版下載' }, slug: 'wiki/development-downloads' }] },
				{ label: 'Start here', translations: { 'zh-TW': '從這裡開始' }, items: [{ slug: 'wiki', label: 'Wiki home', translations: { 'zh-TW': 'Wiki 首頁' } }, { slug: 'wiki/getting-started', label: 'Getting started', translations: { 'zh-TW': '開始使用' } }] },
				{ label: 'AI Improve', translations: { 'zh-TW': 'AI Improve' }, items: [
					{ slug: 'wiki/ai-improve/overview', label: 'Overview', translations: { 'zh-TW': '總覽' } },
					{ slug: 'wiki/ai-improve/features', label: 'All features', translations: { 'zh-TW': '全部功能' } },
					{ slug: 'wiki/ai-improve/settings', label: 'Settings', translations: { 'zh-TW': '設定說明' } },
					{ slug: 'wiki/ai-improve/in-game', label: 'Using it in game', translations: { 'zh-TW': '遊戲內使用' } },
					{ slug: 'wiki/ai-improve/compatibility', label: 'Compatibility', translations: { 'zh-TW': '相容性' } },
					{ slug: 'wiki/ai-improve/faq', label: 'FAQ', translations: { 'zh-TW': '常見問題' } },
				] },
				{ label: 'Other mods', translations: { 'zh-TW': '其他模組' }, items: [
					{ slug: 'wiki/transport/overview', label: 'Automated Public Transport Planning', translations: { 'zh-TW': '公共運輸自動規劃' } },
					{ slug: 'wiki/ai-gaming/overview', label: 'AI Gaming', translations: { 'zh-TW': 'AI Gaming' } },
				] },
				{ label: 'Project', translations: { 'zh-TW': '專案' }, items: [
					{ label: 'Module status and targets', translations: { 'zh-TW': '模組狀態與替代目標' }, slug: 'wiki/project/status' },
					{ slug: 'wiki/project/roadmap', label: 'Roadmap', translations: { 'zh-TW': '路線圖' } },
					{ slug: 'wiki/project/contributing', label: 'Contributing & license', translations: { 'zh-TW': '參與與授權' } },
				] },
			],
		}),
	],
});

// Author: OpenAI-Codex.
