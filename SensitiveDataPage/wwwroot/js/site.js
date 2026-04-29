// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

window.i18n = {
    en: {
        'login.title': 'Login into Sensitive Data Manager',
        'login.btn': 'Login',
        'login.forgot': 'Forgot password?',
        'login.join': 'Join us!',
        'login.register': 'Create new account',
        'login.email': 'Email',
        'login.password': 'Password',
        'reg.title': 'Create a new account and protect your sensitive data',
        'reg.btn': 'Register',
        'reg.email': 'Email',
        'reg.password': 'Password',
        'reg.confirmPassword': 'Confirm password',
        'reg.birthday': 'Birthday',
        'reg.emailPh': 'Email',
        'reg.passwordPh': 'Password',
        'reg.confirmPh': 'Confirm password',
        'dash.choice': 'Selection',
        'dash.tier1': 'Confidential Data',
        'dash.tier2': 'Sensitive Data',
        'dash.tier3': 'Public Data',
        'dash.tier4': 'Custom Data',
        'dash.addBtn': 'Add',
        'dash.entryName': 'Entry name (e.g. Gmail Account)',
        'dash.selectCategory': '-- Choose category --',
        'dash.colName': 'Name',
        'dash.colCategory': 'Category',
        'dash.colFields': 'Fields',
        'dash.colAdded': 'Added',
        'dash.settings': 'Account settings \u2699',
        'dash.logout': 'Logout \u23FB',
        'dash.changePass': 'Change password',
        'dash.deleteAcc': 'Delete account',
        'dash.settingsTitle': 'Account settings',
        'dash.chooseCategory': 'Choose category.',
        'dash.confirmDelete': 'Are you sure you want to delete this entry?',
        'dash.errorUpdate': 'Error updating data.',
        'dash.errorDelete': 'Error deleting data.',
        'dash.errorSave': 'Error saving data.',
        'dash.searchPh': 'Search by name or category...',
        'dash.allCategories': 'All categories',
        'dash.search': 'Search',
        'forgot.title': 'Reset your password',
        'forgot.email': 'Email',
        'forgot.emailPh': 'Email',
        'forgot.btn': 'Send reset link',
        'forgot.remember': 'Remember your password?',
        'forgot.backLogin': 'Back to login',
        'reset.title': 'Create a new password',
        'reset.password': 'New Password',
        'reset.passwordPh': 'New password',
        'reset.confirmPassword': 'Confirm Password',
        'reset.confirmPh': 'Confirm password',
        'reset.btn': 'Reset Password',
        'reset.backQ': 'Back to login?',
        'reset.backLogin': 'Go to login',
        'reset.unable': 'Unable to reset password',
        'reset.requestNew': 'Request new reset link',
    },
    pl: {
        'login.title': 'Zaloguj się do Menedżera Danych Wrażliwych',
        'login.btn': 'Zaloguj',
        'login.forgot': 'Zapomniałeś hasła?',
        'login.join': 'Dołącz do nas!',
        'login.register': 'Utwórz nowe konto',
        'login.email': 'E-mail',
        'login.password': 'Hasło',
        'reg.title': 'Utwórz nowe konto i chroń swoje dane wrażliwe',
        'reg.btn': 'Zarejestruj',
        'reg.email': 'E-mail',
        'reg.password': 'Hasło',
        'reg.confirmPassword': 'Potwierdź hasło',
        'reg.birthday': 'Data urodzenia',
        'reg.emailPh': 'E-mail',
        'reg.passwordPh': 'Hasło',
        'reg.confirmPh': 'Potwierdź hasło',
        'dash.choice': 'Wybór',
        'dash.tier1': 'Dane poufne',
        'dash.tier2': 'Dane wrażliwe',
        'dash.tier3': 'Dane publiczne',
        'dash.tier4': 'Niestandardowe dane',
        'dash.addBtn': 'Dodaj',
        'dash.entryName': 'Nazwa wpisu (np. Konto Gmail)',
        'dash.selectCategory': '-- Wybierz kategorię --',
        'dash.colName': 'Nazwa',
        'dash.colCategory': 'Kategoria',
        'dash.colFields': 'Pola',
        'dash.colAdded': 'Dodano',
        'dash.settings': 'Ustawienia konta \u2699',
        'dash.logout': 'Wyloguj \u23FB',
        'dash.changePass': 'Zmień hasło',
        'dash.deleteAcc': 'Usuń konto',
        'dash.settingsTitle': 'Ustawienia konta',
        'dash.chooseCategory': 'Wybierz kategorię.',
        'dash.confirmDelete': 'Czy na pewno chcesz usunąć ten wpis?',
        'dash.errorUpdate': 'Błąd podczas aktualizacji danych.',
        'dash.errorDelete': 'Błąd podczas usuwania danych.',
        'dash.errorSave': 'Błąd podczas zapisywania danych.',
        'dash.searchPh': 'Szukaj po nazwie lub kategorii...',
        'dash.allCategories': 'Wszystkie kategorie',
        'dash.search': 'Szukaj',
        'forgot.title': 'Zresetuj swoje hasło',
        'forgot.email': 'E-mail',
        'forgot.emailPh': 'E-mail',
        'forgot.btn': 'Wyślij link resetujący',
        'forgot.remember': 'Pamiętasz hasło?',
        'forgot.backLogin': 'Powrót do logowania',
        'reset.title': 'Utwórz nowe hasło',
        'reset.password': 'Nowe hasło',
        'reset.passwordPh': 'Nowe hasło',
        'reset.confirmPassword': 'Potwierdź hasło',
        'reset.confirmPh': 'Potwierdź hasło',
        'reset.btn': 'Zresetuj hasło',
        'reset.backQ': 'Wróć do logowania?',
        'reset.backLogin': 'Przejdź do logowania',
        'reset.unable': 'Nie można zresetować hasła',
        'reset.requestNew': 'Wyślij nowy link resetujący',
    }
};

function t(key) {
    var lang = localStorage.getItem('lang') || 'en';
    return (window.i18n[lang] && window.i18n[lang][key]) || key;
}

function applyLang(lang) {
    localStorage.setItem('lang', lang);
    document.documentElement.setAttribute('data-lang', lang);
    var translations = window.i18n[lang] || window.i18n.en;
    document.querySelectorAll('[data-i18n]').forEach(function (el) {
        var key = el.dataset.i18n;
        var val = translations[key];
        if (!val) return;
        if (el.tagName === 'INPUT' && el.hasAttribute('placeholder')) {
            el.placeholder = val;
        } else {
            el.textContent = val;
        }
    });
    document.querySelectorAll('.lang-btn').forEach(function (btn) {
        btn.classList.toggle('lang-btn-active', btn.dataset.lang === lang);
    });
}

function applyTheme(theme) {
    localStorage.setItem('theme', theme);
    document.documentElement.setAttribute('data-theme', theme);
    var cb = document.getElementById('darkModeToggle');
    if (cb) cb.checked = (theme === 'dark');
}

function toggleTheme() {
    applyTheme((localStorage.getItem('theme') || 'light') === 'dark' ? 'light' : 'dark');
}

document.addEventListener('DOMContentLoaded', function () {
    applyLang(localStorage.getItem('lang') || 'en');
    applyTheme(localStorage.getItem('theme') || 'light');
});
