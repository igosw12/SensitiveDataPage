const categoryFields = {
    Account: [
        { id: 'login', label: 'Login / Email', type: 'text' },
        { id: 'password', label: 'Password', type: 'password' },
        { id: 'website', label: 'Website', type: 'url' }
    ],
    Address: [
        { id: 'street', label: 'Street Address', type: 'text' },
        { id: 'city', label: 'City', type: 'text' },
        { id: 'postalCode', label: 'Postal Code', type: 'text' },
        { id: 'country', label: 'Country', type: 'text' }
    ],
    Card: [
        { id: 'cardNumber', label: 'Card Number', type: 'text' },
        { id: 'cardholderName', label: 'Cardholder Name', type: 'text' },
        { id: 'expiry', label: 'Expiry Date (MM/YY)', type: 'text' },
        { id: 'cvv', label: 'CVV', type: 'password' }
    ],
    Phone: [
        { id: 'phoneNumber', label: 'Phone Number', type: 'tel' },
        { id: 'carrier', label: 'Carrier', type: 'text' },
        { id: 'country', label: 'Country', type: 'text' }
    ],
    Documents: [
        { id: 'documentType', label: 'Document Type', type: 'text' },
        { id: 'number', label: 'Document Number', type: 'text' },
        { id: 'issuer', label: 'Issuer', type: 'text' },
        { id: 'expiry', label: 'Expiry Date', type: 'date' }
    ],
    Others: [
        { id: 'key', label: 'Key', type: 'text' },
        { id: 'value', label: 'Value', type: 'text' }
    ]
};

let currentTier = null;
let allEntries = [];

function token() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
}

