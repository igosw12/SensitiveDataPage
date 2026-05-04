document.getElementById('resetForm').addEventListener('submit', async function (e) {
    e.preventDefault();
    const msg = document.getElementById('formMessage');
    const data = new URLSearchParams(new FormData(e.target));

    try {
        const resp = await fetch(window.location.href, { method: 'POST', body: data });
        const json = await resp.json();

        msg.className = 'alert ' + (json.success ? 'alert-success' : 'alert-danger');
        msg.textContent = t(json.message);

        if (json.success) {
            e.target.querySelector('button[type=submit]').disabled = true;
            setTimeout(() => window.location.href = '/Login', 2500);
        }
    } catch {
        msg.className = 'alert alert-danger';
        msg.textContent = t('error.unexpected');
    }
});
