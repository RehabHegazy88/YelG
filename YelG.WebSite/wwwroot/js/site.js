/*
 * YelG Advisory public site: the prototype's component logic (B-Ar.dc.html / B-En.dc.html) as small vanilla
 * modules over server-rendered markup. Texts come from the page (data-* attributes and the JSON blocks the
 * views write); nothing here holds copy.
 */
(function () {
  'use strict';

  var doc = document;

  function $(sel, root) { return (root || doc).querySelector(sel); }
  function $$(sel, root) { return Array.prototype.slice.call((root || doc).querySelectorAll(sel)); }
  function json(id) { var el = doc.getElementById(id); return el ? JSON.parse(el.textContent) : null; }
  function reduced() { try { return window.matchMedia('(prefers-reduced-motion: reduce)').matches; } catch (e) { return false; } }

  /* Scrolls to a section and moves focus there (after the DOM update), as the prototype's goTo(). */
  function goTo(id, focusId) {
    setTimeout(function () {
      var el = doc.getElementById(id);
      if (!el) return;
      el.scrollIntoView({ behavior: reduced() ? 'auto' : 'smooth', block: 'start' });
      var f = doc.getElementById(focusId || id);
      if (f && f.focus) f.focus({ preventScroll: true });
    }, 30);
  }

  /* Arrow keys, Home and End move between the tabs of a tablist (focus follows selection). */
  function tabKeys(list, select) {
    list.addEventListener('keydown', function (e) {
      var tabs = $$('[role="tab"]', list).filter(function (t) { return !t.hidden; });
      var i = tabs.indexOf(doc.activeElement);
      if (i < 0) return;
      var rtl = getComputedStyle(list).direction === 'rtl';
      var next = { ArrowRight: rtl ? i - 1 : i + 1, ArrowLeft: rtl ? i + 1 : i - 1, Home: 0, End: tabs.length - 1 }[e.key];
      if (next === undefined) return;
      e.preventDefault();
      var tab = tabs[(next + tabs.length) % tabs.length];
      tab.focus();
      select(tab);
    });
  }

  function setTabs(tabs, active, onClass) {
    tabs.forEach(function (t) {
      var on = t === active;
      t.setAttribute('aria-selected', on ? 'true' : 'false');
      t.tabIndex = on ? 0 : -1;
      t.classList.toggle(onClass, on);
    });
  }

  /* ---------- mobile menu ---------- */
  function menu() {
    var btn = $('.menuBtn'), nav = doc.getElementById('mobileNav');
    if (!btn || !nav) return;
    function set(open) { nav.hidden = !open; btn.setAttribute('aria-expanded', open ? 'true' : 'false'); }
    btn.addEventListener('click', function () { set(nav.hidden); });
    $$('a', nav).forEach(function (a) { a.addEventListener('click', function () { set(false); }); });
    doc.addEventListener('keydown', function (e) { if (e.key === 'Escape' && !nav.hidden) { set(false); btn.focus(); } });
  }

  /* ---------- language switch: stay on the same part of the page ---------- */
  function languageSwitch() {
    $$('[data-lang-switch]').forEach(function (a) {
      a.addEventListener('click', function (e) {
        if (!location.hash) return;
        e.preventDefault();
        location.href = a.getAttribute('href') + location.hash;
      });
    });
  }

  /* ---------- consultation form: pre-fill (used by services, ways to work, self-check) ---------- */
  var form = {
    el: null,
    prefill: function (service, message) {
      if (!this.el) return;
      var select = this.el.elements.service, text = this.el.elements.message;
      if (service && $$('option', select).some(function (o) { return o.value === service; })) select.value = service;
      if (message && !text.value) text.value = message;
      this.clear('service');
    },
    clear: function (name) {
      if (!this.el) return;
      var field = this.el.elements[name], err = this.error(name);
      if (field) field.setAttribute('aria-invalid', 'false');
      if (err) { err.hidden = true; err.textContent = ''; }
    },
    error: function (name) { return doc.getElementById({ name: 'eName', email: 'eEmail', company: 'eCompany', phone: 'ePhone', service: 'eService', message: 'eMsg', consent: 'eConsent' }[name]); }
  };

  function consultForm() {
    var el = doc.getElementById('consultForm');
    if (!el) return;
    form.el = el;
    var messages = JSON.parse(el.getAttribute('data-messages') || '{}');
    var summaryTemplate = el.getAttribute('data-error-summary') || '';
    var summary = $('[data-error-summary-box]', el);
    var order = ['name', 'email', 'company', 'phone', 'service', 'message', 'consent'];

    function values() {
      var f = el.elements;
      return { name: f.name.value, email: f.email.value, company: f.company.value, phone: f.phone.value, service: f.service.value, message: f.message.value, consent: f.consent.checked };
    }

    // the prototype's validate(), with the same rules and messages
    function validate(f) {
      var e = {};
      if (!f.name || f.name.trim().length < 2) e.name = messages.name;
      if (!f.email || !/^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/.test(f.email.trim())) e.email = messages.email;
      if (!f.company || f.company.trim().length < 2) e.company = messages.company;
      if (f.phone && !/^[+0-9][0-9\s()\-]{6,}$/.test(f.phone.trim())) e.phone = messages.phone;
      if (!f.service) e.service = messages.service;
      if (!f.message || f.message.trim().length < 20) e.message = messages.message;
      if (!f.consent) e.consent = messages.consent;
      return e;
    }

    order.forEach(function (name) {
      var field = el.elements[name];
      if (!field) return;
      field.addEventListener(field.type === 'checkbox' || field.tagName === 'SELECT' ? 'change' : 'input', function () {
        form.clear(name);
        var left = order.filter(function (n) { var x = form.error(n); return x && !x.hidden; }).length;
        if (!left) summary.hidden = true;
        else $('strong', summary).textContent = summaryTemplate.replace('{count}', left);
      });
    });

    el.addEventListener('submit', function (ev) {
      ev.preventDefault();
      var errs = validate(values());
      var count = 0, first = null;
      order.forEach(function (name) {
        var field = el.elements[name], err = form.error(name);
        if (errs[name]) {
          count++;
          if (!first) first = field;
          field.setAttribute('aria-invalid', 'true');
          err.textContent = errs[name];
          err.hidden = false;
        } else {
          form.clear(name);
        }
      });
      if (count) {
        $('strong', summary).textContent = summaryTemplate.replace('{count}', count);
        summary.hidden = false;
        setTimeout(function () { first.focus(); }, 30);
        return;
      }
      summary.hidden = true;
      // Sending the enquiry (antiforgery, honeypot, rate limit, e-mail) comes with the backend in phase 4.
      el.dispatchEvent(new CustomEvent('consult:valid', { detail: values() }));
    });

    $$('[data-prefill]').forEach(function (a) {
      a.addEventListener('click', function () { form.prefill(a.getAttribute('data-prefill')); });
    });
  }

  /* ---------- services: category tabs and the detail panel ---------- */
  function services() {
    var section = doc.getElementById('services');
    if (!section) return;
    var data = json('svcData') || [];
    var byKey = {};
    data.forEach(function (s) { byKey[s.key] = s; });
    var list = $('[role="tablist"]', section), tabs = $$('[role="tab"]', list);
    var panel = doc.getElementById('svcPanel'), cards = $$('article.svc', section);
    var detail = doc.getElementById('svcDetail');
    var current = 'all', open = null;

    function pick(tab) {
      current = tab.getAttribute('data-cat');
      setTabs(tabs, tab, 'ctab-on');
      panel.setAttribute('aria-labelledby', tab.id);
      cards.forEach(function (c) { c.hidden = current !== 'all' && c.getAttribute('data-cat') !== current; });
    }

    function show(key) {
      var s = byKey[key];
      if (!s) return;
      open = key;
      cards.forEach(function (c) {
        var on = c.getAttribute('data-service') === key;
        c.classList.toggle('svc-on', on);
        $('.tlink', c).setAttribute('aria-expanded', on ? 'true' : 'false');
      });
      $('[data-detail="icon"]', detail).className = 'ico ic-' + s.icon;
      $('[data-detail="cat"]', detail).textContent = s.catLabel;
      $('[data-detail="name"]', detail).textContent = s.name;
      $('[data-detail="long"]', detail).textContent = s.long;
      var ul = $('[data-detail="items"]', detail);
      ul.textContent = '';
      s.items.forEach(function (t) { var li = doc.createElement('li'); li.textContent = t; ul.appendChild(li); });
      detail.hidden = false;
      goTo('svcDetail');
    }

    function close() {
      open = null;
      detail.hidden = true;
      cards.forEach(function (c) { c.classList.remove('svc-on'); $('.tlink', c).setAttribute('aria-expanded', 'false'); });
      goTo('services', 'cat-' + current);
    }

    tabs.forEach(function (t) { t.addEventListener('click', function () { pick(t); }); });
    tabKeys(list, pick);
    $$('[data-open-service]', section).forEach(function (a) {
      a.addEventListener('click', function (e) { e.preventDefault(); show(a.getAttribute('data-open-service')); });
    });
    $('[data-close-service]', detail).addEventListener('click', close);
    $('[data-detail="discuss"]', detail).addEventListener('click', function () { if (open) form.prefill(open); });
    // footer links open a category
    $$('[data-footer-cat]').forEach(function (a) {
      a.addEventListener('click', function () {
        var tab = doc.getElementById('cat-' + a.getAttribute('data-footer-cat'));
        if (tab) pick(tab);
      });
    });
  }

  /* ---------- mission / vision / goals tabs ---------- */
  function purpose() {
    var list = $('.purpose [role="tablist"]');
    if (!list) return;
    var tabs = $$('[role="tab"]', list), panel = doc.getElementById('pp');
    function pick(tab) {
      setTabs(tabs, tab, 'tabbtn-on');
      panel.setAttribute('aria-labelledby', tab.id);
      var i = tab.getAttribute('data-tab');
      $$('[data-tab-panel]', panel).forEach(function (p) { p.hidden = p.getAttribute('data-tab-panel') !== i; });
    }
    tabs.forEach(function (t) { t.addEventListener('click', function () { pick(t); }); });
    tabKeys(list, pick);
  }

  /* ---------- financial self-check ---------- */
  function selfCheck() {
    var root = $('[data-quiz]');
    var data = json('quizData');
    if (!root || !data) return;
    var q = function (name) { return $('[data-quiz="' + name + '"]', root); };
    var run = $('[data-quiz-run]', root), done = $('[data-quiz-done]', root);
    var opts = $$('.opt', root), bar = $('.bar', root);
    var total = data.questions.length;
    var state = { q: 0, answers: data.questions.map(function () { return null; }) };

    function score() { return state.answers.reduce(function (s, a) { return s + (a || 0); }, 0); }
    function band(s) { for (var i = 0; i < data.bands.length; i++) if (s >= data.bands[i].min) return data.bands[i]; return data.bands[data.bands.length - 1]; }

    function render() {
      var answered = state.answers.filter(function (a) { return a !== null; }).length;
      var question = data.questions[state.q];
      q('progress').textContent = data.progress.replace('{n}', state.q + 1).replace('{total}', total);
      q('area').textContent = question.area;
      q('question').textContent = question.text;
      bar.setAttribute('aria-valuenow', String(answered));
      q('bar').style.width = (answered * 100 / total) + '%';
      opts.forEach(function (o) {
        var on = state.answers[state.q] === Number(o.getAttribute('data-score'));
        o.classList.toggle('opt-on', on);
        o.setAttribute('aria-pressed', on ? 'true' : 'false');
      });
      q('back').disabled = state.q === 0;
    }

    function finish() {
      var s = score(), b = band(s), recs = [];
      data.questions.forEach(function (qq, k) {
        var a = state.answers[k];
        if (a !== null && a < data.top && qq.service && recs.indexOf(qq.service) < 0) recs.push(qq.service);
      });
      q('score').textContent = String(s);
      q('band').textContent = b.title;
      q('bandText').textContent = b.text;
      var row = q('recs');
      row.textContent = '';
      recs.forEach(function (n) { var c = doc.createElement('span'); c.className = 'chip'; c.textContent = n; row.appendChild(c); });
      q('recsBlock').hidden = recs.length === 0;
      run.hidden = true;
      done.hidden = false;
    }

    opts.forEach(function (o) {
      o.addEventListener('click', function () {
        state.answers[state.q] = Number(o.getAttribute('data-score'));
        if (state.q < total - 1) { state.q++; render(); } else { render(); finish(); }
      });
    });
    q('back').addEventListener('click', function () { if (state.q > 0) { state.q--; render(); } });
    q('restart').addEventListener('click', function () {
      state.q = 0;
      state.answers = data.questions.map(function () { return null; });
      done.hidden = true;
      run.hidden = false;
      render();
    });
    var discuss = q('discuss');
    if (discuss) discuss.addEventListener('click', function () {
      var s = score();
      var select = form.el && form.el.elements.service;
      form.prefill(select && select.value ? select.value : 'not-sure',
        data.prefill.replace('{score}', s).replace('{max}', data.max).replace('{band}', band(s).title));
    });
    render();
  }

  menu();
  languageSwitch();
  consultForm();
  services();
  purpose();
  selfCheck();
})();
