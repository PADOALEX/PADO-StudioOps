/* Adapted from the original HTML-Test_fin.zip Task Board:
 * retains three Kanban dropzones, card interactions, filters, modal editing and toasts.
 * Persistence is entirely API-backed; no localStorage is used.
 */
const $ = (id) => document.getElementById(id);
const state = { csrf: '', user: null, projects: [], selected: null, tasks: [], milestones: [], members: [], employees: [], overview: null, studios: [], tab: 'board', busy: false };
const columns = { todo: $('todoTasks'), 'in-progress': $('progressTasks'), done: $('doneTasks') };
const preciseStatuses = ['To Do', 'In Progress', 'Code Review', 'Testing', 'Done'];
const priorities = ['Low', 'Medium', 'High', 'Critical'];
const projectStatuses = ['Concept', 'Pre-production', 'Production', 'Testing', 'Released', 'Cancelled'];
const milestoneStatuses = ['Planned', 'In Progress', 'Completed', 'Delayed'];
const esc = (value) => String(value ?? '').replace(/[&<>"']/g, (ch) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[ch]));
const fmt = (date) => date ? new Intl.DateTimeFormat('en-GB', { day: '2-digit', month: 'short', year: 'numeric' }).format(new Date(`${date}T12:00:00`)) : 'No deadline';
const canManage = () => state.user?.role === 'Admin' || state.user?.role === 'ProjectManager';
const isAdmin = () => state.user?.role === 'Admin';
const canEdit = (task) => canManage() || (state.user?.role === 'Developer' && task.assignedEmployeeId === state.user.employeeId);
let toastTimer;
let refreshGeneration = 0;
function toast(message) { const t = $('toast'); t.textContent = message; t.classList.add('is-visible'); clearTimeout(toastTimer); toastTimer = setTimeout(() => t.classList.remove('is-visible'), 3000); }
async function csrf() { const res = await fetch('/api/auth/csrf', { credentials: 'same-origin', cache: 'no-store' }); if (!res.ok) throw new Error('Unable to initiate secure session.'); state.csrf = (await res.json()).token; }
async function api(path, options = {}) {
  const method = options.method || 'GET';
  const headers = { ...(options.body !== undefined ? { 'Content-Type': 'application/json' } : {}), ...(method !== 'GET' ? { 'X-CSRF-TOKEN': state.csrf } : {}) };
  const res = await fetch(path, { ...options, method, headers, credentials: 'same-origin', cache: 'no-store' });
  if (res.status === 401) { showLogin(); throw new Error('Your session has expired. Sign in again.'); }
  if (!res.ok) { const error = await res.json().catch(() => ({})); throw new Error(error.error || error.title || `Request failed (${res.status})`); }
  return res.status === 204 ? null : res.json();
}
function showLogin() { state.user = null; $('appView').hidden = true; $('loginView').hidden = false; }
async function startApp() {
  $('appView').hidden = false; $('loginView').hidden = true;
  $('accountName').textContent = state.user.email;
  $('accountRole').textContent = state.user.role;
  $('profileAvatar').textContent = state.user.email.slice(0, 1).toUpperCase();
  $('adminNav').hidden = !isAdmin();
  await loadProjects();
}
async function loadProjects() {
  const before = state.selected;
  state.projects = await api('/api/projects');
  $('projectSelect').replaceChildren();
  for (const project of state.projects) { const opt = document.createElement('option'); opt.value = project.projectId; opt.textContent = `${project.title} · ${project.projectStatus}`; $('projectSelect').append(opt); }
  state.selected = state.projects.find((p) => p.projectId === before)?.projectId || state.projects[0]?.projectId || null;
  $('emptyView').hidden = Boolean(state.selected); $('projectContent').hidden = !state.selected;
  if (state.selected) { $('projectSelect').value = state.selected; await refreshProject(); }
  else { ++refreshGeneration; state.tasks = []; state.milestones = []; state.members = []; state.overview = null; }
  if (state.tab === 'admin') showTab('admin');
}
async function refreshProject() {
  if (!state.selected) return;
  // Each project selection starts a new generation. Old requests may finish,
  // but must never overwrite a newer project's board or progress.
  const generation = ++refreshGeneration;
  state.busy = true; $('refreshBtn').disabled = true;
  try {
    const id = state.selected;
    const [tasks, milestones, members, overview] = await Promise.all([
      api(`/api/projects/${id}/tasks?sort=${encodeURIComponent($('sortSelect').value)}`),
      api(`/api/projects/${id}/milestones`), api(`/api/projects/${id}/members`), api(`/api/projects/${id}/overview`)
    ]);
    if (state.selected !== id || generation !== refreshGeneration) return;
    Object.assign(state, { tasks, milestones, members, overview });
    const project = state.projects.find((p) => p.projectId === id);
    $('projectTitle').textContent = project.title;
    $('projectSubtitle').textContent = `${project.genre}  ·  ${project.studioName}  ·  Target ${fmt(project.plannedReleaseDate)}`;
    $('projectStatusBadge').textContent = project.projectStatus;
    $('metricTotal').textContent = overview.totalTasks;
    $('metricDone').textContent = overview.completedTasks;
    $('metricProgress').textContent = `${overview.completionPercent}%`;
    $('metricMilestones').textContent = overview.openMilestones.length;
    $('progressBar').style.width = `${overview.completionPercent}%`;
    renderBoard(); renderMilestones(); renderTeam(); renderBuilds(); updatePermissions();
  } catch (error) { if (generation === refreshGeneration) toast(error.message); }
  finally { if (generation === refreshGeneration) { state.busy = false; $('refreshBtn').disabled = false; } }
}
function updatePermissions() {
  for (const id of ['createMilestoneBtn', 'addMemberBtn', 'addBuildBtn']) $(id).hidden = !canManage();
  $('createTaskBtn').hidden = state.milestones.length === 0;
}
function filteredTasks() {
  const q = $('taskSearchInput').value.trim().toLowerCase(), priority = $('priorityFilter').value;
  return state.tasks.filter((t) => (!q || t.title.toLowerCase().includes(q) || t.description.toLowerCase().includes(q)) && (!priority || t.priority === priority));
}
function createCard(task) {
  const article = document.createElement('article'); article.className = 'task-card'; article.tabIndex = 0; article.draggable = canEdit(task);
  article.setAttribute('role', 'button'); article.setAttribute('aria-label', `Open task: ${task.title}`);
  const top = document.createElement('div'); top.className = 'task-card-top';
  const p = document.createElement('span'); p.className = `priority-pill priority-${task.priority.toLowerCase()}`; p.textContent = task.priority;
  const handle = document.createElement('span'); handle.textContent = article.draggable ? '⋮⋮' : '◌'; handle.style.color = '#b1becf'; top.append(p, handle);
  const title = document.createElement('h3'); title.textContent = task.title;
  const description = document.createElement('p'); description.className = 'task-card-description'; description.textContent = task.description.slice(0, 120) + (task.description.length > 120 ? '…' : '');
  const footer = document.createElement('div'); footer.className = 'task-card-footer';
  const code = document.createElement('span'); code.textContent = `TASK-${String(task.taskId).padStart(3, '0')}`;
  const status = document.createElement('span'); status.textContent = task.taskStatus + (task.dueDate ? ` · ${fmt(task.dueDate)}` : '');
  footer.append(code, status); article.append(top, title, description, footer);
  article.addEventListener('click', () => editTask(task));
  article.addEventListener('keydown', (e) => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); editTask(task); } });
  article.addEventListener('dragstart', (e) => { article.classList.add('is-dragging'); e.dataTransfer.effectAllowed = 'move'; e.dataTransfer.setData('text/plain', String(task.taskId)); });
  article.addEventListener('dragend', () => { article.classList.remove('is-dragging'); Object.values(columns).forEach((c) => c.classList.remove('is-drag-over')); });
  return article;
}
function renderBoard() {
  const filtered = filteredTasks(); $('filteredCount').textContent = filtered.length;
  for (const [key, el] of Object.entries(columns)) {
    el.replaceChildren(); const items = filtered.filter((t) => t.column === key);
    for (const task of items) el.append(createCard(task));
    if (items.length === 0) { const empty = document.createElement('div'); empty.className = 'empty-column'; empty.textContent = 'No tickets in this stage'; el.append(empty); }
  }
  $('todoCount').textContent = state.tasks.filter((t) => t.column === 'todo').length;
  $('inprogressCount').textContent = state.tasks.filter((t) => t.column === 'in-progress').length;
  $('doneCount').textContent = state.tasks.filter((t) => t.column === 'done').length;
}
async function moveTask(taskId, column) {
  const task = state.tasks.find((t) => t.taskId === Number(taskId));
  if (!task || !canEdit(task) || task.column === column) return;
  const status = column === 'todo' ? 'To Do' : column === 'done' ? 'Done' : 'In Progress';
  try { await api(`/api/projects/${state.selected}/tasks/${task.taskId}/status`, { method: 'PATCH', body: JSON.stringify({ status, revision: task.revision }) }); toast(`Moved to ${status}`); await refreshProject(); }
  catch (error) { toast(error.message); await refreshProject(); }
}
for (const el of Object.values(columns)) {
  el.addEventListener('dragover', (e) => { e.preventDefault(); el.classList.add('is-drag-over'); });
  el.addEventListener('dragleave', (e) => { if (!el.contains(e.relatedTarget)) el.classList.remove('is-drag-over'); });
  el.addEventListener('drop', (e) => { e.preventDefault(); el.classList.remove('is-drag-over'); moveTask(e.dataTransfer.getData('text/plain'), el.dataset.status); });
}
function renderMilestones() {
  const host = $('milestoneList'); host.replaceChildren();
  for (const m of state.milestones) {
    const card = document.createElement('article'); card.className = 'info-card';
    card.innerHTML = `<span class="eyebrow">MILESTONE #${m.milestoneId}</span><h3>${esc(m.name)}</h3><p>${esc(m.milestoneStatus)}</p><span class="meta">Due ${esc(fmt(m.dueDate))}</span>`;
    if (canManage()) { card.style.cursor = 'pointer'; card.title = 'Edit milestone'; card.addEventListener('click', () => editMilestone(m)); }
    host.append(card);
  }
  if (!state.milestones.length) host.textContent = 'No milestones yet. Add a milestone before creating tickets.';
}
function renderTeam() {
  const host = $('teamList'); host.replaceChildren();
  for (const m of state.members) {
    const card = document.createElement('article'); card.className = 'info-card';
    card.innerHTML = `<span class="eyebrow">${esc(m.roleInProject)}</span><h3>${esc(m.firstName)} ${esc(m.lastName)}</h3><p>${esc(m.position)}</p><span class="meta">Joined ${esc(fmt(m.joinedAt))}</span>`; host.append(card);
  }
  if (!state.members.length) host.textContent = 'No team members yet.';
}
function renderBuilds() {
  const host = $('buildList'); host.replaceChildren();
  for (const b of (state.overview?.builds || [])) {
    const card = document.createElement('article'); card.className = 'info-card';
    card.innerHTML = `<span class="eyebrow">${b.isStable ? 'STABLE BUILD' : 'EXPERIMENTAL'}</span><h3>${esc(b.version)}</h3><p>${esc(b.platform)}</p><span class="meta">${esc(new Date(b.buildDate).toLocaleDateString('en-GB'))}</span>`; host.append(card);
  }
  if (!host.children.length) host.textContent = 'No builds recorded for this project.';
}
function showTab(tab) {
  state.tab = tab;
  document.querySelectorAll('.nav-button').forEach((b) => b.classList.toggle('active', b.dataset.tab === tab));
  document.querySelectorAll('[data-panel]').forEach((p) => { p.hidden = p.dataset.panel !== tab; });
  $('projectContent').hidden = tab === 'admin' || !state.selected;
  $('emptyView').hidden = tab === 'admin' || Boolean(state.selected);
  $('crumbTitle').textContent = ({ board: 'Production Board', milestones: 'Milestones', team: 'Project Team', builds: 'Game Builds', admin: 'Administration' })[tab];
}
document.querySelectorAll('.nav-button').forEach((b) => b.addEventListener('click', () => showTab(b.dataset.tab)));
function optionList(entries, selected = null) { return entries.map(([value, label]) => `<option value="${esc(value)}" ${String(value) === String(selected) ? 'selected' : ''}>${esc(label)}</option>`).join(''); }
function field(name, label, type = 'text', value = '', extra = '') {
  const input = type === 'textarea' ? `<textarea name="${name}" ${extra}>${esc(value)}</textarea>` : `<input name="${name}" type="${type}" value="${esc(value)}" ${extra}>`;
  return `<label><span>${esc(label)}</span>${input}</label>`;
}
function selectField(name, label, options, value) { return `<label><span>${esc(label)}</span><select name="${name}">${optionList(options, value)}</select></label>`; }
let onSave = null, onDelete = null;
function openEditor(title, eyebrow, html, save, deleteFn = null) {
  $('formTitle').textContent = title; $('formEyebrow').textContent = eyebrow; $('formFields').innerHTML = html;
  $('formError').hidden = true; onSave = save; onDelete = deleteFn; $('deleteCurrentBtn').hidden = !deleteFn;
  $('saveFormBtn').textContent = 'Save changes';
  $('editorDialog').showModal(); $('formFields').querySelector('input,select,textarea')?.focus();
}
function closeEditor() { $('editorDialog').close(); onSave = null; onDelete = null; }
$('editorForm').addEventListener('submit', async (e) => {
  e.preventDefault();
  if (!onSave) return;
  const values = Object.fromEntries(new FormData($('editorForm')).entries()); $('saveFormBtn').disabled = true; $('formError').hidden = true;
  try { await onSave(values); closeEditor(); toast('Saved to database'); await loadProjects(); }
  catch (error) { $('formError').textContent = error.message; $('formError').hidden = false; }
  finally { $('saveFormBtn').disabled = false; }
});
$('deleteCurrentBtn').addEventListener('click', async () => {
  if (!onDelete || !window.confirm('Delete this record permanently?')) return;
  $('deleteCurrentBtn').disabled = true;
  try { await onDelete(); closeEditor(); toast('Deleted'); await loadProjects(); }
  catch (error) { $('formError').textContent = error.message; $('formError').hidden = false; }
  finally { $('deleteCurrentBtn').disabled = false; }
});
$('closeDialogBtn').addEventListener('click', closeEditor); $('cancelDialogBtn').addEventListener('click', closeEditor);
function memberOptions() { return [['', 'Unassigned'], ...state.members.map((m) => [m.employeeId, `${m.firstName} ${m.lastName}`])].filter(([id]) => canManage() || Number(id) === state.user.employeeId); }
function taskFields(t = {}) {
  const m = state.milestones;
  return field('title', 'Ticket title', 'text', t.title || '', 'maxlength="160" required minlength="3"') +
    field('description', 'Description', 'textarea', t.description || '', 'maxlength="2000"') +
    selectField('milestoneId', 'Milestone', m.map((x) => [x.milestoneId, x.name]), t.milestoneId || m[0]?.milestoneId) +
    selectField('assignedEmployeeId', 'Assignee', memberOptions(), t.assignedEmployeeId ?? (state.user.role === 'Developer' ? state.user.employeeId : '')) +
    selectField('taskStatus', 'Workflow status', preciseStatuses.map((x) => [x, x]), t.taskStatus || 'To Do') +
    selectField('priority', 'Priority', priorities.map((x) => [x, x]), t.priority || 'Medium') +
    field('estimatedHours', 'Estimate (hours)', 'number', t.estimatedHours ?? 1, 'step="0.25" min="0.25" max="99999" required') +
    field('dueDate', 'Deadline (optional)', 'date', t.dueDate || '');
}
function taskPayload(v, revision = undefined) { return { title: v.title.trim(), description: v.description.trim(), milestoneId: Number(v.milestoneId), assignedEmployeeId: v.assignedEmployeeId ? Number(v.assignedEmployeeId) : null, taskStatus: v.taskStatus, priority: v.priority, estimatedHours: Number(v.estimatedHours), dueDate: v.dueDate || null, ...(revision === undefined ? {} : { revision }) }; }
function createTask() {
  if (!state.milestones.length) return toast('Create a milestone first.');
  openEditor('New production ticket', 'NEW TICKET', taskFields(), (v) => api(`/api/projects/${state.selected}/tasks`, { method: 'POST', body: JSON.stringify(taskPayload(v)) }));
}
function editTask(t) {
  const allowed = canEdit(t);
  if (!allowed) { toast(`Read-only ticket · ${t.taskStatus} · ${t.priority}`); return; }
  openEditor(`Edit TASK-${t.taskId}`, 'PRODUCTION TICKET', taskFields(t), (v) => api(`/api/projects/${state.selected}/tasks/${t.taskId}`, { method: 'PUT', body: JSON.stringify(taskPayload(v, t.revision)) }),
    canManage() ? () => api(`/api/projects/${state.selected}/tasks/${t.taskId}`, { method: 'DELETE' }) : null);
}
function milestoneFields(m = {}) { return field('name', 'Milestone name', 'text', m.name || '', 'maxlength="120" required') + field('dueDate', 'Due date', 'date', m.dueDate || '', 'required') + selectField('milestoneStatus', 'Status', milestoneStatuses.map((x) => [x, x]), m.milestoneStatus || 'Planned'); }
function createMilestone() { openEditor('New milestone', 'PROJECT ROADMAP', milestoneFields(), (v) => api(`/api/projects/${state.selected}/milestones`, { method: 'POST', body: JSON.stringify(v) })); }
function editMilestone(m) { openEditor('Edit milestone', 'PROJECT ROADMAP', milestoneFields(m), (v) => api(`/api/projects/${state.selected}/milestones/${m.milestoneId}`, { method: 'PUT', body: JSON.stringify(v) }),
    () => api(`/api/projects/${state.selected}/milestones/${m.milestoneId}`, { method: 'DELETE' })); }