async function post(handler, body) {
    const res = await fetch(`?handler=${handler}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token() },
        body: JSON.stringify(body)
    });
    return res.json();
}

async function loadTier(tier, name) {
    currentTier = tier;
    document.getElementById('tier-title').textContent = name;
    document.getElementById('input-type').value = '';
    document.getElementById('input-category').value = '';
    document.getElementById('dynamic-fields').innerHTML = '';
    document.getElementById('tier-panel').style.display = 'block';
    await refreshEntries();
}

async function refreshEntries() {
    const res = await fetch(`?handler=Entries&tier=${currentTier}`, {
        headers: { 'X-Requested-With': 'XMLHttpRequest' }
    });
    allEntries = await res.json();
    filterAndRender();
}

function filterAndRender() {
    const query = (document.getElementById('search-input')?.value ?? '').toLowerCase().trim();
    const cat = document.getElementById('search-category')?.value ?? '';
    const data = allEntries.filter(e => {
        const matchCat = !cat || e.category === cat;
        const matchQuery = !query || e.type.toLowerCase().includes(query) || e.category.toLowerCase().includes(query);
        return matchCat && matchQuery;
    });
    renderTable(data);
}

function renderTable(data) {
    document.getElementById('entries-body').innerHTML = data.map(e => `
        <tr data-id="${e.id}" data-category="${escapeAttr(e.category)}" data-fields='${escapeAttr(JSON.stringify(e.fields))}'>
            <td>${escapeHtml(e.type)}</td>
            <td>${escapeHtml(e.category)}</td>
            <td><div class="entry-fields blurred">${renderFieldsHtml(e.category, e.fields)}</div></td>
            <td>${new Date(e.createdAt).toLocaleString()}</td>
            <td class="entry-actions">
                <button class="icon-btn" onclick="toggleBlur(this)" title="Show/Hide"><i class="bi bi-eye"></i></button>
                <button class="icon-btn" onclick="editRow(this)" title="Edit"><i class="bi bi-pencil"></i></button>
                <button class="icon-btn text-danger" onclick="deleteRow(this)" title="Delete"><i class="bi bi-trash"></i></button>
            </td>
        </tr>`).join('');
}

function renderFieldsHtml(category, fields) {
    return (categoryFields[category] || [])
        .map(f => `<div><strong>${escapeHtml(f.label)}:</strong> ${escapeHtml(fields[f.id] ?? '')}</div>`)
        .join('');
}

function renderDynamicFields(category, containerId, existingData) {
    const container = document.getElementById(containerId);
    const fields = categoryFields[category] || [];
    container.innerHTML = fields.map(f => `
        <div class="mb-2">
            <label class="field-label">${escapeHtml(f.label)}</label>
            <input id="field-${f.id}" name="${f.id}" class="form-control" type="${f.type}"
                   value="${existingData ? escapeAttr(existingData[f.id] ?? '') : ''}" />
        </div>`).join('');
}

function toggleBlur(btn) {
    const div = btn.closest('tr').querySelector('.entry-fields');
    const isBlurred = div.classList.toggle('blurred');
    btn.innerHTML = `<i class="bi bi-eye${isBlurred ? '' : '-slash'}"></i>`;
}

function editRow(btn) {
    const tr = btn.closest('tr');
    const id = tr.dataset.id;
    const category = tr.dataset.category;
    const fields = JSON.parse(tr.dataset.fields);

    renderDynamicFields(category, 'dynamic-fields', fields);
    document.getElementById('input-category').value = category;

    const addForm = document.getElementById('add-form');
    addForm.dataset.editId = id;
    addForm.querySelector('.submitBtn').textContent = t('dash.saveBtn') || 'Save';
}

async function deleteRow(btn) {
    if (!confirm(t('dash.confirmDelete'))) return;
    const id = btn.closest('tr').dataset.id;
    const res = await post('DeleteData', { id });
    if (res.success) await refreshEntries();
    else alert(t('dash.errorDelete'));
}

function openSettings() {
    document.getElementById('settingsModal').style.display = 'block';
}

function closeSettings() {
    document.getElementById('settingsModal').style.display = 'none';
}

function openDeleteConfirm() {
    closeSettings();
    document.getElementById('deleteConfirmModal').style.display = 'block';
}

function closeDeleteConfirm() {
    document.getElementById('deleteConfirmModal').style.display = 'none';
}

function openChangePassword() {
    closeSettings();
    document.getElementById('cp-old').value = '';
    document.getElementById('cp-new').value = '';
    document.getElementById('cp-confirm').value = '';
    document.getElementById('cp-match-hint').style.display = 'none';
    document.getElementById('changePasswordAlert').style.display = 'none';
    document.getElementById('changePasswordModal').style.display = 'block';
}

function closeChangePassword() {
    document.getElementById('changePasswordModal').style.display = 'none';
}

function validateConfirm() {
    const np = document.getElementById('cp-new').value;
    const cp = document.getElementById('cp-confirm').value;
    document.getElementById('cp-match-hint').style.display = (cp && np !== cp) ? 'block' : 'none';
}

function showChangePasswordAlert(message, type) {
    const el = document.getElementById('changePasswordAlert');
    el.textContent = message;
    el.className = `modal-alert alert alert-${type}`;
    el.style.display = 'block';
}

async function submitChangePassword() {
    const oldPwd = document.getElementById('cp-old').value.trim();
    const newPwd = document.getElementById('cp-new').value.trim();
    const confirmPwd = document.getElementById('cp-confirm').value.trim();

    if (!oldPwd || !newPwd || !confirmPwd) {
        showChangePasswordAlert(t('dash.allFieldsRequired'), 'danger');
        return;
    }
    if (newPwd !== confirmPwd) {
        showChangePasswordAlert(t('dash.passwordMismatch'), 'danger');
        return;
    }
    if (!/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$/.test(newPwd)) {
        showChangePasswordAlert(t('dash.passwordRule'), 'danger');
        return;
    }

    const res = await post('ChangePassword', { oldPassword: oldPwd, newPassword: newPwd, confirmPassword: confirmPwd });
    if (res.success) {
        showChangePasswordAlert(t('dash.changePassSuccess'), 'success');
        document.getElementById('cp-old').value = '';
        document.getElementById('cp-new').value = '';
        document.getElementById('cp-confirm').value = '';
    } else {
        showChangePasswordAlert(res.message ?? t('dash.changePassError'), 'danger');
    }
}

async function toggle2fa(checkbox) {
    const res = await post('Toggle2fa', { enabled: checkbox.checked });
    if (!res.success) checkbox.checked = !checkbox.checked;
}

function escapeHtml(str) {
    return String(str ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

function escapeAttr(str) {
    return String(str ?? '')
        .replace(/&/g, '&amp;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

async function submitAddForm(btn) {
    const type = document.getElementById('input-type').value.trim();
    const category = document.getElementById('input-category').value;
    if (!type || !category) return;

    const fields = {};
    document.getElementById('dynamic-fields').querySelectorAll('input').forEach(input => {
        fields[input.name] = input.value;
    });

    const form = document.getElementById('add-form');
    const editId = form.dataset.editId;
    let res;

    if (editId) {
        res = await post('UpdateData', { id: editId, fields });
    } else {
        res = await post('SaveData', { tier: currentTier, type, category, fields });
    }

    if (res.success) {
        document.getElementById('input-type').value = '';
        document.getElementById('input-category').value = '';
        document.getElementById('dynamic-fields').innerHTML = '';
        delete form.dataset.editId;
        btn.textContent = t('dash.addBtn') || 'Add';
        await refreshEntries();
    } else {
        alert(res.message ?? t('dash.errorSave'));
    }
}
