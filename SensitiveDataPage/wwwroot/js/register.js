var onloadCallback = function () {
    if (typeof grecaptcha !== 'undefined' && document.getElementById('html_element')) {
        grecaptcha.render('html_element', { sitekey: document.getElementById('html_element').dataset.sitekey });
    }
};

document.getElementById('registerForm').addEventListener('submit', async function (e) {
    e.preventDefault();
    var token = (typeof grecaptcha !== 'undefined') ? grecaptcha.getResponse() : '';
    document.getElementById('recaptchaTokenInputId').value = token;

    const msg = document.getElementById('formMessage');
    const data = new URLSearchParams(new FormData(e.target));

    try {
        const resp = await fetch(window.location.pathname, { method: 'POST', body: data });
        const json = await resp.json();

        msg.className = 'alert ' + (json.success ? 'alert-success' : 'alert-danger');
        msg.textContent = t(json.message);

        if (json.success) {
            setTimeout(() => window.location.href = '/Login', 2500);
        }
    } catch {
        msg.className = 'alert alert-danger';
        msg.textContent = t('error.unexpected');
    }
});
