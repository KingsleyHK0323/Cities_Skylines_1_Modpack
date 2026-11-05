import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

// Resolved from the working directory (site/) rather than import.meta.url: during the static
// build this module is bundled into dist/.prerender/chunks/, so a path relative to the module
// no longer points at the website branch root.
const changeLogPath = resolve(process.cwd(), '..', 'Change Log.md');
const source = readFileSync(changeLogPath, 'utf8');

export interface ChangeEntry { date: string; title: string; bullets: string[]; }

export const changeEntries: ChangeEntry[] = source
	.split(/(?=^##?\s+\d{4}-\d{2}-\d{2})/m)
	.map((part) => {
		const heading = part.match(/^##?\s+(\d{4}-\d{2}-\d{2})\s*(?:[—–-]\s*)?(.*)$/m);
		if (!heading) return null;
		const bullets = part.split('\n').filter((line) => /^\s*[-*]\s+/.test(line)).map((line) => line.replace(/^\s*[-*]\s+/, '').trim());
		return { date: heading[1], title: heading[2].trim(), bullets };
	})
	.filter((entry): entry is ChangeEntry => Boolean(entry))
	// Keep explicitly marked internal workflow notes out of the public site. Signature: OpenAI-Codex.
	.filter((entry) => !entry.title.endsWith('<!-- internal -->'))
	.sort((a, b) => b.date.localeCompare(a.date));
