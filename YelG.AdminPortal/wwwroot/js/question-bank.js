// YELG Lens — question bank editor (draft versions only). Every change is saved to the server,
// which validates it (codes, options, branching loops, bands) and the page reloads.
(function () {
    'use strict';

    const data = JSON.parse(document.getElementById('bankData').textContent);
    const base = '/QuestionBank';
    const $ = (selector, root = document) => root.querySelector(selector);
    const allQuestions = data.categories.flatMap(c => c.questions.map(q => ({ ...q, categoryCode: c.code })));
    const byId = Object.fromEntries(allQuestions.map(q => [q.id, q]));
    const byCode = Object.fromEntries(allQuestions.map(q => [q.code, q]));
    const categoryById = Object.fromEntries(data.categories.map(c => [c.id, c]));
    const withOptions = new Set(['YesNo', 'Choice', 'Multi']);

    async function post(action, query, body) {
        const params = new URLSearchParams(query || {}).toString();
        await window.yelg.api(`${base}/${action}/${data.versionId}${params ? '?' + params : ''}`, body || {});
    }

    async function run(action, query, body, okMessage) {
        try {
            await post(action, query, body);
            if (okMessage) window.yelg.toast(okMessage);
            setTimeout(() => location.reload(), okMessage ? 400 : 0);
            return true;
        } catch (err) {
            window.yelg.toast(err.message, true);
            return false;
        }
    }

    // ---------------- categories ----------------
    const categoryModal = new bootstrap.Modal('#categoryModal');
    let editingCategory = null;

    function openCategory(category) {
        editingCategory = category;
        $('#categoryTitle').textContent = category ? 'تعديل الفئة' : 'إضافة فئة';
        $('#catCode').value = category ? category.code : '';
        $('#catCode').readOnly = !!category;
        $('#catTitle').value = category ? category.title : '';
        $('#catStatus').value = category ? category.status : 'معتمد';
        if ($('#catStatus').value !== (category ? category.status : 'معتمد') && category) {
            $('#catStatus').add(new Option(category.status, category.status, true, true));
        }
        $('#catWeight').value = category ? category.baseWeight : 0;
        categoryModal.show();
    }

    $('[data-add-category]')?.addEventListener('click', () => openCategory(null));

    $('#categoryForm').addEventListener('submit', async e => {
        e.preventDefault();
        await run('SaveCategory', editingCategory ? { categoryId: editingCategory.id } : null, {
            code: $('#catCode').value, title: $('#catTitle').value, status: $('#catStatus').value, baseWeight: +$('#catWeight').value
        }, 'تم حفظ الفئة.');
    });

    document.querySelectorAll('[data-category]').forEach(section => {
        const category = categoryById[section.dataset.category];
        $('[data-edit-category]', section)?.addEventListener('click', () => openCategory(category));
        $('[data-add-question]', section)?.addEventListener('click', () => openQuestion(null, category.code));
        $('[data-delete-category]', section)?.addEventListener('click', () => {
            if (!confirm(`حذف فئة «${category.title}» وكل أسئلتها (${category.questions.length})؟ الأسئلة المتفرعة منها في فئات أخرى ستظهر بدون شرط.`)) return;
            run('DeleteCategory', { categoryId: category.id }, null, 'تم حذف الفئة.');
        });
        section.querySelectorAll('[data-move-category]').forEach(b => b.addEventListener('click', () =>
            run('MoveCategory', { categoryId: category.id, direction: b.dataset.moveCategory })));
    });

    // ---------------- question rows ----------------
    document.querySelectorAll('[data-question]').forEach(row => {
        const question = byId[row.dataset.question];
        $('[data-edit-question]', row)?.addEventListener('click', () => openQuestion(question, question.categoryCode));
        $('[data-toggle-question]', row)?.addEventListener('click', () =>
            run('SaveQuestion', { questionId: question.id }, { ...toRequest(question), isActive: !question.isActive },
                question.isActive ? 'تم إيقاف السؤال.' : 'تم إظهار السؤال.'));
        $('[data-delete-question]', row)?.addEventListener('click', () => {
            const note = question.childCount ? ` سيظهر ${question.childCount} سؤال متفرع منه بدون شرط.` : '';
            if (!confirm(`حذف السؤال ${question.code} — «${question.text}»؟${note}`)) return;
            run('DeleteQuestion', { questionId: question.id }, null, 'تم حذف السؤال.');
        });
        row.querySelectorAll('[data-move-question]').forEach(b => b.addEventListener('click', () =>
            run('MoveQuestion', { questionId: question.id, direction: b.dataset.moveQuestion })));
    });

    function toRequest(q) {
        return {
            categoryCode: q.categoryCode, code: q.code, text: q.text, answerType: q.answerType, kind: q.kind,
            direction: q.direction, isCritical: q.isCritical, allowCustomAnswer: q.allowCustomAnswer, isActive: q.isActive,
            weights: q.weights, parentCode: q.parentCode, conditionOperator: q.conditionOperator,
            conditionValue: q.conditionValue, options: q.options, bands: q.bands
        };
    }

    // ---------------- question modal ----------------
    const questionModal = new bootstrap.Modal('#questionModal');
    let editingQuestion = null;

    function optionRow(option) {
        const row = document.createElement('div');
        row.className = 'd-flex gap-2';
        row.innerHTML = `<input class="form-control" data-option-text maxlength="250" placeholder="نص الاختيار">
            <input class="form-control num" data-option-score type="number" min="0" max="1" step="0.01" placeholder="غير منطبق" style="width:130px">
            <button type="button" class="btn btn-danger-soft btn-sm" title="حذف">×</button>`;
        row.querySelector('[data-option-text]').value = option ? option.text : '';
        row.querySelector('[data-option-score]').value = option && option.score !== null && option.score !== undefined ? option.score : '';
        row.querySelector('button').addEventListener('click', () => row.remove());
        $('#optionRows').appendChild(row);
    }

    function bandRow(band) {
        const row = document.createElement('div');
        row.className = 'd-flex gap-2 align-items-center';
        row.innerHTML = `<input class="form-control num" data-band-min type="number" step="any" placeholder="من (بلا حد)">
            <input class="form-control num" data-band-max type="number" step="any" placeholder="إلى (بلا حد)">
            <input class="form-control num" data-band-score type="number" min="0" max="1" step="0.01" placeholder="الدرجة" required style="width:130px">
            <button type="button" class="btn btn-danger-soft btn-sm" title="حذف">×</button>`;
        if (band) {
            row.querySelector('[data-band-min]').value = band.minValue ?? '';
            row.querySelector('[data-band-max]').value = band.maxValue ?? '';
            row.querySelector('[data-band-score]').value = band.score;
        }
        row.querySelector('button').addEventListener('click', () => row.remove());
        $('#bandRows').appendChild(row);
    }

    function refreshTypeBlocks() {
        const type = $('#qType').value;
        const hasOptions = withOptions.has(type);
        $('#optionRows').hidden = !hasOptions;
        $('#addOption').hidden = !hasOptions;
        $('#optionHint').hidden = !hasOptions;
        $('#noOptions').hidden = hasOptions;
        $('#bandsBlock').hidden = hasOptions || !(type === 'Number' || $('#qKind').value === 'Numeric');
        $('#qCustom').disabled = type !== 'Multi';
        if (type !== 'Multi') $('#qCustom').checked = false;
        if (hasOptions && !$('#optionRows').children.length) {
            if (type === 'YesNo') [{ text: 'نعم', score: 1 }, { text: 'لا', score: 0 }, { text: 'غير منطبق', score: null }].forEach(optionRow);
            else { optionRow(); optionRow(); }
        }
    }

    function refreshCondition(value) {
        const parent = byCode[$('#qParent').value];
        const select = $('#qConditionValue');
        const text = $('#qConditionText');
        $('#qOperator').disabled = !parent;
        select.innerHTML = '';
        if (!parent) { select.hidden = false; select.disabled = true; text.hidden = true; return; }
        if (parent.options.length) {
            parent.options.forEach(o => select.add(new Option(o.text, o.text, false, o.text === value)));
            select.hidden = false; select.disabled = false; text.hidden = true;
        } else {
            select.hidden = true; text.hidden = false; text.value = value || '';
        }
    }

    function openQuestion(question, categoryCode) {
        editingQuestion = question;
        $('#questionTitle').textContent = question ? `تعديل السؤال ${question.code}` : 'إضافة سؤال';
        $('#qCategory').value = categoryCode;
        $('#qCode').value = question ? question.code : '';
        $('#qCode').readOnly = !!question;
        $('#qText').value = question ? question.text : '';
        $('#qType').value = question ? question.answerType : 'YesNo';
        $('#qKind').value = question ? question.kind : 'Binary';
        $('#qDirection').value = question ? question.direction : 'Positive';
        $('#qCritical').checked = question ? question.isCritical : false;
        $('#qCustom').checked = question ? question.allowCustomAnswer : false;
        $('#qActive').checked = question ? question.isActive : true;
        const weights = question ? question.weights : [3, 2, 1];
        ['#qW0', '#qW1', '#qW2'].forEach((id, i) => $(id).value = weights[i]);

        $('#optionRows').innerHTML = '';
        (question ? question.options : []).forEach(optionRow);
        $('#bandRows').innerHTML = '';
        (question ? question.bands : []).forEach(bandRow);
        refreshTypeBlocks();

        const parentSelect = $('#qParent');
        parentSelect.length = 1;
        allQuestions.filter(q => !question || q.id !== question.id)
            .forEach(q => parentSelect.add(new Option(`${q.code} — ${q.text.slice(0, 70)}`, q.code)));
        parentSelect.value = question && question.parentCode ? question.parentCode : '';
        $('#qOperator').value = question && question.conditionOperator ? question.conditionOperator : 'Equals';
        refreshCondition(question ? question.conditionValue : null);

        questionModal.show();
    }

    $('#qType').addEventListener('change', refreshTypeBlocks);
    $('#qKind').addEventListener('change', refreshTypeBlocks);
    $('#qParent').addEventListener('change', () => refreshCondition(null));
    $('#addOption').addEventListener('click', () => optionRow());
    $('#addBand').addEventListener('click', () => bandRow());

    const numberOrNull = value => value === '' ? null : +value;

    $('#questionForm').addEventListener('submit', async e => {
        e.preventDefault();
        const hasOptions = withOptions.has($('#qType').value);
        const parentCode = $('#qParent').value || null;
        const conditionValue = !parentCode ? null
            : ($('#qConditionText').hidden ? $('#qConditionValue').value : $('#qConditionText').value);

        const request = {
            categoryCode: $('#qCategory').value,
            code: $('#qCode').value,
            text: $('#qText').value,
            answerType: $('#qType').value,
            kind: $('#qKind').value,
            direction: $('#qDirection').value,
            isCritical: $('#qCritical').checked,
            allowCustomAnswer: $('#qCustom').checked,
            isActive: $('#qActive').checked,
            weights: ['#qW0', '#qW1', '#qW2'].map(id => +$(id).value || 0),
            parentCode,
            conditionOperator: parentCode ? $('#qOperator').value : null,
            conditionValue,
            options: hasOptions ? [...$('#optionRows').children].map(row => ({
                text: row.querySelector('[data-option-text]').value,
                score: numberOrNull(row.querySelector('[data-option-score]').value)
            })).filter(o => o.text.trim()) : [],
            bands: $('#bandsBlock').hidden ? [] : [...$('#bandRows').children].map(row => ({
                minValue: numberOrNull(row.querySelector('[data-band-min]').value),
                maxValue: numberOrNull(row.querySelector('[data-band-max]').value),
                score: +row.querySelector('[data-band-score]').value
            }))
        };

        const button = $('#saveQuestion');
        window.yelg.setBusy(button, true);
        const ok = await run('SaveQuestion', editingQuestion ? { questionId: editingQuestion.id } : null, request, 'تم حفظ السؤال.');
        if (!ok) window.yelg.setBusy(button, false);
    });
})();
