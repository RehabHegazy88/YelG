"""
Full-page screenshots through the Chrome DevTools Protocol, at exact CSS widths, for comparing the site with the
static reference of the prototypes (reference-render.py).

  python -I tools/website/screenshot.py <out-dir> <name>=<url> [<name>=<url> ...] [--widths 375,768,1024,1440]

Writes <out-dir>/<name>-<width>.png. The page is loaded at each width (phones below 768px are emulated as
mobile), fonts are awaited, the real document height is measured and the page is captured in tiles that are
stitched together, so tall phone pages are not cut off.
"""
import asyncio
import base64
import io
import json
import os
import shutil
import subprocess
import sys
import tempfile
import time
import urllib.request

import websockets
from PIL import Image

CHROME = r'C:\Program Files\Google\Chrome\Application\chrome.exe'
PORT = 9333
TILE = 4000


class Cdp:
    def __init__(self, ws):
        self.ws, self.next_id, self.events = ws, 0, []

    async def call(self, method, **params):
        self.next_id += 1
        my_id = self.next_id
        await self.ws.send(json.dumps({'id': my_id, 'method': method, 'params': params}))
        while True:
            msg = json.loads(await self.ws.recv())
            if msg.get('id') == my_id:
                if 'error' in msg:
                    raise RuntimeError(f'{method}: {msg["error"]}')
                return msg.get('result', {})
            self.events.append(msg)

    async def wait_event(self, name, timeout=30):
        end = time.time() + timeout
        while time.time() < end:
            if any(e.get('method') == name for e in self.events):
                self.events = [e for e in self.events if e.get('method') != name]
                return
            try:
                msg = json.loads(await asyncio.wait_for(self.ws.recv(), timeout=1))
                self.events.append(msg)
            except asyncio.TimeoutError:
                pass
        raise TimeoutError(name)


async def capture(cdp, url, width, path):
    mobile = width < 768
    await cdp.call('Emulation.setDeviceMetricsOverride', width=width, height=900, deviceScaleFactor=1, mobile=mobile)
    await cdp.call('Page.enable')
    await cdp.call('Page.navigate', url=url)
    await cdp.wait_event('Page.loadEventFired')
    await cdp.call('Runtime.evaluate', expression='document.fonts.ready.then(() => true)', awaitPromise=True)
    # walk down the page like a reader so lazy images load, wait for them, then return to the top
    await cdp.call('Runtime.evaluate', awaitPromise=True, expression=(
        "(async () => { document.documentElement.style.scrollBehavior = 'auto';"
        " for (let y = 0; y < document.documentElement.scrollHeight; y += innerHeight) {"
        "   scrollTo(0, y); await new Promise(r => setTimeout(r, 60)); }"
        " await Promise.all([...document.images].map(i => i.complete ? 0 : new Promise(r => { i.onload = i.onerror = r; })));"
        " scrollTo(0, 0); return true; })()"))
    # the marquee and the badges move: switch animations off so both captures show the same frame
    await cdp.call('Runtime.evaluate', expression=(
        "const s = document.createElement('style');"
        "s.textContent = '*,*::before,*::after{animation:none!important;transition:none!important}';"
        "document.head.appendChild(s); document.documentElement.style.scrollBehavior = 'auto'; true"))
    await asyncio.sleep(0.8)
    metrics = await cdp.call('Page.getLayoutMetrics')
    height = int(metrics['cssContentSize']['height'])
    page = Image.new('RGB', (width, height), 'white')
    y = 0
    while y < height:
        h = min(TILE, height - y)
        shot = await cdp.call('Page.captureScreenshot', format='png', captureBeyondViewport=True,
                              clip={'x': 0, 'y': y, 'width': width, 'height': h, 'scale': 1})
        page.paste(Image.open(io.BytesIO(base64.b64decode(shot['data']))), (0, y))
        y += h
    page.save(path)
    print(f'{os.path.basename(path)}: {width}x{height}')


async def run(out, targets, widths):
    profile = tempfile.mkdtemp(prefix='yelg-cdp-')
    chrome = subprocess.Popen([CHROME, '--headless=new', '--disable-gpu', '--hide-scrollbars', f'--remote-debugging-port={PORT}',
                               f'--user-data-dir={profile}', '--no-first-run', '--no-default-browser-check', 'about:blank'],
                              stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    try:
        for _ in range(50):
            try:
                tabs = json.load(urllib.request.urlopen(f'http://127.0.0.1:{PORT}/json'))
                page = next(t for t in tabs if t.get('type') == 'page')
                break
            except Exception:
                time.sleep(0.2)
        async with websockets.connect(page['webSocketDebuggerUrl'], max_size=None) as ws:
            cdp = Cdp(ws)
            for name, url in targets:
                for w in widths:
                    await capture(cdp, url, w, os.path.join(out, f'{name}-{w}.png'))
    finally:
        chrome.terminate()
        chrome.wait(10)
        shutil.rmtree(profile, ignore_errors=True)


def main():
    args = sys.argv[1:]
    widths = [375, 768, 1024, 1440]
    if '--widths' in args:
        i = args.index('--widths')
        widths = [int(x) for x in args[i + 1].split(',')]
        del args[i:i + 2]
    out = args[0]
    os.makedirs(out, exist_ok=True)
    targets = [tuple(a.split('=', 1)) for a in args[1:]]
    asyncio.run(run(out, targets, widths))


if __name__ == '__main__':
    main()
