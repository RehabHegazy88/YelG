// YELG Lens — shared helpers for every page.
(function () {
    'use strict';

    const token = () => document.querySelector('meta[name="request-verification-token"]')?.content || '';

    /**
     * POSTs JSON with the antiforgery header and returns the parsed body.
     * Throws an Error whose message is the server's Arabic error text.
     */
    async function api(url, body) {
        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json',
                'X-Requested-With': 'XMLHttpRequest',
                'RequestVerificationToken': token()
            },
            body: JSON.stringify(body || {})
        });
        if (response.status === 401) {
            window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(location.pathname);
            throw new Error('انتهت الجلسة. سجّلي الدخول مرة أخرى.');
        }
        let data = null;
        try { data = await response.json(); } catch { /* empty body */ }
        if (!response.ok) throw new Error((data && data.error) || 'حدث خطأ غير متوقع. حاولي مرة أخرى.');
        return data;
    }

    let toastTimer = null;
    function toast(message, bad) {
        const el = document.getElementById('toast');
        if (!el) return;
        el.textContent = message;
        el.classList.toggle('bad', !!bad);
        el.classList.add('on');
        clearTimeout(toastTimer);
        toastTimer = setTimeout(() => el.classList.remove('on'), 4500);
    }

    function setBusy(button, busy, label) {
        if (!button) return;
        button.disabled = busy;
        if (busy) { button.dataset.label = button.textContent; button.textContent = label || 'جارٍ الحفظ…'; }
        else if (button.dataset.label) { button.textContent = button.dataset.label; delete button.dataset.label; }
    }

    // Reviewer pickers: "select all" / "clear" buttons inside a modal.
    document.addEventListener('click', e => {
        const all = e.target.closest('[data-select-all]');
        const none = e.target.closest('[data-select-none]');
        if (!all && !none) return;
        const scope = (all || none).closest('.modal, form');
        scope.querySelectorAll('input[type=checkbox][data-reviewer]:not(:disabled)').forEach(cb => cb.checked = !!all);
    });

    // Confirmation for destructive form posts: <form data-confirm="...">.
    document.addEventListener('submit', e => {
        const message = e.target.dataset.confirm;
        if (message && !window.confirm(message)) e.preventDefault();
    });

    window.yelg = { api, toast, setBusy };
})();
