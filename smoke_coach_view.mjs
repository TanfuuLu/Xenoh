import https from 'https';
const BASE = 'https://localhost:7017';

function req(method, path, body, token) {
  return new Promise((resolve, reject) => {
    const payload = body ? JSON.stringify(body) : null;
    const headers = { 'Content-Type': 'application/json' };
    if (token) headers['Authorization'] = `Bearer ${token}`;
    if (payload) headers['Content-Length'] = Buffer.byteLength(payload);
    const url = new URL(`${BASE}${path}`);
    const r = https.request({ hostname: url.hostname, port: url.port, path: url.pathname + url.search, method, headers, rejectUnauthorized: false }, res => {
      let d = ''; res.on('data', c => d += c);
      res.on('end', () => { try { resolve({ status: res.statusCode, body: JSON.parse(d) }); } catch { resolve({ status: res.statusCode, body: d }); } });
    });
    r.on('error', reject); if (payload) r.write(payload); r.end();
  });
}

const ok   = (label, res) => console.log(`  ✅ [${res.status}] ${label}`);
const fail = (label, res) => console.log(`  ❌ [${res.status}] ${label} → ${JSON.stringify(res.body?.message ?? res.body).substring(0, 100)}`);
const check = (label, res, expectStatus) => res.status === expectStatus ? ok(label, res) : fail(label, res);

console.log('\n=== Coach View Plan Test ===\n');

// Dùng accounts từ smoke_permissions (đã có relationship Accepted)
const cL = await req('POST', '/api/auth/login', { email: 'perm.coach@xenoh.com',  password: 'Test123!' });
const uL = await req('POST', '/api/auth/login', { email: 'perm.client@xenoh.com', password: 'Test123!' });
const ct = cL.body.accessToken;
const ut = uL.body.accessToken;
console.log('Coach login:', cL.status === 200 ? 'OK' : 'FAIL');
console.log('Client login:', uL.status === 200 ? 'OK' : 'FAIL');

// Lấy danh sách clients của coach → tìm clientId
const clients = await req('GET', '/api/coach-client/my-clients', null, ct);
console.log('Clients:', clients.body.map?.(c => `${c.fullName}(${c.status})`).join(', '));
const clientId = clients.body.find?.(c => c.status === 'Accepted')?.clientId;

if (!clientId) { console.log('❌ No accepted client found'); process.exit(1); }

// Coach tạo plan mới cho client (tạo mới để test clean)
const plan = await req('POST', '/api/plans/for-user', {
  userId: clientId, name: 'View Test Plan',
  startDate: '2026-05-05', endDate: '2026-05-18'
}, ct);
console.log(`\nPlan: "${plan.body.name}" type=${plan.body.planType} status=${plan.status}`);
const planId = plan.body.id;

// Template
const tmpl = await req('GET', '/api/exercise-templates?muscleGroup=Chest', null, ct);
const tmplId = tmpl.body[0].id;

// ── Coach xem plan ──────────────────────────────────────
console.log('\n--- Coach xem plan tạo cho client ---');
const rPlan = await req('GET', `/api/plans/${planId}`, null, ct);
check('GET /plans/:id', rPlan, 200);

const rWeeks = await req('GET', `/api/plans/${planId}/weeks`, null, ct);
check('GET /plans/:id/weeks', rWeeks, 200);
if (rWeeks.status === 200) console.log(`         → ${rWeeks.body.length} tuần`);

const weekId = rWeeks.body[0]?.id;
const rDays = await req('GET', `/api/weeks/${weekId}/days`, null, ct);
check('GET /weeks/:id/days', rDays, 200);
if (rDays.status === 200) console.log(`         → ${rDays.body.length} ngày`);

const dayId = rDays.body[0]?.id;
const rEx0 = await req('GET', `/api/exercises/by-day/${dayId}`, null, ct);
check('GET /exercises/by-day/:id (rỗng)', rEx0, 200);

// Coach thêm exercise
await req('POST', '/api/exercises', { dailyWorkoutId: dayId, exerciseTemplateId: tmplId, plannedSets: 3, plannedReps: 10 }, ct);
const rEx1 = await req('GET', `/api/exercises/by-day/${dayId}`, null, ct);
check('GET /exercises/by-day/:id (sau khi thêm)', rEx1, 200);
if (rEx1.status === 200) console.log(`         → ${rEx1.body.length} exercises: "${rEx1.body[0]?.name}"`);

// ── Client xem được ─────────────────────────────────────
console.log('\n--- Client xem plan của mình (được phép) ---');
check('GET weeks (client)',    await req('GET', `/api/plans/${planId}/weeks`, null, ut), 200);
check('GET days (client)',     await req('GET', `/api/weeks/${weekId}/days`, null, ut), 200);
check('GET exercises (client)',await req('GET', `/api/exercises/by-day/${dayId}`, null, ut), 200);

// Client mark set done
const setId = rEx1.body[0]?.sets?.[0]?.id;
check('Client mark set done',  await req('PATCH', `/api/exercises/sets/${setId}/complete`, { setId }, ut), 200);

// ── Client không sửa được ───────────────────────────────
console.log('\n--- Client không sửa cấu trúc (bị chặn) ---');
const exId = rEx1.body[0]?.id;
check('Client sửa exercise → 400', await req('PUT', `/api/exercises/${exId}`, { exerciseId: exId, plannedReps: 99 }, ut), 400);
check('Client thêm exercise → 400', await req('POST', '/api/exercises', { dailyWorkoutId: dayId, exerciseTemplateId: tmplId, plannedSets: 2, plannedReps: 5 }, ut), 400);

console.log('\n✅ Done!\n');
