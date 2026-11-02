import { execFileSync } from 'node:child_process';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

// Updated by OpenAI-Codex on 2026-10-26 to support an explicitly verified source commit
// when sandbox policy blocks the extractor's child-process Git lookup.
// Updated by OpenAI-Codex_extractor_worker for callback switches and the current project layout.

const siteRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const sourceRoot = path.resolve(
	process.env.AI_IMPROVE_SOURCE || path.join(siteRoot, '..', '..', '..', 'AI_Improve', 'Source_Code'),
);
const outputPath = path.join(siteRoot, 'src', 'data', 'ai-improve.json');

function assertFile(value, file) {
	if (!value) throw new Error('Could not extract required source data from ' + file);
	return value;
}

function balancedBlock(text, opening) {
	let depth = 0;
	let quote = false;
	let escape = false;
	let lineComment = false;
	let blockComment = false;
	for (let i = opening; i < text.length; i++) {
		const ch = text[i];
		const next = text[i + 1];
		if (lineComment) {
			if (ch === '\n') lineComment = false;
			continue;
		}
		if (blockComment) {
			if (ch === '*' && next === '/') { blockComment = false; i++; }
			continue;
		}
		if (quote) {
			if (escape) escape = false;
			else if (ch === '\\') escape = true;
			else if (ch === '"') quote = false;
			continue;
		}
		if (ch === '/' && next === '/') { lineComment = true; i++; continue; }
		if (ch === '/' && next === '*') { blockComment = true; i++; continue; }
		if (ch === '"') { quote = true; continue; }
		if (ch === '{') depth++;
		if (ch === '}' && --depth === 0) return text.slice(opening + 1, i);
	}
	throw new Error('Unclosed C# dictionary block.');
}

function unquote(csharpLiteral) {
	return JSON.parse(csharpLiteral);
}

function readLanguageTable(source, language) {
	const marker = language === 'en'
		? '[DefaultLanguage] = new Dictionary<string, string>'
		: '["' + language + '"] = new Dictionary<string, string>';
	const markerAt = source.indexOf(marker);
	assertFile(markerAt >= 0, 'Localization.cs (' + language + ' table)');
	const opening = source.indexOf('{', markerAt);
	const block = balancedBlock(source, opening);
	const result = new Map();
	const entry = /\["((?:\\.|[^"\\])*)"\]\s*=\s*((?:"(?:\\.|[^"\\])*"\s*\+\s*)*"(?:\\.|[^"\\])*")\s*,?/g;
	for (const match of block.matchAll(entry)) {
		const key = unquote('"' + match[1] + '"');
		const parts = [...match[2].matchAll(/"(?:\\.|[^"\\])*"/g)].map((part) => unquote(part[0]));
		result.set(key, parts.join(''));
	}
	return result;
}

function parseSettings(source) {
	const settings = new Map();
	const declaration = /(?:public|private)\s+static\s+readonly\s+Saved(Bool|Int|Float|String)\s+(\w+)\s*=\s*new Saved\1\("([^"]+)",\s*FileName,\s*([\s\S]*?),\s*true\);/g;
	for (const match of source.matchAll(declaration)) {
		settings.set(match[2], {
			property: match[2],
			key: match[3],
			type: match[1].toLowerCase(),
			defaultExpression: match[4].trim().replace(/\s+/g, ' '),
		});
	}
	const resolved = new Map();
	function valueOf(property, seen = new Set()) {
		if (resolved.has(property)) return resolved.get(property);
		if (seen.has(property)) return null;
		seen.add(property);
		const setting = settings.get(property);
		if (!setting) return null;
		const expr = setting.defaultExpression;
		let value = null;
		if (expr === 'true') value = true;
		else if (expr === 'false') value = false;
		else if (/^-?\d+(?:\.\d+)?f?$/.test(expr)) value = Number(expr.replace(/f$/, ''));
		else if (expr.startsWith('"') && expr.endsWith('"')) value = unquote(expr);
		else if (expr.endsWith('.value')) value = valueOf(expr.slice(0, -6), seen);
		resolved.set(property, value);
		return value;
	}
	for (const key of settings.keys()) settings.get(key).defaultValue = valueOf(key);
	return settings;
}

function translated(key, english, traditional) {
	const en = english.get(key);
	assertFile(en, 'Localization.cs key ' + key);
	const zh = traditional.get(key);
	return { en, zhTW: zh ?? en, zhTWFallback: zh === undefined };
}

