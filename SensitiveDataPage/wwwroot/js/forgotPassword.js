document.getElementById('forgotForm').addEventListener('submit', async function (e) {
    e.preventDefault();
    const msg = document.getElementById('formMessage');
    const data = new URLSearchParams(new FormData(e.target));

    try {
        const resp = await fetch(window.location.pathname, { method: 'POST', body: data });
        const json = await resp.json();

        msg.className = 'alert ' + (json.success ? 'alert-success' : 'alert-danger');
        msg.textContent = t(json.message);

        if (json.success) {
            e.target.querySelector('button[type=submit]').disabled = true;
        }
    } catch {
        msg.className = 'alert alert-danger';
        msg.textContent = t('error.unexpected');
    }
});
