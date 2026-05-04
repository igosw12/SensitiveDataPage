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

        msg.className = 'alert alert-danger';
        msg.textContent = t(json.message);
    } catch {
        msg.className = 'alert alert-danger';
        msg.textContent = t('error.unexpected');
    }
});
