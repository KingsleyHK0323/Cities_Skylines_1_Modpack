// Steam Workshop items for each mod. Hidden items are not linked: a visitor who is not the owner
// only sees a Steam error page. Flip `public` once an item is made public on Steam.
// Workshop URLs supplied by Kingsley on 2026-10-26.
// OpenAI-Codex_Responsible: Website
export type Edition = 'release' | 'development';
export interface WorkshopItem { id: string; public: boolean; removed?: boolean; }

export const workshop: Record<string, Record<Edition, WorkshopItem>> = {
	'ai-improve': {
		release: { id: '3782858610', public: true },
		development: { id: '3823575900', public: false, removed: true },
	},
	transport: {
		release: { id: '3823739383', public: true },
		development: { id: '3823739653', public: false, removed: true },
	},
	'ai-gaming': {
		release: { id: '3823740106', public: true },
		development: { id: '3823740277', public: false, removed: true },
	},
};

export const workshopUrl = (id: string) => `https://steamcommunity.com/sharedfiles/filedetails/?id=${id}`;
export const harmonyUrl = workshopUrl('2040656402');

// Development Workshop editions removed per owner update, 2026-11-01. Author: OpenAI-Codex.
