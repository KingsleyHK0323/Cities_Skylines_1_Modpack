// Signature: OpenAI-Codex_workshop_implementation
import test from 'node:test';
import assert from 'node:assert/strict';
let steam;
try { steam = await import('./steam.mjs'); } catch (error) {
  if (error.code !== 'ERR_MODULE_NOT_FOUND') throw error;
}
test('the Steam preflight is implemented', () => {
  assert.ok(steam, 'the Steam preflight has not been implemented');
});

const testOwner = '76561198000000000';
const good = {response:{result:1,resultcount:1,publishedfiledetails:[{
  publishedfileid:'3782858610',creator:testOwner,consumer_app_id:255710,result:1,
}]}};

test('Steam preflight uses one official request for the registered item and checks the owner and game', async () => {
  let call;
  const result = await steam.queryRegisteredItem('AI_Improve', async (url, options) => {
    call = {url, options}; return new Response(JSON.stringify(good), {status:200});
  }, testOwner);
  assert.equal(result, true);
  assert.equal(call.url, 'https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/');
  assert.equal(call.options.method, 'POST');
  assert.equal(call.options.redirect, 'error');
  const input = new URLSearchParams(call.options.body);
  assert.equal(input.get('itemcount'), '1');
  assert.equal(input.get('publishedfileids[0]'), '3782858610');
});

test('a second reviewed module has an independent fixed Steam item identity', async () => {
  const wireless = {response:{result:1,resultcount:1,publishedfiledetails:[{
    publishedfileid:'3832301115',creator:testOwner,consumer_app_id:255710,result:1,
  }]}};
  let id;
  assert.equal(await steam.queryRegisteredItem('Wireless_Utilities_And_Global_Coverage', async (_url, options) => {
    id = new URLSearchParams(options.body).get('publishedfileids[0]');
    return new Response(JSON.stringify(wireless), {status:200});
  }, testOwner), true);
  assert.equal(id, '3832301115');
  await assert.rejects(steam.queryRegisteredItem('Wireless_Utilities_And_Global_Coverage', async () =>
    new Response(JSON.stringify(good), {status:200}), testOwner));
});

test('private, missing, wrong owner/game, malformed and rate-limited responses fail without retry', async () => {
  const responses = [
    {response:{result:1,resultcount:1,publishedfiledetails:[{publishedfileid:'3782858610',result:9}]}},
    {response:{result:1,resultcount:1,publishedfiledetails:[{...good.response.publishedfiledetails[0],creator:'1'}]}},
    {response:{result:1,resultcount:1,publishedfiledetails:[{...good.response.publishedfiledetails[0],consumer_app_id:42}]}},
    {response:{result:1,resultcount:0,publishedfiledetails:[]}},
    {response:{result:1,resultcount:2,publishedfiledetails:good.response.publishedfiledetails}},
  ];
  for (const data of responses) {
    let calls = 0;
    await assert.rejects(steam.queryRegisteredItem('AI_Improve', async () => {
      calls++; return new Response(JSON.stringify(data), {status:200});
    }, testOwner));
    assert.equal(calls, 1);
  }
  let calls = 0;
  await assert.rejects(steam.queryRegisteredItem('AI_Improve', async () => {
    calls++; return new Response('rate limited', {status:429});
  }, testOwner));
  assert.equal(calls, 1);
  await assert.rejects(steam.queryRegisteredItem('AI_Gaming', async () => { throw new Error('must not request'); }, testOwner));
});

test('a missing or malformed private owner setting blocks before any network request', async () => {
  for (const owner of ['', '123x', '123456789012345678901']) {
    let called = false;
    await assert.rejects(steam.queryRegisteredItem('AI_Improve', async () => {called = true;}, owner));
    assert.equal(called, false);
  }
});

// Additional validation: OpenAI-Codex.
