// YELG Lens — evaluation screen.
// The server owns the rules: every change asks /Evaluations/Preview for the visible questions and the
// score, so branching and scoring live only in the C# engine.
(function () {
    'use strict';

    const data = JSON.parse(document.getElementById('evaluationData').textContent);
    const answers = {};
    Object.entries(data.answers || {}).forEach(([code, values]) => answers[code] = [...values]);
    let rowVersion = data.rowVersion;
    let dirty = false;
    let previewTimer = null;
    let previewSeq = 0;

    const $ = (selector, root = document) => root.querySelector(selector);
    const $$ = (selector, root = document) => [...root.querySelectorAll(selector)];
    const percent = v => v === null || v === undefined ? '—' : v.toFixed(1) + '%';
    const escapeHtml = s => String(s).replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));

    // ---------------- category navigation ----------------
    const sections = $$('[data-cat]');
    const navButtons = $$('[data-cat-btn]');
    function showCategory(index) {
        sections.forEach(s => s.hidden = +s.dataset.cat !== index);
        navButtons.forEach(b => {
            const on = +b.dataset.catBtn === index;
            b.classList.toggle('on', on);
            if (on) b.setAttribute('aria-current', 'step'); else b.removeAttribute('aria-current');
        });
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }
    navButtons.forEach(b => b.addEventListener('click', () => showCategory(+b.dataset.catBtn)));
    sections.forEach(s => {
        const index = +s.dataset.cat;
        $('[data-prev]', s)?.addEventListener('click', () => showCategory(index - 1));
        $('[data-next]', s)?.addEventListener('click', () => showCategory(index + 1));
    });

    // ---------------- answering ----------------
    function setAnswer(code, values) {
        const clean = values.map(v => v.trim()).filter(Boolean);
        if (clean.length) answers[code] = clean; else delete answers[code];
        markDirty();
        renderQuestion(code);
        schedulePreview();
    }

    function renderQuestion(code) {
        const row = $(`.eval-q[data-code="${CSS.escape(code)}"]`);
        if (!row) return;
        const values = answers[code] || [];
        row.classList.toggle('answered', values.length > 0);

        let risky = false;
        $$('.opt', row).forEach(button => {
            const selected = values.includes(button.dataset.option);
            const score = button.dataset.score === '' ? null : +button.dataset.score;
            button.classList.toggle('sel', selected);
            button.classList.toggle('risk', selected && score !== null && score < 0.5);
            button.setAttribute('aria-pressed', selected ? 'true' : 'false');
            if (selected && score !== null && score < 0.5) risky = true;
            if (button.dataset.custom && !selected) button.remove();
        });

        const note = $('[data-risk-note]', row);
        if (note) {
            note.hidden = !risky;
            note.textContent = row.dataset.critical === '1'
                ? 'هذه الإجابة تكشف بندًا حرجًا وتحتاج مراجعة فورية.'
                : 'هذه الإجابة تخفض مؤشر الثقة وتحتاج انتباهًا عند المراجعة.';
        }
    }

    document.addEventListener('click', e => {
        const option = e.target.closest('.opt');
        if (option && !option.disabled) {
            const row = option.closest('.eval-q');
            const code = row.dataset.code;
            const value = option.dataset.option;
            const current = answers[code] || [];
            if (row.dataset.multi === '1') {
                setAnswer(code, current.includes(value) ? current.filter(v => v !== value) : [...current, value]);
            } else {
                setAnswer(code, current[0] === value ? [] : [value]);
            }
            return;
        }
        const add = e.target.closest('[data-custom-add]');
        if (add) addCustom(add.closest('.eval-q'));
    });

    document.addEventListener('keydown', e => {
        if (e.key === 'Enter' && e.target.matches('[data-custom-input]')) {
            e.preventDefault();
            addCustom(e.target.closest('.eval-q'));
        }
    });

    function addCustom(row) {
        const input = $('[data-custom-input]', row);
        const value = input.value.trim().replace(/\s+/g, ' ');
        if (!value) { input.focus(); return; }
        const code = row.dataset.code;
        const current = answers[code] || [];
        if (current.some(v => v.toLowerCase() === value.toLowerCase())) { window.yelg.toast('هذا الاختيار مضاف بالفعل.', true); return; }

        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'opt';
        button.dataset.option = value;
        button.dataset.custom = '1';
        button.dataset.score = '';
        button.textContent = value + ' ×';
        const lastOption = $$('.opt', row).pop();
        lastOption.after(button);

        input.value = '';
        setAnswer(code, [...current, value]);
    }

    $$('[data-input]').forEach(input => input.addEventListener('input', () => {
        setAnswer(input.closest('.eval-q').dataset.code, [input.value]);
    }));

    $('#judgment')?.addEventListener('change', () => { markDirty(); renderJudgmentMatch(lastScore); });
    $('#note')?.addEventListener('input', markDirty);

    // ---------------- preview (visibility + score) ----------------
    function schedulePreview() {
        clearTimeout(previewTimer);
        previewTimer = setTimeout(runPreview, 250);
    }

    async function runPreview() {
        const seq = ++previewSeq;
        try {
            const result = await window.yelg.api(`/Evaluations/Preview/${data.id}`, { answers });
            if (seq !== previewSeq) return; // a newer change is already on its way
            applyVisibility(result.visibleCodes);
            applyProgress(result.progress);
            renderScore(result.score);
        } catch (err) {
            window.yelg.toast(err.message, true);
        }
    }

    function applyVisibility(visibleCodes) {
        const visible = new Set(visibleCodes);
        $$('.eval-q').forEach(row => row.hidden = !visible.has(row.dataset.code));
    }

    function applyProgress(progress) {
        progress.forEach(p => {
            const count = $(`[data-count="${CSS.escape(p.code)}"]`);
            if (!count) return;
            count.textContent = `${p.answered}/${p.visible}`;
            count.closest('.eval-index-btn').classList.toggle('done', p.visible > 0 && p.answered === p.visible);
        });
    }

    let lastScore = data.initialScore;
    function renderScore(score) {
        lastScore = score;
        $('#answered').textContent = score.answered;
        $('#visibleCount').textContent = score.visibleCount;
        $('#progressBar').style.width = (score.visibleCount ? score.answered / score.visibleCount * 100 : 0) + '%';
        $('#confidence').textContent = score.answered ? percent(score.confidence) : '—';
        $('#grade').textContent = score.answered ? data.gradeLabels[score.grade] : '—';
        $('#breachCount').textContent = score.breaches.length;

        const flags = [
            ...score.breaches.map(q => `<div class="flag"><span class="code">${escapeHtml(q.code)}</span>${escapeHtml(q.text)}</div>`),
            ...score.unscored.map(q => `<div class="flag"><span class="code" style="color:var(--color-text-3)">${escapeHtml(q.code)}</span>${escapeHtml(q.text)} <span class="chip">لا يدخل في الحساب بعد</span></div>`)
        ];
        $('#flags').innerHTML = flags.length ? flags.join('') : '<div class="small text-secondary">لا توجد بنود حرجة تحتاج مراجعة حاليًا.</div>';

        $('#categoryResults').innerHTML = score.categories.map(c => `
            <tr><td class="code">${escapeHtml(c.code)}</td><td>${escapeHtml(c.title)}</td>
            <td class="num">${percent(c.confidence)}</td><td class="num">${percent(c.workflow)}</td><td class="num">${percent(c.pricing)}</td>
            <td class="num">${c.relativeWeight.toFixed(1)}</td><td class="num">${c.contribution === null ? '—' : c.contribution.toFixed(2)}</td></tr>`).join('');

        renderJudgmentMatch(score);
    }

    function renderJudgmentMatch(score) {
        const el = $('#judgmentMatch');
        const judgment = $('#judgment')?.value;
        if (!el) return;
        if (!judgment || !score || !score.answered) { el.innerHTML = ''; return; }
        el.innerHTML = judgment === score.grade
            ? '<span class="status ok">مطابق للتقدير المحتسب</span>'
            : '<span class="status back">مختلف — يلزم مراجعة الأوزان</span>';
    }

    // ---------------- saving and workflow ----------------
    function markDirty() {
        if (!data.canEdit) return;
        dirty = true;
        const state = $('#saveState');
        if (state) { state.textContent = 'توجد تغييرات غير محفوظة.'; state.classList.add('dirty'); }
    }

    async function save() {
        const button = $('#saveBtn');
        window.yelg.setBusy(button, true);
        try {
            const result = await window.yelg.api(`/Evaluations/Save/${data.id}`, {
                answers,
                professionalJudgment: $('#judgment').value || null,
                note: $('#note').value,
                rowVersion
            });
            rowVersion = result.rowVersion;
            dirty = false;
            const state = $('#saveState');
            state.textContent = 'تم الحفظ.';
            state.classList.remove('dirty');
            renderScore(result.score);
            updateStatus(result);
            window.yelg.toast('تم حفظ التقييم.');
            return true;
        } catch (err) {
            window.yelg.toast(err.message, true);
            return false;
        } finally {
            window.yelg.setBusy(button, false);
        }
    }

    function updateStatus(result) {
        const badge = $('#statusBadge');
        badge.textContent = result.statusLabel;
        badge.className = 'status ms-auto ' + ({ Submitted: 'wait', Approved: 'ok', Returned: 'back' }[result.status] || '');
    }

    async function transition(url, confirmText, okText) {
        if (confirmText && !window.confirm(confirmText)) return;
        if (dirty && data.canEdit && !(await save())) return;
        try {
            await window.yelg.api(url, { rowVersion });
            window.yelg.toast(okText);
            dirty = false;
            setTimeout(() => window.location.reload(), 600);
        } catch (err) {
            window.yelg.toast(err.message, true);
        }
    }

    $('#saveBtn')?.addEventListener('click', save);

    $('#submitBtn')?.addEventListener('click', () => {
        const remaining = lastScore ? lastScore.visibleCount - lastScore.answered : 0;
        const question = remaining > 0
            ? `ما زال هناك ${remaining} سؤالًا ظاهرًا غير مستوفى. هل تريدين الإرسال للاعتماد رغم ذلك؟`
            : 'إرسال التقييم للاعتماد؟ لن تتمكني من تعديله حتى يراجعه الـAdmin.';
        transition(`/Evaluations/Submit/${data.id}`, question, 'تم إرسال التقييم للاعتماد.');
    });
    $('#approveBtn')?.addEventListener('click', () =>
        transition(`/Evaluations/Approve/${data.id}`, 'اعتماد هذا التقييم؟', 'تم اعتماد التقييم.'));
    $('#returnBtn')?.addEventListener('click', () =>
        transition(`/Evaluations/Return/${data.id}`, 'إعادة التقييم للمقيم للتعديل؟', 'تمت إعادة التقييم للتعديل.'));

    window.addEventListener('beforeunload', e => {
        if (dirty) { e.preventDefault(); e.returnValue = ''; }
    });

    // initial render of the side panel, flags and results table
    Object.keys(answers).forEach(renderQuestion);
    renderScore(data.initialScore);
})();
