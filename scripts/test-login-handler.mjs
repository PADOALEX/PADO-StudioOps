import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { Script, createContext } from 'node:vm';
import { fileURLToPath } from 'node:url';

// Exercise the actual browser handler with the DOM event lifetime: currentTarget
// is available during dispatch, then null while asynchronous login work resumes.
const file = fileURLToPath(new URL('../src/PADO.StudioOps.Api/wwwroot/js/board.js', import.meta.url));
const source = readFileSync(file, 'utf8');
const begin = source.indexOf("$('loginForm').addEventListener('submit'");
const end = source.indexOf('\n(async () =>', begin);
assert.ok(begin >= 0 && end > begin, 'Actual login handler must be present');

async function scenario(failLogin) {
  let listener, resets = 0, starts = 0;
  const button = { disabled: false };
  const error = { hidden: true, textContent: '' };
  const form = {
    querySelector: () => button,
    reset: () => { resets++; },
    addEventListener: (_name, fn) => { listener = fn; },
  };
  const state = {};
  const context = createContext({
    $: id => id === 'loginForm' ? form : error,
    FormData: class { constructor(value) { assert.equal(value, form); } get(key) { return key === 'email' ? 'synthetic@example.test' : 'test-only'; } },
    csrf: async () => {},
    api: async path => {
      if (failLogin && path.endsWith('/login')) throw new Error('Invalid credentials');
      return { role: 'Admin' };
    },
    state,
    startApp: async () => { starts++; },
  });
  new Script(source.slice(begin, end)).runInContext(context);
  const event = { currentTarget: form, preventDefault() {} };
  const done = listener(event);
  event.currentTarget = null;
  await done;
  assert.equal(button.disabled, false);
  assert.equal(starts, failLogin ? 0 : 1);
  assert.equal(resets, failLogin ? 0 : 1);
  assert.equal(error.hidden, !failLogin);
  if (failLogin) assert.equal(error.textContent, 'Invalid credentials');
  else assert.equal(state.user.role, 'Admin');
}
await scenario(false);
await scenario(true);
console.log('PASS: asynchronous successful login and failed-login recovery');