async function addMember() {
  const employees = isAdmin() ? await api('/api/employees') : [];
  if (!isAdmin()) { toast('Ask an administrator to add a studio employee first; managers can add them by ID.'); }
  const options = employees.length ? employees.map((e) => [e.employeeId, `${e.firstName} ${e.lastName}`]) : [];
  const html = options.length ? selectField('employeeId', 'Employee', options) : field('employeeId', 'Existing employee ID', 'number', '', 'min="1" required');
  openEditor('Assign project member', 'TEAM ACCESS', html + field('roleInProject', 'Project role', 'text', 'Developer', 'maxlength="80" required'),
    (v) => api(`/api/projects/${state.selected}/members`, { method: 'POST', body: JSON.stringify({ employeeId: Number(v.employeeId), roleInProject: v.roleInProject }) }));
}
function addBuild() {
  openEditor('Record a game build', 'RELEASE MANAGEMENT', field('version', 'Version', 'text', '', 'maxlength="30" required') + field('platform', 'Platform', 'text', 'Windows', 'maxlength="40" required') +
    selectField('createdByEmployeeId', 'Built by', memberOptions()) + selectField('isStable', 'Release channel', [['false', 'Experimental'], ['true', 'Stable']]),
    (v) => api(`/api/projects/${state.selected}/builds`, { method: 'POST', body: JSON.stringify({ version: v.version, platform: v.platform, createdByEmployeeId: v.createdByEmployeeId ? Number(v.createdByEmployeeId) : null, isStable: v.isStable === 'true' }) }));
}
function addStudio() {
  openEditor('Create a studio', 'ADMINISTRATION', field('name', 'Studio name', 'text', '', 'required maxlength="100"') + field('country', 'Country', 'text', 'Ukraine', 'required maxlength="60"') + field('foundedYear', 'Founded year', 'number', 2026, 'required min="1970" max="2100"') + field('website', 'Website', 'url'),
    (v) => api('/api/studios', { method: 'POST', body: JSON.stringify({ ...v, foundedYear: Number(v.foundedYear), website: v.website || null }) }));
}
async function addEmployee() {
  const studios = await api('/api/studios');
  openEditor('Create employee', 'ADMINISTRATION', selectField('studioId', 'Studio', studios.map((s) => [s.studioId, s.name])) +
    field('firstName', 'First name', 'text', '', 'required') + field('lastName', 'Last name', 'text', '', 'required') + field('email', 'Email', 'email', '', 'required') +
    field('position', 'Position', 'text', 'Gameplay Developer', 'required') + field('hireDate', 'Hire date', 'date', new Date().toISOString().slice(0, 10), 'required') + field('salary', 'Salary (internal only)', 'number', '0', 'min="0" step="0.01" required'),
    (v) => api('/api/employees', { method: 'POST', body: JSON.stringify({ ...v, studioId: Number(v.studioId), salary: Number(v.salary) }) }));
}
async function addProject() {
  const [studios, employees] = await Promise.all([api('/api/studios'), api('/api/employees')]);
  openEditor('Create project', 'ADMINISTRATION', selectField('studioId', 'Studio', studios.map((s) => [s.studioId, s.name])) +
    selectField('projectLeadId', 'Project lead', [['', 'Assign later'], ...employees.map((e) => [e.employeeId, `${e.firstName} ${e.lastName}`])]) +
    field('title', 'Project title', 'text', '', 'required maxlength="120"') + field('genre', 'Genre', 'text', 'Action', 'required maxlength="60"') + selectField('projectStatus', 'Status', projectStatuses.map((x) => [x, x]), 'Concept') +
    field('startDate', 'Start date', 'date', new Date().toISOString().slice(0, 10), 'required') + field('plannedReleaseDate', 'Release date', 'date') + field('budget', 'Budget', 'number', '0', 'min="0" step="0.01" required'),
    (v) => api('/api/projects', { method: 'POST', body: JSON.stringify({ ...v, studioId: Number(v.studioId), projectLeadId: v.projectLeadId ? Number(v.projectLeadId) : null, plannedReleaseDate: v.plannedReleaseDate || null, budget: Number(v.budget) }) }));
}
async function addAccount() {
  const employees = await api('/api/employees');
  openEditor('Provision account', 'SECURITY / RBAC', field('email', 'Login email', 'email', '', 'required') + field('password', 'Password (12+ characters)', 'password', '', 'minlength="12" required') +
    selectField('role', 'Account role', [['Developer', 'Developer'], ['ProjectManager', 'Project Manager'], ['Admin', 'Admin']]) + selectField('employeeId', 'Linked employee', [['', 'None (Admin only)'], ...employees.map((e) => [e.employeeId, `${e.firstName} ${e.lastName}`])]),
    (v) => api('/api/admin/users', { method: 'POST', body: JSON.stringify({ email: v.email, password: v.password, role: v.role, employeeId: v.employeeId ? Number(v.employeeId) : null }) }));
}
$('createTaskBtn').addEventListener('click', createTask); $('createMilestoneBtn').addEventListener('click', createMilestone); $('addMemberBtn').addEventListener('click', () => addMember().catch((e) => toast(e.message)));
$('addBuildBtn').addEventListener('click', addBuild); $('addStudioBtn').addEventListener('click', addStudio);
$('addEmployeeBtn').addEventListener('click', () => addEmployee().catch((e) => toast(e.message)));
$('addProjectBtn').addEventListener('click', () => addProject().catch((e) => toast(e.message)));
$('addAccountBtn').addEventListener('click', () => addAccount().catch((e) => toast(e.message)));
$('refreshBtn').addEventListener('click', refreshProject);
$('projectSelect').addEventListener('change', async () => { state.selected = Number($('projectSelect').value); await refreshProject(); });
$('taskSearchInput').addEventListener('input', renderBoard); $('priorityFilter').addEventListener('change', renderBoard);
$('sortSelect').addEventListener('change', refreshProject);
$('logoutBtn').addEventListener('click', async () => { try { await api('/api/auth/logout', { method: 'POST' }); } catch (e) { toast(e.message); } showLogin(); await csrf().catch(() => {}); });
$('loginForm').addEventListener('submit', async (e) => {
  e.preventDefault(); const fd = new FormData(e.currentTarget); const btn = e.currentTarget.querySelector('button[type="submit"]');
  btn.disabled = true; $('loginError').hidden = true;
  try { await api('/api/auth/login', { method: 'POST', body: JSON.stringify({ email: fd.get('email'), password: fd.get('password') }) }); e.currentTarget.reset(); state.user = await api('/api/auth/me'); await startApp(); }
  catch (err) { $('loginError').textContent = err.message; $('loginError').hidden = false; }
  finally { btn.disabled = false; }
});
(async () => {
  try { await csrf(); const me = await fetch('/api/auth/me', { credentials: 'same-origin' }); if (me.ok) { state.user = await me.json(); await startApp(); } else showLogin(); }
  catch (err) { $('loginError').textContent = err.message; $('loginError').hidden = false; }
})();
