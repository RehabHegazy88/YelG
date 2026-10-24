/* Admin panel behaviour. Every screen works without it (plain forms and links); this adds the menu drawer,
   confirmations, the unsaved-changes warning, the media picker, drag-and-drop ordering and uploads. */
(function () {
  'use strict';

  document.documentElement.classList.add('js');

  var tokenInput = document.querySelector('#mediaPicker input[name="__RequestVerificationToken"]');
  function token() { return tokenInput ? tokenInput.value : ''; }

  function post(url, body) {
    return fetch(url, {
      method: 'POST',
      body: body,
      credentials: 'same-origin',
      headers: { 'Accept': 'application/json', 'RequestVerificationToken': token() }
    });
  }

  // ---------- menu drawer (under 900px) ----------
  var toggle = document.querySelector('[data-nav-toggle]');
  var nav = document.getElementById('adminNav');
  if (toggle && nav) {
    toggle.addEventListener('click', function () {
      var open = nav.classList.toggle('open');
      toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
    });
    document.addEventListener('keydown', function (e) {
      if (e.key === 'Escape' && nav.classList.contains('open')) {
        nav.classList.remove('open');
        toggle.setAttribute('aria-expanded', 'false');
        toggle.focus();
      }
    });
  }

  // ---------- confirmations ----------
  document.addEventListener('submit', function (e) {
    var form = e.target;
    var message = form.getAttribute('data-confirm');
    if (message && !window.confirm(message)) e.preventDefault();
  }, true);

  // ---------- unsaved changes ----------
  var dirtyForms = new Set();
  function markDirty(form) {
    if (!form || !form.hasAttribute('data-unsaved')) return;
    dirtyForms.add(form);
    var note = form.querySelector('[data-dirty-note]');
    if (note) note.hidden = false;
  }
  document.addEventListener('input', function (e) { markDirty(e.target.form); });
  document.addEventListener('change', function (e) { markDirty(e.target.form); });
  document.addEventListener('submit', function (e) { if (!e.defaultPrevented) dirtyForms.delete(e.target); });
  window.addEventListener('beforeunload', function (e) {
    if (dirtyForms.size > 0) { e.preventDefault(); e.returnValue = ''; }
  });

  // first error on a returned form
  var errorBox = document.querySelector('[data-focus-on-load]');
  if (errorBox) {
    errorBox.focus();
    var firstInvalid = document.querySelector('[aria-invalid="true"]');
    if (firstInvalid) firstInvalid.scrollIntoView({ block: 'center' });
  }

  // ---------- character counters ----------
  document.querySelectorAll('[data-max]').forEach(function (input) {
    var max = parseInt(input.getAttribute('data-max'), 10);
    if (!max || !input.parentElement.classList.contains('lang-box')) return;
    var counter = document.createElement('span');
    counter.className = 'counter';
    counter.setAttribute('aria-hidden', 'true');
    input.parentElement.appendChild(counter);
    function update() {
      var left = max - input.value.length;
      counter.textContent = left < 40 ? String(left) : '';
      counter.classList.toggle('over', left < 0);
    }
    input.addEventListener('input', update);
    update();
  });

  // ---------- temporary passwords ----------
  document.querySelectorAll('[data-generate-password]').forEach(function (button) {
    button.addEventListener('click', function () {
      var input = button.parentElement.querySelector('[data-password]');
      var chars = 'abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789';
      var bytes = new Uint32Array(14);
      window.crypto.getRandomValues(bytes);
      var value = '';
      for (var i = 0; i < bytes.length; i++) value += chars[bytes[i] % chars.length];
      // the policy asks for a digit and a lower-case letter
      input.value = value.slice(0, 12) + 'k' + (bytes[0] % 8 + 2);
      input.dispatchEvent(new Event('input', { bubbles: true }));
      input.select();
    });
  });

  // ---------- media picker ----------
  var picker = document.getElementById('mediaPicker');
  var activeField = null;
  if (picker) {
    var grid = picker.querySelector('[data-picker-grid]');
    var search = picker.querySelector('[data-picker-search]');
    var status = picker.querySelector('[data-picker-status]');
    var upload = picker.querySelector('[data-picker-upload]');
    var searchTimer = 0;

    function kind() { return activeField ? activeField.getAttribute('data-kind') : 'Image'; }

    function card(item) {
      var b = document.createElement('button');
      b.type = 'button';
      b.className = 'picker-item';
      b.setAttribute('aria-pressed', activeField && activeField.querySelector('[data-media-value]').value === String(item.id) ? 'true' : 'false');
      var thumb = document.createElement('span');
      thumb.className = 'media-thumb';
      if (item.thumb) {
        var img = document.createElement('img');
        img.src = item.thumb; img.alt = ''; img.loading = 'lazy';
        thumb.appendChild(img);
      } else {
        var v = document.createElement('span');
        v.className = 'media-video'; v.textContent = '▶ فيديو';
        thumb.appendChild(v);
      }
      var name = document.createElement('span');
      name.className = 'media-name'; name.dir = 'ltr'; name.textContent = item.name;
      var meta = document.createElement('span');
      meta.className = 'media-meta'; meta.dir = 'ltr';
      meta.textContent = item.size + (item.width ? ' · ' + item.width + '×' + item.height : '');
      b.appendChild(thumb); b.appendChild(name); b.appendChild(meta);
      b.setAttribute('aria-label', item.name + (item.alt ? ' — ' + item.alt : ''));
      b.addEventListener('click', function () { choose(item); });
      return b;
    }

    function load() {
      status.textContent = 'جارٍ التحميل…';
      var url = '/admin/media/pick?kind=' + encodeURIComponent(kind()) + '&q=' + encodeURIComponent(search.value.trim());
      fetch(url, { credentials: 'same-origin', headers: { 'Accept': 'application/json' } })
        .then(function (r) { if (!r.ok) throw new Error(); return r.json(); })
        .then(function (data) {
          grid.textContent = '';
          data.items.forEach(function (item) { grid.appendChild(card(item)); });
          status.textContent = data.items.length ? data.items.length + ' ملف' : 'لا توجد ملفات. ارفع ملفًا جديدًا.';
        })
        .catch(function () { status.textContent = 'تعذّر تحميل المكتبة.'; });
    }

    function choose(item) {
      var field = activeField;
      field.querySelector('[data-media-value]').value = item.id;
      var preview = field.querySelector('[data-media-preview]');
      preview.textContent = '';
      if (item.kind === 'image') {
        var img = document.createElement('img');
        img.src = item.thumb; img.alt = '';
        preview.appendChild(img);
      } else {
        var v = document.createElement('span');
        v.className = 'media-video'; v.dir = 'ltr'; v.textContent = '▶ ' + item.name;
        preview.appendChild(v);
      }
      var clear = field.querySelector('[data-media-clear]');
      if (clear) clear.hidden = false;
      var link = field.querySelector('[data-media-link]');
      if (link) link.href = '/admin/media/' + item.id;
      markDirty(field.closest('form'));
      picker.close();
      field.querySelector('[data-media-pick]').focus();
    }

    document.addEventListener('click', function (e) {
      var pick = e.target.closest('[data-media-pick]');
      if (pick) {
        activeField = pick.closest('[data-media-field]');
        search.value = '';
        upload.accept = kind() === 'Video' ? 'video/mp4,video/webm' : 'image/jpeg,image/png,image/webp,image/gif';
        grid.textContent = '';
        picker.showModal();
        search.focus();
        load();
        return;
      }
      var clear = e.target.closest('[data-media-clear]');
      if (clear) {
        var field = clear.closest('[data-media-field]');
        field.querySelector('[data-media-value]').value = '';
        var preview = field.querySelector('[data-media-preview]');
        preview.textContent = '';
        var empty = document.createElement('span');
        empty.className = 'media-empty';
        empty.textContent = field.getAttribute('data-kind') === 'Video' ? 'لم يُختر فيديو' : 'لم تُختر صورة';
        preview.appendChild(empty);
        clear.hidden = true;
        markDirty(field.closest('form'));
        field.querySelector('[data-media-pick]').focus();
      }
    });

    search.addEventListener('input', function () {
      clearTimeout(searchTimer);
      searchTimer = setTimeout(load, 250);
    });

    upload.addEventListener('change', function () {
      if (!upload.files.length) return;
      var body = new FormData();
      body.append('files', upload.files[0]);
      status.textContent = 'جارٍ الرفع…';
      post('/admin/media/upload', body)
        .then(function (r) { return r.json().then(function (data) { return { ok: r.ok, data: data }; }); })
        .then(function (res) {
          upload.value = '';
          if (!res.ok || !res.data.items || !res.data.items.length) {
            status.textContent = (res.data.errors || ['تعذّر رفع الملف.']).join(' ');
            return;
          }
          var item = res.data.items[0];
          if ((item.kind === 'video') !== (kind() === 'Video')) {
            status.textContent = 'رُفع الملف إلى المكتبة، لكنه ليس من النوع المطلوب هنا.';
            load();
            return;
          }
          choose(item);
        })
        .catch(function () { status.textContent = 'تعذّر رفع الملف.'; upload.value = ''; });
    });
  }

  // ---------- drag-and-drop ordering ----------
  document.querySelectorAll('table[data-reorder] tbody').forEach(function (body) {
    var table = body.closest('table');
    var dragged = null;
    body.querySelectorAll('tr').forEach(function (row) {
      var handle = row.querySelector('[data-drag]');
      if (!handle) return;
      handle.addEventListener('mousedown', function () { row.draggable = true; });
      handle.addEventListener('touchstart', function () { row.draggable = true; }, { passive: true });
      row.addEventListener('dragstart', function (e) {
        dragged = row;
        row.classList.add('dragging');
        e.dataTransfer.effectAllowed = 'move';
        e.dataTransfer.setData('text/plain', row.getAttribute('data-id'));
      });
      row.addEventListener('dragend', function () {
        row.classList.remove('dragging');
        row.draggable = false;
        body.querySelectorAll('.drop-before').forEach(function (r) { r.classList.remove('drop-before'); });
        if (!dragged) return;
        dragged = null;
        save();
      });
      row.addEventListener('dragover', function (e) {
        if (!dragged || dragged === row) return;
        e.preventDefault();
        var rect = row.getBoundingClientRect();
        var after = e.clientY > rect.top + rect.height / 2;
        body.insertBefore(dragged, after ? row.nextSibling : row);
      });
    });

    function save() {
      var ids = Array.prototype.map.call(body.querySelectorAll('tr[data-id]'), function (r) { return r.getAttribute('data-id'); });
      var data = new FormData();
      ids.forEach(function (id) { data.append('ids', id); });
      post(table.getAttribute('data-reorder'), data)
        .then(function (r) {
          if (!r.ok) return r.json().then(function (d) { throw new Error(d.message || ''); });
          // the up/down buttons follow the new order after a reload
          window.location.reload();
        })
        .catch(function (err) {
          window.alert(err.message || 'تعذّر حفظ الترتيب.');
          window.location.reload();
        });
    }
  });

  // ---------- library uploads (drop zone) ----------
  var zone = document.querySelector('[data-dropzone]');
  if (zone) {
    var input = zone.querySelector('[data-autosubmit]');
    var zoneStatus = zone.querySelector('[data-dropzone-status]');
    function send(files) {
      if (!files.length) return;
      zoneStatus.textContent = 'جارٍ رفع ' + files.length + ' ملف…';
      zone.submit();
    }
    input.addEventListener('change', function () { send(input.files); });
    ['dragenter', 'dragover'].forEach(function (t) {
      zone.addEventListener(t, function (e) { e.preventDefault(); zone.classList.add('over'); });
    });
    ['dragleave', 'drop'].forEach(function (t) {
      zone.addEventListener(t, function () { zone.classList.remove('over'); });
    });
    zone.addEventListener('drop', function (e) {
      e.preventDefault();
      if (!e.dataTransfer.files.length) return;
      input.files = e.dataTransfer.files;
      send(input.files);
    });
  }
})();
