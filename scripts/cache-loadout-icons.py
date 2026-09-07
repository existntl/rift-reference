"""Cache Riot Data Dragon loadout icons alongside the bundled patch data.

Run with Python 3 before building. No network requests are made by the editor.
"""
import concurrent.futures
import json
from pathlib import Path
import urllib.request

ROOT = Path(__file__).resolve().parents[1] / 'cache' / 'data'


def main():
    version = (ROOT / 'version.txt').read_text(encoding='utf-8-sig').strip()
    stamp = ROOT / 'loadout-icons' / 'version.txt'
    same_version = stamp.exists() and stamp.read_text(encoding='utf-8').strip() == version
    jobs = {}
    for tree in json.loads((ROOT / 'runes.json').read_text(encoding='utf-8-sig')):
        for rune in [tree] + [r for slot in tree['slots'] for r in slot['runes']]:
            jobs[f"runes/{rune['id']}.png"] = 'https://ddragon.leagueoflegends.com/cdn/img/' + rune['icon']
    items = json.loads((ROOT / 'item.json').read_text(encoding='utf-8-sig'))['data']
    for key, item in items.items():
        if item.get('maps', {}).get('11') and item.get('gold', {}).get('purchasable'):
            jobs[f'items/{key}.png'] = f"https://ddragon.leagueoflegends.com/cdn/{version}/img/item/{item['image']['full']}"
    for key, name in {5008: 'AdaptiveForce', 5005: 'AttackSpeed', 5007: 'CDRScaling',
                      5010: 'MovementSpeed', 5001: 'HealthScaling', 5011: 'HealthPlus',
                      5013: 'Tenacity'}.items():
        jobs[f'runes/{key}.png'] = f'https://ddragon.leagueoflegends.com/cdn/img/perk-images/StatMods/StatMods{name}Icon.png'

    def fetch(job):
        relative, url = job
        destination = ROOT / 'loadout-icons' / relative
        destination.parent.mkdir(parents=True, exist_ok=True)
        if same_version and destination.exists() and destination.read_bytes().startswith(b'\x89PNG\r\n\x1a\n'):
            return
        with urllib.request.urlopen(url, timeout=30) as response:
            content = response.read(2_000_000)
        if not content.startswith(b'\x89PNG\r\n\x1a\n'):
            raise ValueError(f'Invalid icon: {relative}')
        temporary = destination.with_suffix('.tmp')
        temporary.write_bytes(content)
        temporary.replace(destination)

    with concurrent.futures.ThreadPoolExecutor(max_workers=8) as executor:
        list(executor.map(fetch, jobs.items()))
    stamp.write_text(version, encoding='utf-8')
    print(f'Cached {len(jobs)} Data Dragon icons for {version}.')


if __name__ == '__main__':
    main()
