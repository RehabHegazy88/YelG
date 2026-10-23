"""
Renders the approved Design Component prototypes as plain static HTML in their initial state, so the live
site can be compared with them screenshot by screenshot (the DC runtime, support.js, is not available).

  python -I tools/website/reference-render.py <out-dir>

Writes <out-dir>/ref-ar.html and ref-en.html and copies wwwroot/assets to <out-dir>/assets. Serve the folder
(python -m http.server) and open /ref-ar.html. The initial state is the component's constructor state:
first purpose tab, "All services", no service open, question 1, empty form, mobile menu closed.
"""
import json
import os
import re
import shutil
import subprocess
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
DESIGN = os.path.join(ROOT, 'YelG.WebSite', 'docs', 'design')
ASSETS = os.path.join(ROOT, 'YelG.WebSite', 'wwwroot', 'assets')


def data(script):
    start, end = script.index('var CATS'), script.index('class Component')
    code = script[start:end] + '\nprocess.stdout.write(JSON.stringify({CATS, SERVICES, QUIZ, OPTS}));'
    return json.loads(subprocess.run(['node', '-e', code], capture_output=True, text=True, encoding='utf-8', check=True).stdout)


def state(d):
    """Values of the template holes in the initial state (mirrors renderVals())."""
    cats = d['CATS']
    label = {c['id']: c['label'] for c in cats}
    q0 = d['QUIZ'][0]
    holes = {
        'menuOpen': False, 'menuExpanded': 'false', 't0': 'true', 't1': 'false', 't2': 'false',
        'c0': 'tabbtn tabbtn-on', 'c1': 'tabbtn', 'c2': 'tabbtn', 's0': True, 's1': False, 's2': False, 'ptab': 'pt0',
        'activeTabId': 'cat-all', 'hasDetail': False, 'quizRunning': True, 'quizDone': False,
        'qNum': '1', 'qArea': q0['area'], 'qText': q0['q'], 'answeredCount': '0', 'barStyle': 'width: 0%',
        'noBack': True, 'backOpacity': '0.4', 'hasRecs': False, 'showErrSummary': False, 'errCount': '0',
        'ready': False, 'notReady': True, 'score': '0',
    }
    for k in ('name', 'email', 'company', 'phone', 'service', 'message', 'consent'):
        holes[f'e.{k}'] = False
        holes[f'inv.{k}'] = 'false'
        holes[f'f.{k}'] = False if k == 'consent' else ''
    lists = {
        'cats': [{'tabId': 'cat-' + c['id'], 'label': c['label'],
                  'count': str(len(d['SERVICES']) if c['id'] == 'all' else sum(1 for s in d['SERVICES'] if s['cat'] == c['id'])),
                  'sel': 'true' if c['id'] == 'all' else 'false', 'cls': 'ctab ctab-on' if c['id'] == 'all' else 'ctab'} for c in cats],
        'svcList': [{'name': s['name'], 'desc': s['desc'], 'catLabel': label[s['cat']], 'iconCls': 'ico ic-' + s['id'],
                     'cardCls': 'svc', 'expanded': 'false'} for s in d['SERVICES']],
        'qOpts': [{'l': o['l'], 'cls': 'opt', 'pressed': 'false'} for o in d['OPTS']],
        'recs': [], 'cur.items': [],
    }
    return holes, lists


def esc(v):
    return str(v).replace('&', '&amp;').replace('<', '&lt;').replace('>', '&gt;').replace('"', '&quot;')


def fill(html, holes, scope=None):
    scope = scope or {}

    def value(name):
        name = name.strip()
        if name in ('true', 'false'):
            return name == 'true'
        if '.' in name and name.split('.')[0] in scope:
            head, tail = name.split('.', 1)
            return scope[head].get(tail, '')
        return holes.get(name, '')

    # boolean attributes: x="{{ true }}" -> x ; x="{{false-ish}}" -> removed
    def attr(m):
        name, expr = m.group(1), m.group(2)
        if name.lower().startswith('on'):
            return ''  # event handlers
        v = value(expr)
        if isinstance(v, bool):
            return f' {name}' if v else ''
        return f' {name}="{esc(v)}"'

    html = re.sub(r'\s([A-Za-z-]+)="\{\{([^}]*)\}\}"', attr, html)
    return re.sub(r'\{\{([^}]*)\}\}', lambda m: esc(value(m.group(1))), html)


def expand(html, holes, lists):
    # sc-for: repeat the body per item (innermost first)
    for_rx = re.compile(r'<sc-for list="\{\{([^}]+)\}\}" as="(\w+)"[^>]*>((?:(?!<sc-for).)*?)</sc-for>', re.S)
    while True:
        m = for_rx.search(html)
        if not m:
            break
        items = lists.get(m.group(1).strip(), [])
        body = ''.join(fill(m.group(3), holes, {m.group(2): it}) for it in items)
        html = html[:m.start()] + body + html[m.end():]
    # sc-if: keep the body when the value is truthy (innermost first)
    if_rx = re.compile(r'<sc-if value="\{\{([^}]+)\}\}"[^>]*>((?:(?!<sc-if).)*?)</sc-if>', re.S)
    while True:
        m = if_rx.search(html)
        if not m:
            break
        keep = holes.get(m.group(1).strip(), False)
        html = html[:m.start()] + (m.group(2) if keep else '') + html[m.end():]
    return fill(html, holes)


def render(name, out):
    raw = open(os.path.join(DESIGN, name), encoding='utf-8').read()
    script = raw[raw.index('<script type="text/x-dc"'):]
    holes, lists = state(data(script))
    helmet = raw[raw.index('<helmet>') + len('<helmet>'):raw.index('</helmet>')]
    body = raw[raw.index('</helmet>') + len('</helmet>'):raw.index('</x-dc>')]
    head = raw[:raw.index('<script src="./support.js"></script>')]
    page = head + helmet + '</head>\n<body>\n' + expand(body, holes, lists) + '\n</body>\n</html>\n'
    page = page.replace('href="B-En.dc.html"', 'href="ref-en.html"').replace('href="B-Ar.dc.html"', 'href="ref-ar.html"')
    open(out, 'w', encoding='utf-8').write(page)
    left = re.findall(r'\{\{|<sc-(?:if|for)', page)
    print(os.path.basename(out), 'unresolved:', len(left))


def main():
    out = os.path.abspath(sys.argv[1])
    os.makedirs(out, exist_ok=True)
    render('B-Ar.dc.html', os.path.join(out, 'ref-ar.html'))
    render('B-En.dc.html', os.path.join(out, 'ref-en.html'))
    shutil.copytree(ASSETS, os.path.join(out, 'assets'), dirs_exist_ok=True)


if __name__ == '__main__':
    main()
