const TOKEN_KEY = 'focusberry_cloud_token';
const EMAIL_KEY = 'focusberry_cloud_email';
const API_KEY = 'focusberry_api_url';
const DEFAULT_API = 'http://localhost:8080/api';
let lastSnapshot = null;
let syncing = false;

const $ = id => document.getElementById(id);
const frame = () => $('app');
const apiBase = () => (localStorage.getItem(API_KEY) || DEFAULT_API).replace(/\/$/, '');

function setStatus(message, kind='') {
  $('status').textContent = message;
  $('status').className = 'status ' + kind;
}
function token() { return localStorage.getItem(TOKEN_KEY); }
function authHeaders(json=false) {
  const h = {};
  if (json) h['Content-Type'] = 'application/json';
  if (token()) h.Authorization = 'Bearer ' + token();
  return h;
}

async function request(path, options={}) {
  const response = await fetch(apiBase() + path, {
    ...options,
    headers: {...authHeaders(!!options.body), ...(options.headers || {})}
  });
  if (!response.ok) {
    let message = response.statusText;
    try { message = (await response.json()).message || message; } catch {}
    throw new Error(message);
  }
  return response.json();
}

function localDb() {
  try {
    return JSON.parse(frame().contentWindow.eval('JSON.stringify(db)'));
  } catch (e) {
    throw new Error('Could not read local Focusberry data: ' + e.message);
  }
}
function setLocalDb(value) {
  const payload = JSON.stringify(value).replace(/\\/g,'\\\\').replace(/`/g,'\\`');
  frame().contentWindow.eval(`db = JSON.parse(\`${payload}\`); save(); render();`);
}
function merge(a,b) {
  if (Array.isArray(a) && Array.isArray(b)) {
    const map = new Map();
    for (const x of a) if (x && x.id != null) map.set(String(x.id), x);
    for (const x of b) if (x && x.id != null) map.set(String(x.id), x);
    return [...map.values()];
  }
  if (a && b && typeof a === 'object' && typeof b === 'object') {
    const out = {...a};
    for (const key of Object.keys(b)) out[key] = key in out ? merge(out[key], b[key]) : b[key];
    return out;
  }
  return b ?? a;
}

async function loginUser() { await authenticate('/auth/login'); }
async function registerUser() { await authenticate('/auth/register'); }

async function authenticate(path) {
  const email = $('email').value.trim().toLowerCase();
  const password = $('password').value;
  if (!email || password.length < 8) return setStatus('Enter a valid email and an 8+ character password.', 'err');
  try {
    setStatus('Connecting…');
    const result = await request(path, {method:'POST', body:JSON.stringify({email,password})});
    localStorage.setItem(TOKEN_KEY, result.token);
    localStorage.setItem(EMAIL_KEY, result.email);
    $('accountEmail').textContent = result.email;
    $('auth').classList.add('hidden');
    $('account').classList.remove('hidden');
    await initialSync();
  } catch (e) { setStatus(e.message || 'Connection failed.', 'err'); }
}

async function initialSync() {
  const local = localDb();
  const cloud = await request('/sync');
  const cloudDb = safeJson(cloud.data);
  const localHasData = hasData(local);
  const cloudHasData = hasData(cloudDb);

  if (!cloudHasData && localHasData) {
    await upload(local);
    setStatus('Cloud sync enabled — local data uploaded.', 'ok');
    return;
  }
  if (cloudHasData && !localHasData) {
    setLocalDb(cloudDb);
    lastSnapshot = JSON.stringify(cloudDb);
    setStatus('Cloud data restored to this device.', 'ok');
    return;
  }
  if (cloudHasData && localHasData) {
    const merged = merge(cloudDb, local);
    setLocalDb(merged);
    await upload(merged);
    setStatus('Synced — local and cloud data merged.', 'ok');
    return;
  }
  setStatus('Cloud sync enabled.', 'ok');
}

function safeJson(value) { try { return JSON.parse(value || '{}'); } catch { return {}; } }
function hasData(db) {
  return !!db && ((db.tasks||[]).length || (db.brainDump||[]).length || Object.keys(db.days||{}).length || Object.keys(db.meds||{}).length || (db.experiments||[]).length);
}

async function upload(data) {
  const version = Date.now();
  const result = await request('/sync', {method:'PUT', body:JSON.stringify({data:JSON.stringify(data),version})});
  lastSnapshot = JSON.stringify(safeJson(result.data));
  return result;
}

async function syncNow() {
  if (!token() || syncing) return;
  syncing = true;
  try {
    const local = localDb();
    const cloud = await request('/sync');
    const cloudDb = safeJson(cloud.data);
    const localJson = JSON.stringify(local);
    if (localJson !== lastSnapshot && cloud.Version > 0) {
      const merged = merge(cloudDb, local);
      setLocalDb(merged);
      await upload(merged);
    } else if (localJson !== lastSnapshot) {
      await upload(local);
    } else if (JSON.stringify(cloudDb) !== localJson && cloud.Version > 0) {
      setLocalDb(cloudDb);
      lastSnapshot = JSON.stringify(cloudDb);
    }
    setStatus('Synced ' + new Date().toLocaleTimeString(), 'ok');
  } catch (e) {
    setStatus('Sync offline: ' + e.message, 'err');
  } finally { syncing = false; }
}

function logout() {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(EMAIL_KEY);
  $('account').classList.add('hidden');
  $('auth').classList.remove('hidden');
  setStatus('Signed out — local data remains on this device.');
}

function saveApiUrl() {
  const value = $('apiUrl').value.trim().replace(/\/$/,'');
  if (!/^https?:\/\//i.test(value)) return setStatus('API URL must start with http:// or https://', 'err');
  localStorage.setItem(API_KEY, value);
  setStatus('API URL saved.');
}

window.addEventListener('load', () => {
  $('apiUrl').value = localStorage.getItem(API_KEY) || DEFAULT_API;
  if (token()) {
    $('auth').classList.add('hidden');
    $('account').classList.remove('hidden');
    $('accountEmail').textContent = localStorage.getItem(EMAIL_KEY) || 'Signed in';
    initialSync().catch(e => setStatus('Cloud unavailable: ' + e.message, 'err'));
  }
  setInterval(syncNow, 5000);
});
