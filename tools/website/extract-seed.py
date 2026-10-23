"""
Builds the website seed (seed-content.json) from the two approved prototypes, so no copy is typed by hand.

  python -I tools/website/extract-seed.py

Reads  YelG.WebSite/docs/design/B-Ar.dc.html and B-En.dc.html (never modified)
Writes YelG.WebSite.Infrastructure/Seeding/Data/seed-content.json

Both files have the same markup structure, so each text is read from the same element in both and paired
as {"ar": ..., "en": ...}. Inline formatting becomes the site's small markup:
  <em> / the cyan span of the h1  ->  *text*
  <br>                            ->  newline
  <a href="#x">t</a>             ->  [t](#x)
  <bdi>, <span class="sr">       ->  plain text (Latin isolation is re-applied when rendering Arabic)
The data arrays at the top of each script (CATS, SERVICES, QUIZ, OPTS) are evaluated with Node.
"""
import json
import os
import re
import subprocess
import sys

from lxml import html

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
DESIGN = os.path.join(ROOT, 'YelG.WebSite', 'docs', 'design')
OUT = os.path.join(ROOT, 'YelG.WebSite.Infrastructure', 'Seeding', 'Data', 'seed-content.json')

# CSS image classes of the design -> files in wwwroot/assets
IMAGE_CLASSES = {
    'i-meeting': 'meeting.jpg', 'i-desk': 'desk.jpg', 'i-smiles': 'post-from-numbers-to-smiles.jpg',
    'i-hiring': 'post-hiring.jpg', 'i-insight': 'post-insight-to-action.jpg',
    'i-lighthouse': 'post-finance-should-do-more.jpg', 'i-ifc1': 'post-infinite-financial-care.jpg',
    'i-avatar': 'dr-yasser-avatar.jpg', 'yasserArch': 'dr-yasser-cutout.png',
}


def load(name):
    raw = open(os.path.join(DESIGN, name), encoding='utf-8').read()
    body = raw[raw.index('</helmet>') + len('</helmet>'):raw.index('</x-dc>')]
    script = raw[raw.index('<script type="text/x-dc"'):]
    tree = html.fromstring('<div id="__root">' + body + '</div>')
    return raw, tree, script


def squash(text):
    return re.sub(r'[ \t\r\f\v]+', ' ', re.sub(r' *\n *', '\n', text)).strip()


def esc(text):
    """A literal asterisk in the copy ("Fields marked * are required") is escaped (backslash-asterisk) so it is not a highlight."""
    return (text or '').replace('*', chr(92) + '*')


def markup(el):
    """Inner content of an element as the site's markup."""
    out = [esc(el.text)]
    for child in el:
        tag = child.tag if isinstance(child.tag, str) else ''
        cls = child.get('class') or ''
        if tag == 'br':
            out.append('\n')
        elif tag in ('svg',) or child.get('aria-hidden') == 'true' or 'sr' in cls.split():
            # decorative marks, and screen-reader-only phrases ("opens in a new tab"): those live in the
            # site's UI resources, not in the editable copy
            pass
        elif tag == 'em' or (tag == 'span' and '#44D2EA' in (child.get('style') or '')):
            out.append('*' + markup(child) + '*')
        elif tag == 'a' and (child.get('href') or '').startswith(('#', 'http', 'mailto:')):
            out.append('[' + markup(child) + '](' + child.get('href') + ')')
        else:
            out.append(markup(child))
        out.append(esc(child.tail))
    return squash(''.join(out))


def plain(el):
    """Text without the site's markup: [label](#x) -> label, highlight marks removed, an escaped asterisk kept as *."""
    text = re.sub(r'\[([^\]]*)\]\([^)]*\)', r'\1', markup(el))
    escaped_star = chr(92) + '*'
    text = text.replace(escaped_star, '\x00').replace('*', '').replace('\x00', '*')
    return squash(text)


