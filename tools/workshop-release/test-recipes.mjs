// Signature: OpenAI-Codex_workshop_implementation
// Updated by: OpenAI-Codex.
import test from 'node:test';
import assert from 'node:assert/strict';
import {registry} from './request.mjs';
let library;
try { library = await import('./recipes.mjs'); } catch (error) {
  if (error.code !== 'ERR_MODULE_NOT_FOUND') throw error;
}
test('the reviewed compiler recipe registry is implemented', () => {
  assert.ok(library, 'the reviewed compiler recipe registry has not been implemented');
});

test('each reviewed recipe has an independent module, exact SHA and fixed source list', () => {
  assert.equal(library.recipes.length, 12);
  for (const recipe of library.recipes) {
    const registered = registry.find(item => item.key === recipe.module);
    assert.ok(registered);
    assert.equal(recipe.sourceSha, registered.reviewedSource);
    assert.equal(recipe.assembly, registered.assembly);
    assert.ok(recipe.sources.length > 0);
    assert.equal(new Set(recipe.sources).size, recipe.sources.length);
  }
});

test('the vehicle management candidate uses its reviewed cloud-built source and assembly metadata', () => {
  const item = library.recipes.find(recipe => recipe.module === 'Automated_Public_Transport_Vehicle_Management');
  assert.equal(item.sourceSha, 'afeeb21571a7ce5a496b892a4e55388d9463c968');
  assert.equal(item.languageVersion, '5');
  assert.ok(item.sources.includes('Properties/AssemblyInfo.cs'));
  assert.ok(item.sources.includes('Source/Lifecycle/CitySession.cs'));
  assert.equal(item.references.length, 7);
});

test('unreviewed compiler options, paths, duplicate modules and game-reference names are rejected', () => {
  const fixture = () => structuredClone({schema:1,signature:'OpenAI-Codex_workshop_implementation',recipes:library.recipes});
  for (const mutate of [
    data => {data.recipes[0].sources.push('../other.cs');},
    data => {data.recipes[0].sources.push('Source/*.cs');},
    data => {data.recipes[0].compilerOptions = ['/define:DEVTOOLS'];},
    data => {data.recipes[0].references.push('malicious.dll');},
    data => {data.recipes.push(structuredClone(data.recipes[0]));},
    data => {data.recipes[0].sourceSha = 'bad';},
  ]) {
    const data = fixture(); mutate(data); assert.throws(() => library.validateRecipes(data));
  }
});
