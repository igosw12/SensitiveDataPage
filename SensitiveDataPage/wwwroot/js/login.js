document.getElementById('loginForm').addEventListener('submit', async function (e) {
    e.preventDefault();
    const msg = document.getElementById('formMessage');
    const data = new URLSearchParams(new FormData(e.target));

    try {
        const resp = await fetch(window.location.pathname, { method: 'POST', body: data });
        const json = await resp.json();

        if (json.success) {
            window.location.href = '/Dashboard';
            return;
        }

        if (json.twoFactorRequired) {
            document.getElementById('loginForm').classList.add('d-none');
            document.getElementById('twoFactorForm').classList.remove('d-none');
            msg.className = 'alert alert-info';
            msg.textContent = t(json.message);
            return;
        }

        msg.className = 'alert alert-danger';
        msg.textContent = t(json.message);
    } catch {
        msg.className = 'alert alert-danger';
        msg.textContent = t('error.unexpected');
    }
});

document.getElementById('twoFactorForm').addEventListener('submit', async function (e) {
    e.preventDefault();
    const msg = document.getElementById('formMessage');
    const data = new URLSearchParams(new FormData(e.target));

    try {
        const resp = await fetch(window.location.pathname + '?handler=VerifyTwoFactor', { method: 'POST', body: data });
        const json = await resp.json();

        if (json.success) {
            window.location.href = '/Dashboard';
            return;
        }

        msg.className = 'alert alert-danger';
        msg.textContent = t(json.message);
    } catch {
        msg.className = 'alert alert-danger';
        msg.textContent = t('error.unexpected');
    }
});