class Pair:
    """Runs the same XPath on both prototypes."""

    def __init__(self):
        self.ar_raw, self.ar, self.ar_js = load('B-Ar.dc.html')
        self.en_raw, self.en, self.en_js = load('B-En.dc.html')

    def nodes(self, xp):
        a, e = self.ar.xpath(xp), self.en.xpath(xp)
        if len(a) != len(e) or not a:
            raise SystemExit(f'XPath {xp}: {len(a)} Arabic vs {len(e)} English nodes')
        return list(zip(a, e))

    def one(self, xp, fn=markup):
        (a, e), = self.nodes(xp)
        return {'ar': fn(a), 'en': fn(e)}

    def many(self, xp, fn=markup):
        return [{'ar': fn(a), 'en': fn(e)} for a, e in self.nodes(xp)]

    def attr(self, xp, name):
        (a, e), = self.nodes(xp)
        return {'ar': a.get(name) or '', 'en': e.get(name) or ''}

    def same(self, xp, fn=plain):
        """A value that must be identical in both languages (URLs, emails, codes)."""
        (a, e), = self.nodes(xp)
        va, ve = fn(a), fn(e)
        if va != ve:
            raise SystemExit(f'{xp}: differs between languages: {va!r} / {ve!r}')
        return va


def data_arrays(js):
    """Evaluates the var declarations (CATS … TO) of a prototype script with Node and returns them as JSON."""
    start = js.index('var CATS')
    end = js.index('class Component')
    code = js[start:end] + '\nprocess.stdout.write(JSON.stringify({CATS, SERVICES, QUIZ, OPTS, TO}));'
    res = subprocess.run(['node', '-e', code], capture_output=True, text=True, encoding='utf-8', check=True)
    return json.loads(res.stdout)


def logic_strings(js):
    """Band texts, the self-check prefill message and the 'not sure' value from the component code."""
    bands = re.findall(r"\['([^']+)', '([^']+)'\]", js[js.index('var band'):js.index('var recNames')])
    thresholds = [int(x) for x in re.findall(r'score >= (\d+)', js)] + [0]
    prefill = re.search(r"discussScore: function \(\) \{ self\.prefill\(st\.form\.service \|\| '([^']+)', '([^']*)' \+ score \+ '([^']*)' \+ band\[0\] \+ '([^']*)'\)", js)
    validation = dict(re.findall(r"e\.(\w+) = '([^']+)'", js))
    return {
        'bands': [{'minScore': m, 'title': t, 'text': x} for m, (t, x) in zip(thresholds, bands)],
        'notSureValue': prefill.group(1),
        'prefillParts': [prefill.group(2), prefill.group(3), prefill.group(4)],
        'validation': validation,
    }


def image_file(el):
    for cls in (el.get('class') or '').split():
        if cls in IMAGE_CLASSES:
            return IMAGE_CLASSES[cls]
    raise SystemExit(f'No image class on {html.tostring(el)[:120]}')


