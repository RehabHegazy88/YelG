// YELG Lens — task library and answer effects. The server validates every save; the page reloads after it.
(function () {
    'use strict';

    const data = JSON.parse(document.getElementById('libraryData').textContent);
    const $ = (selector, root = document) => root.querySelector(selector);
    const $$ = (selector, root = document) => [...root.querySelectorAll(selector)];
    const num = id => +$(id).value || 0;

    async function run(url, body, okMessage) {
        try {
            await window.yelg.api(url, body || {});
            window.yelg.toast(okMessage);
            setTimeout(() => location.reload(), 400);
            return true;
        } catch (err) {
            window.yelg.toast(err.message, true);
            return false;
        }
    }

    function filterRows(rows, emptyId, matches) {
        let shown = 0;
        rows.forEach(row => { const ok = matches(row); row.hidden = !ok; if (ok) shown++; });
        $(emptyId).hidden = shown > 0;
    }

    // =============================== tasks ===============================
    if (data.templates) {
        const byId = Object.fromEntries(data.templates.map(t => [t.id, t]));
        const rows = $$('#taskTable [data-template]');
        const applyFilter = () => {
            const text = $('#taskSearch').value.trim().toLowerCase();
            const line = $('#taskLine').value;
            const state = $('#taskState').value;
            filterRows(rows, '#noTasks', row =>
                (!text || row.dataset.search.includes(text))
                && (!line || row.dataset.line === line)
                && (state !== 'hours' || (+row.dataset.hours === 0 && row.dataset.active === '1'))
                && (state !== 'default' || row.dataset.default === '1')
                && (state !== 'inactive' || row.dataset.active === '0'));
        };
        ['#taskSearch', '#taskLine', '#taskState'].forEach(id => $(id).addEventListener('input', applyFilter));

        const modal = new bootstrap.Modal('#templateModal');
        let editing = null;

        const refreshDriver = () => {
            const fixed = !$('#tDriver').value;
            $('#tUnit').disabled = fixed;
            $('#tMax').disabled = fixed;
            $('#driverHint').hidden = fixed;
        };
        $('#tDriver').addEventListener('change', refreshDriver);

        function open(t) {
            editing = t;
            $('#templateTitle').textContent = t ? `تعديل المهمة ${t.code}` : 'إضافة مهمة';
            $('#tCode').value = t ? t.code : '';
            $('#tCode').readOnly = !!t;
            $('#tSource').value = t?.sourceQuestionCode || '';
            $('#tLine').value = t?.serviceLine || '';
            $('#tPurpose').value = t?.purpose || '';
            $('#tTitle').value = t?.title || '';
            $('#tEvidence').value = t?.evidence || '';
            $('#tAnalyst').value = t ? t.roleHours.analyst : 0;
            $('#tTeamLead').value = t ? t.roleHours.teamLead : 0;
            $('#tManager').value = t ? t.roleHours.manager : 0;
            $('#tPartner').value = t ? t.roleHours.partner : 0;
            $('#tFrequency').value = t ? t.frequency : 'Monthly';
            $('#tPriority').value = t ? t.priority : 'Medium';
            $('#tOwner').value = t ? t.defaultOwner : 'YelgTeam';
            $('#tBasis').value = t ? t.basisStatus : 'Draft';
            $('#tDriver').value = t?.driverQuestionCode || '';
            $('#tUnit').value = t ? t.driverUnit : 1;
            $('#tMax').value = t ? t.maxMultiplier : 4;
            $('#tBasisNote').value = t?.basisNote || '';
            $('#tDefault').checked = t ? t.defaultIncluded : false;
            $('#tActive').checked = t ? t.isActive : true;
            refreshDriver();
            modal.show();
        }

        $('[data-add-template]').addEventListener('click', () => open(null));
        rows.forEach(row => {
            const t = byId[row.dataset.template];
            $('[data-edit-template]', row).addEventListener('click', () => open(t));
            $('[data-delete-template]', row).addEventListener('click', () => {
                const note = t.ruleCount ? ` سيتم حذف ${t.ruleCount} تأثير مرتبط بها أيضًا.` : '';
                if (!confirm(`حذف المهمة ${t.code}؟${note}`)) return;
                run(`/TaskLibrary/DeleteTemplate/${t.id}`, null, 'تم حذف المهمة.');
            });
        });

        $('#templateForm').addEventListener('submit', async e => {
            e.preventDefault();
            const button = $('#saveTemplate');
            window.yelg.setBusy(button, true);
            const ok = await run(`/TaskLibrary/SaveTemplate${editing ? '/' + editing.id : ''}`, {
                code: $('#tCode').value, title: $('#tTitle').value, serviceLine: $('#tLine').value,
                evidence: $('#tEvidence').value, purpose: $('#tPurpose').value, defaultOwner: $('#tOwner').value,
                sourceQuestionCode: $('#tSource').value,
                roleHours: { analyst: num('#tAnalyst'), teamLead: num('#tTeamLead'), manager: num('#tManager'), partner: num('#tPartner') },
                driverQuestionCode: $('#tDriver').value || null, driverUnit: num('#tUnit') || 1, maxMultiplier: num('#tMax') || 1,
                frequency: $('#tFrequency').value, priority: $('#tPriority').value,
                defaultIncluded: $('#tDefault').checked, isActive: $('#tActive').checked,
                basisStatus: $('#tBasis').value, basisNote: $('#tBasisNote').value
            }, 'تم حفظ المهمة.');
            if (!ok) window.yelg.setBusy(button, false);
        });
    }

    // =============================== rules ===============================
    if (data.rules) {
        const byId = Object.fromEntries(data.rules.map(r => [r.id, r]));
        const questions = Object.fromEntries(data.questions.map(q => [q.code, q]));
        const rows = $$('#ruleTable [data-rule]');
        const applyFilter = () => {
            const text = $('#ruleSearch').value.trim().toLowerCase();
            const effect = $('#ruleEffect').value;
            filterRows(rows, '#noRules', row => (!text || row.dataset.search.includes(text)) && (!effect || row.dataset.effect === effect));
        };
        ['#ruleSearch', '#ruleEffect'].forEach(id => $(id).addEventListener('input', applyFilter));

        const modal = new bootstrap.Modal('#ruleModal');
        let editing = null;
        const noValue = new Set(['Answered', 'NotAnswered']);
        const numeric = new Set(['GreaterThan', 'GreaterOrEqual', 'LessThan']);
        const needsTask = new Set(['ExpandTask', 'AddProcedure', 'OptionalService']);

        // The value control follows the question: options become a dropdown (or checkboxes for "one of").
        function refreshValue(value) {
            const q = questions[$('#rQuestion').value];
            const op = $('#rOperator').value;
            const select = $('#rValueSelect'), checks = $('#rValueChecks'), text = $('#rValueText');
            const hasOptions = q && q.options.length > 0 && !numeric.has(op) && op !== 'Contains';
            $('#valueBlock').hidden = noValue.has(op);
            select.hidden = !(hasOptions && op !== 'In');
            checks.hidden = !(hasOptions && op === 'In');
            text.hidden = hasOptions;
            text.type = numeric.has(op) ? 'number' : 'text';

            const current = value !== undefined ? value : null;
            select.innerHTML = '';
            checks.innerHTML = '';
            if (hasOptions) {
                const chosen = new Set((current || '').split('|').map(v => v.trim()));
                q.options.forEach(o => {
                    select.add(new Option(o, o, false, o === current));
                    const id = 'opt-' + Math.random().toString(36).slice(2);
                    checks.insertAdjacentHTML('beforeend',
                        `<label class="reviewer-option py-1" for="${id}"><input class="form-check-input mt-0" type="checkbox" id="${id}" value=""><span></span></label>`);
                    const label = checks.lastElementChild;
                    label.querySelector('input').value = o;
                    label.querySelector('input').checked = chosen.has(o);
                    label.querySelector('span').textContent = o;
                });
            } else if (current !== null) {
                text.value = current;
            }
        }

        function refreshEffect() {
            const effect = $('#rEffect').value;
            $('#taskBlock').hidden = !needsTask.has(effect);
            $('#deltaBlock').hidden = effect !== 'ExpandTask';
            $('#recommendationBlock').hidden = effect !== 'Recommendation';
        }

        $('#rQuestion').addEventListener('change', () => { $('#rValueText').value = ''; refreshValue(null); });
        $('#rOperator').addEventListener('change', () => refreshValue(null));
        $('#rEffect').addEventListener('change', refreshEffect);

        function open(r) {
            editing = r;
            $('#ruleTitle').textContent = r ? `تعديل التأثير ${r.code}` : 'إضافة تأثير للإجابة';
            $('#rCode').value = r ? r.code : '';
            $('#rCode').readOnly = !!r;
            if (r && !questions[r.questionCode]) $('#rQuestion').add(new Option(`${r.questionCode} — غير موجود في النسخة المنشورة`, r.questionCode));
            $('#rQuestion').value = r ? r.questionCode : data.questions[0]?.code;
            $('#rOperator').value = r ? r.operator : 'Equals';
            $('#rValueText').value = '';
            refreshValue(r ? r.value : null);
            $('#rEffect').value = r ? r.effectType : 'AddProcedure';
            if (r && r.taskTemplateId) $('#rTask').value = r.taskTemplateId;
            $('#rdAnalyst').value = r ? r.roleDelta.analyst : 0;
            $('#rdTeamLead').value = r ? r.roleDelta.teamLead : 0;
            $('#rdManager').value = r ? r.roleDelta.manager : 0;
            $('#rdPartner').value = r ? r.roleDelta.partner : 0;
            $('#rRecommendation').value = r?.recommendation || '';
            $('#rNote').value = r?.conditionNote || '';
            $('#rPriority').value = r ? r.priority : 'Medium';
            $('#rActive').checked = r ? r.isActive : true;
            refreshEffect();
            modal.show();
        }

        $('[data-add-rule]').addEventListener('click', () => open(null));
        rows.forEach(row => {
            const r = byId[row.dataset.rule];
            $('[data-edit-rule]', row).addEventListener('click', () => open(r));
            $('[data-delete-rule]', row).addEventListener('click', () => {
                if (!confirm(`حذف التأثير ${r.code}؟`)) return;
                run(`/TaskLibrary/DeleteRule/${r.id}`, null, 'تم حذف التأثير.');
            });
        });

        $('#ruleForm').addEventListener('submit', async e => {
            e.preventDefault();
            const op = $('#rOperator').value;
            let value = null;
            if (!noValue.has(op)) {
                if (!$('#rValueChecks').hidden) value = $$('#rValueChecks input:checked').map(cb => cb.value).join('|');
                else if (!$('#rValueSelect').hidden) value = $('#rValueSelect').value;
                else value = $('#rValueText').value;
            }
            const effect = $('#rEffect').value;
            const button = $('#saveRule');
            window.yelg.setBusy(button, true);
            const ok = await run(`/TaskLibrary/SaveRule${editing ? '/' + editing.id : ''}`, {
                code: $('#rCode').value || null, questionCode: $('#rQuestion').value, operator: op, value, effectType: effect,
                taskTemplateId: needsTask.has(effect) ? +$('#rTask').value : null,
                roleDelta: { analyst: num('#rdAnalyst'), teamLead: num('#rdTeamLead'), manager: num('#rdManager'), partner: num('#rdPartner') },
                recommendation: $('#rRecommendation').value, conditionNote: $('#rNote').value,
                priority: $('#rPriority').value, isActive: $('#rActive').checked
            }, 'تم حفظ التأثير.');
            if (!ok) window.yelg.setBusy(button, false);
        });
    }
})();