function readSections(source, english, traditional, settings) {
	const sectionStart = [...source.matchAll(/model\.Add\(new Section/g)].map((match) => match.index);
	const sections = [];
	for (let i = 0; i < sectionStart.length; i++) {
		const chunk = source.slice(sectionStart[i], sectionStart[i + 1] ?? source.length);
		const sectionKey = chunk.match(/NavKey\s*=\s*"(nav\.[^"]+)"/);
		if (!sectionKey) continue;
		const featureMarkers = [...chunk.matchAll(/Toggle\("(feature\.[^"]+)"\s*,\s*ModSettings\.(\w+)(?=\s*[,)])|new Feature\s*\{\s*Key\s*=\s*"(feature\.[^"]+)"\s*\}/g)];
		const features = [];
		for (let index = 0; index < featureMarkers.length; index++) {
			const match = featureMarkers[index];
			const featureKey = match[1] || match[3];
			const switchProperty = match[2] || null;
			const featureSource = chunk.slice(match.index, featureMarkers[index + 1]?.index ?? chunk.length);
			const tunables = [];
			const withExpression = /\.With\("(tune\.[^"]+)"\s*,\s*([-\d.]+)f\s*,\s*([-\d.]+)f\s*,\s*([-\d.]+)f\s*,\s*\(\)\s*=>\s*ModSettings\.(\w+)\.value/g;
			for (const item of featureSource.matchAll(withExpression)) {
				const setting = settings.get(item[5]);
				assertFile(setting, 'ModSettings.cs setting ' + item[5]);
				tunables.push({
					key: item[1],
					label: translated(item[1], english, traditional),
					description: translated(item[1] + '.desc', english, traditional),
					setting: setting.key,
					property: item[5],
					type: setting.type,
					defaultValue: setting.defaultValue,
					min: Number(item[2]),
					max: Number(item[3]),
					step: Number(item[4]),
				});
			}
			const sourceSection = sectionKey[1].slice(4);
			const groupMap = {
				citizens: 'citizens', emergency: 'emergency', sanitation: 'sanitation', road: 'road', rail: 'rail',
				aviation: 'aviationShipping', shipping: 'aviationShipping',
			};
			let group = groupMap[sourceSection] || 'publicTransport';
			if (featureKey === 'feature.aircraftReroute') group = 'aviationShipping';
			if (featureKey === 'feature.emergencyReroute') group = 'emergency';
			if (featureKey === 'feature.laneBottleneck') group = 'road';
			if (featureKey === 'feature.rerouteTiming') group = null;
			const switchSetting = switchProperty ? settings.get(switchProperty) : null;
			features.push({
				key: featureKey,
				id: featureKey.slice('feature.'.length),
				setting: switchSetting?.key ?? null,
				property: switchProperty,
				name: translated(featureKey, english, traditional),
				description: english.has(featureKey + '.desc') || traditional.has(featureKey + '.desc')
					? translated(featureKey + '.desc', english, traditional)
					: null,
				defaultEnabled: switchSetting?.defaultValue ?? null,
				sectionKey: sectionKey[1],
				section: translated(sectionKey[1], english, traditional),
				group,
				status: featureKey === 'feature.aircraftReroute' ? 'known-limitation' : sourceSection === 'experimental' ? 'experimental' : 'implemented',
				tunables,
			});
		}
		sections.push({ key: sectionKey[1], title: translated(sectionKey[1], english, traditional), features });
	}
	return sections;
}

const [localizationSource, settingsSource, settingsUiSource] = await Promise.all([
	readFile(path.join(sourceRoot, 'Localization.cs'), 'utf8'),
	readFile(path.join(sourceRoot, 'ModSettings.cs'), 'utf8'),
	readFile(path.join(sourceRoot, 'SettingsPageUI.cs'), 'utf8'),
]);
const english = readLanguageTable(localizationSource, 'en');
const traditional = readLanguageTable(localizationSource, 'zh-tw');
const settings = parseSettings(settingsSource);
const sections = readSections(settingsUiSource, english, traditional, settings);
const generalFields = [
	{ property: 'LanguageOverride', labelKey: 'lang.label', descriptionKey: null, kind: 'select' },
	{ property: 'VerboseLogging', labelKey: 'tune.verboseLogging', descriptionKey: 'tune.verboseLogging.desc', kind: 'toggle' },
	{ property: 'ShowAdvancedTuning', labelKey: 'tune.showAdvanced', descriptionKey: 'tune.showAdvanced.desc', kind: 'toggle' },
].map((field) => {
	const setting = assertFile(settings.get(field.property), 'SettingsPageUI.cs field ' + field.property);
	return {
		property: field.property,
		key: setting.key,
		kind: field.kind,
		label: translated(field.labelKey, english, traditional),
		description: field.descriptionKey ? translated(field.descriptionKey, english, traditional) : null,
		defaultValue: setting.defaultValue,
	};
});
const languageCodesText = settingsUiSource.match(/LanguageCodes\s*=\s*\{([^}]+)\}/)?.[1] ?? '';
const languageLabelsText = settingsUiSource.match(/LanguageLabels\s*=\s*\{([^}]+)\}/)?.[1] ?? '';
const readStringArray = (text) => [...text.matchAll(/"((?:\\.|[^"\\])*)"/g)].map((match) => unquote('"' + match[1] + '"'));
const languageCodes = readStringArray(languageCodesText);
const languageLabels = readStringArray(languageLabelsText);
assertFile(languageCodes.length === languageLabels.length && languageCodes.length > 0, 'SettingsPageUI.cs language options');
generalFields[0].options = languageCodes.map((code, index) => ({ value: code, label: languageLabels[index] }));
const sourceCommit = process.env.AI_IMPROVE_SOURCE_COMMIT?.trim()
	|| execFileSync('git', ['-c', 'safe.directory=' + sourceRoot, '-C', sourceRoot, 'rev-parse', '--short', 'HEAD'], { encoding: 'utf8' }).trim();
const extractedAt = new Intl.DateTimeFormat('en-CA', { timeZone: 'Asia/Hong_Kong', year: 'numeric', month: '2-digit', day: '2-digit' }).format(new Date());
const output = {
	metadata: {
		sourceCommit,
		extractedAt,
		updatedBy: 'OpenAI-Codex_extractor_worker',
		previousUpdatedBy: 'OpenAI-Codex',
		sourceFiles: ['Localization.cs', 'ModSettings.cs', 'SettingsPageUI.cs'],
		languages: ['en', 'zh-tw'],
		featureCount: sections.reduce((sum, section) => sum + section.features.length, 0),
	},
	generalSettings: generalFields,
	sections,
};
await mkdir(path.dirname(outputPath), { recursive: true });
await writeFile(outputPath, JSON.stringify(output, null, 2) + '\n', 'utf8');
console.log('Extracted ' + output.metadata.featureCount + ' settings cards in ' + sections.length + ' sections from AI Improve ' + sourceCommit + '.');