def main():
    p = Pair()
    sec = lambda sid: f'//section[@id="{sid}"]'
    head = lambda base: {
        'eyebrow': p.one(f'({base}//*[contains(@class,"eyebrow")])[1]'),
        'heading': p.one(f'({base}//h2)[1]'),
        'hideHeadingBreakOnMobile': bool(p.ar.xpath(f'({base}//h2)[1]/br[contains(@class,"hide-sm")]')),
    }

    seed = {}

    # ---------------- site ----------------
    ar_title = re.search(r'<title>(.*?)</title>', p.ar_raw).group(1)
    en_title = re.search(r'<title>(.*?)</title>', p.en_raw).group(1)
    seed['site'] = {
        'tagline': p.one('//footer//p[1]'),
        'contactEmail': p.same('//section[@id="consult"]//a[starts-with(@href,"mailto:")]'),
        'phone': p.same('//section[@id="consult"]//a[starts-with(@href,"tel:")]'),
        'websiteUrl': p.same('//footer//a[contains(@href,"yelgadvisory.com") and starts-with(@href,"http")]', fn=lambda a: a.get('href')),
        'linkedInUrl': p.same('//footer//a[contains(@href,"linkedin.com")]', fn=lambda a: a.get('href')),
        'careersEmail': p.same('//*[@id="careers"]//a[starts-with(@href,"mailto:")]', fn=lambda a: a.get('href').split(':', 1)[1].split('?')[0]),
        'seoTitle': {'ar': ar_title, 'en': en_title},
        # No meta description in the design: the hero supporting text is used (existing, approved copy).
        'seoDescription': p.one('//section[@id="top"]//h1/following-sibling::p[1]', fn=plain),
        'copyright': p.one('//footer//div[contains(@style,"justify-content: space-between")]/span[1]'),
    }
    seed['site']['copyright'] = {k: v.replace('2026', '{year}', 1) for k, v in seed['site']['copyright'].items()}

    # ---------------- hero ----------------
    hero = sec('top')
    seed['hero'] = {
        'eyebrow': p.one(f'{hero}//div[contains(@class,"heroGrid")]/div[1]/span[1]'),
        'heading': p.one(f'{hero}//h1'),
        'hideHeadingBreakOnMobile': False,
        'lead': p.one(f'{hero}//h1/following-sibling::p[1]'),
        'primaryCtaLabel': p.one(f'{hero}//div[contains(@class,"heroCtas")]/a[1]'),
        'primaryCtaTarget': p.same(f'{hero}//div[contains(@class,"heroCtas")]/a[1]', fn=lambda a: a.get('href')),
        'secondaryCtaLabel': p.one(f'{hero}//div[contains(@class,"heroCtas")]/a[2]'),
        'secondaryCtaTarget': p.same(f'{hero}//div[contains(@class,"heroCtas")]/a[2]', fn=lambda a: a.get('href')),
        'selfCheckLinkText': p.one(f'{hero}//div[contains(@class,"heroCtas")]/following-sibling::a[1]'),
        'image': image_file(p.ar.xpath(f'{hero}//div[contains(@class,"heroArch")]//*[contains(@class,"photo")]')[0]),
        'imageAlt': p.attr(f'{hero}//div[contains(@class,"heroArch")]//*[contains(@class,"photo")]', 'aria-label'),
        'badgeRingText': p.same(f'{hero}//textpath'),
    }
    stats = []
    for i, (a, e) in enumerate(p.nodes(f'{hero}//dl[contains(@class,"heroStats")]/div'), 1):
        dd_a, dd_e = a.xpath('./dd')[0], e.xpath('./dd')[0]
        dt = {'ar': plain(a.xpath('./dt')[0]), 'en': plain(e.xpath('./dt')[0])}
        if dd_a.xpath('./div[contains(@class,"num")]'):
            stats.append({'kind': 'Number', 'value': plain(dd_a.xpath('./div')[0]), 'screenReaderLabel': dt,
                          'caption': {'ar': markup(dd_a.xpath('./p')[0]), 'en': markup(dd_e.xpath('./p')[0])}, 'chipCodes': ''})
        else:
            chips_a = [plain(s) for s in dd_a.xpath('./span[2]/span')]
            chips_e = [plain(s) for s in dd_e.xpath('./span[2]/span')]
            assert chips_a == chips_e, (chips_a, chips_e)
            stats.append({'kind': 'Chips', 'value': '', 'screenReaderLabel': dt, 'chipCodes': ','.join(chips_a),
                          'caption': {'ar': plain(dd_a.xpath('./span[1]')[0]), 'en': plain(dd_e.xpath('./span[1]')[0])}})
    seed['heroStats'] = stats

    # ---------------- marquee ----------------
    strip = f'{hero}/div[contains(@style,"#44D2EA")]'
    seed['marquee'] = {
        'screenReaderText': p.one(f'{strip}/p[contains(@class,"sr")]', fn=plain),
        # first copy of the repeated strip, without the ✦ separators
        'items': [x for x in p.many(f'{strip}//div[contains(@class,"marquee")]/span[1]/span', fn=plain) if x['ar'] != '✦'],
    }

    # ---------------- about + founder ----------------
    about = sec('about')
    text_col = f'{about}/div/div[1]/div[2]'
    collage = f'{about}//div[contains(@class,"collage")]'
    photos = p.ar.xpath(f'{collage}/div[contains(@class,"photo")]')
    seed['about'] = {
        **head(about),
        'hideHeadingBreakOnMobile': False,
        'lead': p.one(f'{text_col}/p[contains(@class,"p")]'),
        'credentialsLabel': p.one(f'{text_col}//span[contains(@class,"lbl")]'),
        'image1': image_file(photos[0]), 'image1Alt': p.attr(f'({collage}/div[contains(@class,"photo")])[1]', 'aria-label'),
        'image2': image_file(photos[1]), 'image2Alt': p.attr(f'({collage}/div[contains(@class,"photo")])[2]', 'aria-label'),
        'badgeRingText': p.same(f'{collage}//textpath'),
    }
    seed['aboutValues'] = p.many(f'{text_col}//span[contains(@class,"chk")]', fn=plain)
    creds = []
    for a, e in p.nodes(f'{text_col}//span[contains(@class,"cred")]'):
        code_a, code_e = plain(a.xpath('./b')[0]), plain(e.xpath('./b')[0])
        assert code_a == code_e
        creds.append({'code': code_a, 'description': {'ar': plain(a.xpath('./span')[0]), 'en': plain(e.xpath('./span')[0])}})
    seed['credentials'] = creds

    fig = f'{about}//figure'
    seed['founder'] = {
        'eyebrow': p.one(f'{fig}//span[contains(@class,"eyebrow")]'),
        'quote': p.one(f'{fig}//blockquote'),
        'supportingLine': p.one(f'{fig}//blockquote/following-sibling::p[1]'),
        'name': p.one(f'{fig}//figcaption//strong'),
        'title': p.one(f'{fig}//figcaption//strong/following-sibling::span[1]'),
        'photo': image_file(p.ar.xpath(f'{fig}//*[contains(@class,"yasserArch")]')[0]),
        'photoAlt': p.attr(f'{fig}//*[contains(@class,"yasserArch")]', 'aria-label'),
        'avatar': image_file(p.ar.xpath(f'{fig}//*[contains(@class,"i-avatar")]')[0]),
    }

    # ---------------- services ----------------
    svc = sec('services')
    data_ar, data_en = data_arrays(p.ar_js), data_arrays(p.en_js)
    seed['servicesSection'] = {
        **head(svc),
        'lead': p.one(f'{svc}//p[contains(@class,"lead")]'),
        'allTabLabel': {'ar': data_ar['CATS'][0]['label'], 'en': data_en['CATS'][0]['label']},
        'cardLinkLabel': p.one(f'{svc}//article/a[contains(@class,"tlink")]', fn=lambda a: squash((a.text or ''))),
        'scopeHeading': p.one(f'{svc}//section[@id="svcDetail"]//h4'),
        'discussCtaLabel': p.one(f'{svc}//section[@id="svcDetail"]//a[contains(@class,"btn")]'),
    }
    seed['serviceCategories'] = [{'key': a['id'], 'name': {'ar': a['label'], 'en': e['label']}}
                                 for a, e in zip(data_ar['CATS'][1:], data_en['CATS'][1:])]
    services = []
    for a, e in zip(data_ar['SERVICES'], data_en['SERVICES']):
        assert a['id'] == e['id'] and a['cat'] == e['cat'] and len(a['items']) == len(e['items'])
        services.append({'key': a['id'], 'iconKey': a['id'], 'category': a['cat'],
                         'name': {'ar': a['name'], 'en': e['name']},
                         'shortDescription': {'ar': a['desc'], 'en': e['desc']},
                         'longDescription': {'ar': a['long'], 'en': e['long']},
                         'deliverables': [{'ar': x, 'en': y} for x, y in zip(a['items'], e['items'])]})
    seed['services'] = services

    # ---------------- why ----------------
    why = sec('why')
    seed['why'] = {
        **head(why),
        'lead': p.one(f'{why}//p[contains(@class,"lead")]'),
        'image': image_file(p.ar.xpath(f'{why}//*[contains(@class,"photo")]')[0]),
        'imageAlt': p.attr(f'{why}//*[contains(@class,"photo")]', 'aria-label'),
        'floatingCardText': p.one(f'{why}//*[contains(@class,"photo")]/following-sibling::div[1]/span[2]'),
    }
    seed['whyCards'] = [{'title': {'ar': plain(a.xpath('./b')[0]), 'en': plain(e.xpath('./b')[0])},
                         'text': {'ar': markup(a.xpath('./span')[0]), 'en': markup(e.xpath('./span')[0])},
                         'isHighlighted': 'background' in (a.get('style') or '')}
                        for a, e in p.nodes(f'{why}//div[contains(@class,"why")]')]

    # ---------------- how ----------------
    how = sec('how')
    seed['how'] = {**head(how), 'lead': p.one(f'{how}//p[contains(@class,"lead")]')}
    seed['howSteps'] = [{'title': {'ar': plain(a.xpath('.//h3')[0]), 'en': plain(e.xpath('.//h3')[0])},
                         'text': {'ar': markup(a.xpath('.//p')[0]), 'en': markup(e.xpath('.//p')[0])},
                         'isHighlighted': 'background' in (a.xpath('./span')[0].get('style') or '')}
                        for a, e in p.nodes(f'{how}//li[contains(@class,"step")]')]

    # ---------------- purpose ----------------
    pur = '//section[@aria-labelledby="purposeTitle"]'
    tabs = p.many(f'{pur}//button[@role="tab"]', fn=plain)
    seed['purpose'] = {
        **head(pur),
        'hideHeadingBreakOnMobile': False,
        'lead': {'ar': '', 'en': ''},
        'missionTabLabel': tabs[0], 'visionTabLabel': tabs[1], 'goalsTabLabel': tabs[2],
        'mission': p.one(f'({pur}//div[@id="pp"]/sc-if)[1]/p'),
        'vision': p.one(f'({pur}//div[@id="pp"]/sc-if)[2]/p'),
        'video': os.path.basename(p.same(f'{pur}//video', fn=lambda v: v.get('src'))),
        'poster': os.path.basename(p.same(f'{pur}//video', fn=lambda v: v.get('poster'))),
        'videoLabel': p.attr(f'{pur}//video', 'aria-label'),
    }
    seed['purposeGoals'] = p.many(f'({pur}//div[@id="pp"]/sc-if)[3]//span', fn=plain)

    # ---------------- engage ----------------
    eng = sec('engage')
    js_prefill_ar = dict(re.findall(r"(w\d): function \(\) \{ self\.prefill\('([^']+)'\)", p.ar_js))
    not_sure_ar = logic_strings(p.ar_js)['notSureValue']
    names_ar = {s['name']['ar']: s['key'] for s in services}
    models = []
    for a, e in p.nodes(f'{eng}//div[contains(@class,"way")]'):
        btn_a, btn_e = a.xpath('./a')[0], e.xpath('./a')[0]
        handler = re.search(r'\{\{(w\d)\}\}', btn_a.get('onclick') or btn_a.get('onClick') or '').group(1)
        target = js_prefill_ar[handler]
        icon = [c[3:] for c in (a.xpath('./span')[0].get('class') or '').split() if c.startswith('ic-')][0]
        models.append({
            'iconKey': icon,
            'title': {'ar': plain(a.xpath('./h3')[0]), 'en': plain(e.xpath('./h3')[0])},
            'text': {'ar': markup(a.xpath('./p')[0]), 'en': markup(e.xpath('./p')[0])},
            'bullets': [{'ar': plain(x), 'en': plain(y)} for x, y in zip(a.xpath('./ul/li'), e.xpath('./ul/li'))],
            'ctaLabel': {'ar': plain(btn_a), 'en': plain(btn_e)},
            'preselectService': None if target == not_sure_ar else names_ar[target],
            'preselectNotSure': target == not_sure_ar,
            'isHighlighted': 'way-dark' in (a.get('class') or ''),
        })
    seed['engage'] = {**head(eng), 'lead': {'ar': '', 'en': ''}}
    seed['engagementModels'] = models

    # ---------------- industries ----------------
    ind = sec('industries')
    seed['industries'] = {**head(ind), 'lead': p.one(f'{ind}//p[contains(@class,"lead")]'),
                          'approvalNote': p.one(f'{ind}/div/p')}
    seed['sectors'] = [{'label': {'ar': plain(a), 'en': plain(e)}, 'isEmphasized': 'background' in (a.get('style') or '')}
                       for a, e in p.nodes(f'{ind}//ul/li')]
    icon_by_path = {'M3 10l9-6': 'bank', 'M2 9l10-5': 'edu', 'x="4" y="3" width="10"': 'corp'}
    color_by_bg = {'#EEF3FF': 'blue', '#FFF1EA': 'coral', '#E6FAFD': 'cyan'}
    cats = []
    for a, e in p.nodes(f'{ind}/div/div[2]/div'):
        svg = html.tostring(a.xpath('./span[1]')[0]).decode()
        icon = next(v for k, v in icon_by_path.items() if k in svg)
        color = next(v for k, v in color_by_bg.items() if k in (a.xpath('./span[1]')[0].get('style') or ''))
        cats.append({'iconKey': icon, 'colorVariant': color,
                     'title': {'ar': plain(a.xpath('.//h3')[0]), 'en': plain(e.xpath('.//h3')[0])},
                     'text': {'ar': plain(a.xpath('./span[2]/span')[0]), 'en': plain(e.xpath('./span[2]/span')[0])}})
    seed['clientCategories'] = cats

    # ---------------- self-check ----------------
    hc = sec('health')
    logic_ar, logic_en = logic_strings(p.ar_js), logic_strings(p.en_js)
    q_ar, q_en = data_ar['QUIZ'], data_en['QUIZ']
    names_by_lang = {s['name']['ar']: s['key'] for s in services}
    seed['selfCheck'] = {
        **head(hc),
        'lead': p.one(f'{hc}//p[contains(@class,"lead")]'),
        'disclaimer1': p.one(f'({hc}//div[contains(@class,"head")]/ul/li)[1]', fn=plain),
        'disclaimer2': p.one(f'({hc}//div[contains(@class,"head")]/ul/li)[2]', fn=plain),
        'pickHint': p.one(f'{hc}//button[contains(@class,"tlink")]/following-sibling::span[1]'),
        'backLabel': p.one(f'{hc}//button[contains(@class,"tlink")]'),
        'resultLabel': p.one(f'{hc}//div[@role="status"]/span[1]'),
        'recommendationsLabel': p.one(f'{hc}//div[@role="status"]//span[contains(@class,"lbl")]'),
        'methodNote': p.one(f'{hc}//div[@role="status"]/p[last()]'),
        'discussCtaLabel': p.one(f'{hc}//div[@role="status"]//a[contains(@class,"btn")]'),
        'restartLabel': p.one(f'{hc}//div[@role="status"]//button'),
        'prefillTemplate': {
            'ar': logic_ar['prefillParts'][0] + '{score}' + logic_ar['prefillParts'][1] + '{band}' + logic_ar['prefillParts'][2],
            'en': logic_en['prefillParts'][0] + '{score}' + logic_en['prefillParts'][1] + '{band}' + logic_en['prefillParts'][2],
        },
    }
    seed['selfCheckQuestions'] = [{'area': {'ar': a['area'], 'en': e['area']}, 'text': {'ar': a['q'], 'en': e['q']},
                                   'linkedService': names_by_lang[a['svc']]} for a, e in zip(q_ar, q_en)]
    seed['selfCheckOptions'] = [{'label': {'ar': a['l'], 'en': e['l']}, 'score': a['v']} for a, e in zip(data_ar['OPTS'], data_en['OPTS'])]
    seed['selfCheckBands'] = [{'minScore': a['minScore'], 'title': {'ar': a['title'], 'en': e['title']}, 'text': {'ar': a['text'], 'en': e['text']}}
                              for a, e in zip(logic_ar['bands'], logic_en['bands'])]

    # ---------------- faq ----------------
    faq = sec('faq')
    seed['faq'] = {**head(faq), 'lead': p.one(f'{faq}//p[contains(@class,"lead")]')}
    items = []
    for a, e in p.nodes(f'{faq}//details'):
        items.append({'question': {'ar': markup(a.xpath('./summary')[0]), 'en': markup(e.xpath('./summary')[0])},
                      'answer': {'ar': markup(a.xpath('./p')[0]), 'en': markup(e.xpath('./p')[0])},
                      'isOpenByDefault': a.get('open') is not None})
    seed['faqItems'] = items

    # ---------------- consultation ----------------
    con = sec('consult')
    panel = f'{con}/div/div[1]/div[2]'
    seed['consult'] = {
        'eyebrow': p.one(f'{panel}/span[contains(@class,"eyebrow")]'),
        'heading': p.one(f'{panel}/h2'),
        'hideHeadingBreakOnMobile': True,
        'lead': p.one(f'{panel}/p'),
        'directContactLabel': p.one(f'{panel}/div[last()]/span[1]'),
        'formTitle': p.one(f'{con}//form//h3'),
        'requiredNote': p.one(f'{con}//form/div[1]/p'),
        'consentText': p.one(f'{con}//label[contains(@class,"consent")]/span'),
        'notSureLabel': p.one(f'{con}//select/option[last()]', fn=plain),
        'submitLabel': p.one(f'{con}//form//button[@type="submit"]'),
        # The design only prepares a mailto message; real success / failure copy needs company approval.
        'successTitle': {'ar': '', 'en': ''}, 'successText': {'ar': '', 'en': ''}, 'failureText': {'ar': '', 'en': ''},
    }
    steps = []
    for a, e in p.nodes(f'{panel}/ol/li'):
        sa, se = a.xpath('./span[2]')[0], e.xpath('./span[2]')[0]
        # <strong>title</strong><br>text
        steps.append({'title': {'ar': plain(sa.xpath('./strong')[0]), 'en': plain(se.xpath('./strong')[0])},
                      'text': {'ar': squash(sa.xpath('./br[1]')[0].tail or ''), 'en': squash(se.xpath('./br[1]')[0].tail or '')}})
    seed['consultSteps'] = steps
    seed['validation'] = {'ar': logic_ar['validation'], 'en': logic_en['validation']}
    seed['notSureValue'] = {'ar': logic_ar['notSureValue'], 'en': logic_en['notSureValue']}

    # ---------------- insights ----------------
    ins = sec('insights')
    seed['insights'] = {**head(ins), 'hideHeadingBreakOnMobile': False, 'lead': {'ar': '', 'en': ''},
                        'followLinkLabel': p.one(f'{ins}/div/div[1]/a', fn=lambda a: squash(re.sub(r'[↖↗]', '', plain(a)))),
                        'followLinkUrl': p.same(f'{ins}/div/div[1]/a', fn=lambda a: a.get('href'))}
    posts = []
    month = {'يناير': 1, 'فبراير': 2, 'مارس': 3, 'أبريل': 4, 'مايو': 5, 'يونيو': 6, 'يوليو': 7, 'أغسطس': 8, 'سبتمبر': 9,
             'أكتوبر': 10, 'نوفمبر': 11, 'ديسمبر': 12}
    for a, e in p.nodes(f'{ins}//a[contains(@class,"post")]'):
        meta_a, meta_e = plain(a.xpath('./span[2]')[0]), plain(e.xpath('./span[2]')[0])
        cat_a, src, when = [x.strip() for x in meta_a.split('·')]
        cat_e = meta_e.split('·')[0].strip()
        mname, year = when.split()
        posts.append({'image': image_file(a.xpath('./span[1]')[0]), 'imageAlt': {'ar': a.xpath('./span[1]')[0].get('aria-label'), 'en': e.xpath('./span[1]')[0].get('aria-label')},
                      'categoryLabel': {'ar': cat_a, 'en': cat_e}, 'source': src,
                      'categoryColor': 'coral' if '#B4532F' in (a.xpath('./span[2]')[0].get('style') or '') else 'blue',
                      'publishedOn': f'{year}-{month[mname]:02d}-01',
                      'title': {'ar': plain(a.xpath('./span[3]')[0]), 'en': plain(e.xpath('./span[3]')[0])},
                      'externalUrl': a.get('href')})
    seed['insightPosts'] = posts

    # ---------------- careers + portal ----------------
    car = '//div[@id="careers"]'
    links = p.nodes(f'{car}//a[contains(@class,"tlink")]')
    seed['careers'] = {
        'eyebrow': p.one(f'{car}//span[contains(@class,"eyebrow")]'),
        'heading': p.one(f'{car}//h3'), 'hideHeadingBreakOnMobile': False,
        'lead': p.one(f'{car}//h3/following-sibling::p[1]'),
        'image': image_file(p.ar.xpath(f'{car}/span[contains(@class,"photo")]')[0]),
        'imageAlt': p.attr(f'{car}/span[contains(@class,"photo")]', 'aria-label'),
        'sendCvLabel': {'ar': squash(re.sub('←|→', '', plain(links[0][0]))), 'en': squash(re.sub('←|→', '', plain(links[0][1])))},
        'cvEmailSubject': __import__('urllib.parse', fromlist=['unquote']).unquote(links[0][0].get('href').split('subject=')[1]),
        'jobsLinkLabel': {'ar': squash(links[1][0].text or ''), 'en': squash(links[1][1].text or '')},
        'jobsUrl': links[1][0].get('href'),
    }
    por = '//div[@id="portal"]'
    seed['portal'] = {'badgeLabel': p.one(f'{por}/span'), 'title': p.one(f'{por}/h3'), 'text': p.one(f'{por}/p'),
                      'status': 'ComingSoon', 'signInLabel': {'ar': '', 'en': ''}, 'portalUrl': None}

    seed['media'] = sorted(set(IMAGE_CLASSES.values()) | {seed['purpose']['video'], seed['purpose']['poster']})

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, 'w', encoding='utf-8') as f:
        json.dump(seed, f, ensure_ascii=False, indent=1)
    print('written', os.path.relpath(OUT, ROOT), '-', sum(1 for _ in json.dumps(seed)), 'chars')


if __name__ == '__main__':
    sys.stdout.reconfigure(encoding='utf-8')
    main()
