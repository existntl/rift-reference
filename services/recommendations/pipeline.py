"""Bounded, private Riot collector and anonymous recommendation feed publisher."""
import argparse
from collections import defaultdict
import hashlib
import json
import math
import os
from pathlib import Path
import random
import re
import sqlite3
import time
from urllib.request import Request, build_opener, HTTPRedirectHandler
from urllib.error import HTTPError, URLError
from urllib.parse import quote, urlencode

REGIONS = {'NA1': 'americas', 'EUW1': 'europe', 'KR': 'asia'}
ROLES = {'TOP', 'JUNGLE', 'MIDDLE', 'BOTTOM', 'UTILITY'}


class NoRedirect(HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        # Never forward the Riot credential to a redirect destination.
        return None


def positive_int(value):
    return type(value) is int and value > 0


def match_name(value, region):
    return isinstance(value, str) and re.fullmatch(re.escape(region) + r'_\d+', value) is not None


def patch_of(version):
    parts = str(version).split('.')
    return '.'.join(parts[:2]) if len(parts) >= 2 else ''


class Riot:
    def __init__(self, key, budget=500, clock=time.time, sleep=time.sleep, open_url=None):
        if not key:
            raise ValueError('Set RIOT_API_KEY in the collector environment. Never put it in the app.')
        self.key, self.budget = key, budget
        self.clock, self.sleep = clock, sleep
        self.open_url = open_url or build_opener(NoRedirect()).open
        self.next_call = 0

    def get(self, host, path):
        if host not in set(REGIONS.values()) | {r.lower() for r in REGIONS}:
            raise ValueError('Unsupported Riot routing host')
        if not isinstance(path, str) or not path.startswith('/') or path.startswith('//') or any(c in path for c in '\r\n'):
            raise ValueError('Invalid Riot endpoint path')
        for attempt in range(3):
            if self.budget <= 0:
                raise RuntimeError('Collection request budget exhausted; saved samples are retained.')
            self.sleep(max(0, self.next_call - self.clock()))
            self.budget -= 1
            # Conservative global pacing, even across independent regional buckets.
            self.next_call = self.clock() + 1.3
            req = Request('https://' + host + '.api.riotgames.com' + path,
                          headers={'X-Riot-Token': self.key})
            try:
                with self.open_url(req, timeout=30) as response:
                    self.throttle(response.headers)
                    try:
                        body = response.read(16 * 1024 * 1024 + 1)
                        if len(body) > 16 * 1024 * 1024:
                            raise RuntimeError('Riot response exceeds the collection size limit.')
                        return json.loads(body)
                    except (ValueError, UnicodeError):
                        raise RuntimeError('Riot returned an invalid JSON response; collection stopped.') from None
            except HTTPError as error:
                if error.code == 429:
                    try:
                        delay = float(error.headers.get('Retry-After', '120'))
                        if not math.isfinite(delay) or delay < 0:
                            raise ValueError()
                        delay = max(1, delay)
                    except (TypeError, ValueError):
                        delay = 120
                    self.next_call = max(self.next_call, self.clock() + delay)
                    # Do not resume other calls after the final retry without waiting.
                    if attempt == 2:
                        self.sleep(delay)
                        raise RuntimeError('Riot rate limit; collection stopped.') from None
                elif error.code >= 500:
                    self.next_call = self.clock() + 5 * (attempt + 1)
                elif error.code == 404:
                    return None
                else:
                    raise RuntimeError('Riot HTTP %s; collection stopped. Check key and API access.' % error.code) from None
            except (URLError, TimeoutError, OSError):
                raise RuntimeError('Riot network request failed; collection stopped. Saved samples are retained.') from None
        raise RuntimeError('Riot temporarily unavailable; collection stopped.')

    def throttle(self, headers):
        for prefix in ('X-App-Rate-Limit', 'X-Method-Rate-Limit'):
            try:
                limits = {int(window): int(count) for count, window in
                          (v.split(':') for v in headers.get(prefix, '').split(',') if v)}
                for value in headers.get(prefix + '-Count', '').split(','):
                    if not value:
                        continue
                    count, window = map(int, value.split(':'))
                    if count >= limits.get(window, 10**9) - 1:
                        self.next_call = max(self.next_call, self.clock() + window + 1)
            except (TypeError, ValueError):
                self.next_call = max(self.next_call, self.clock() + 120)


def connect(path):
    db = sqlite3.connect(path)
    db.execute('''CREATE TABLE IF NOT EXISTS samples (
      match TEXT, player TEXT, region TEXT, champion INTEGER, role TEXT,
      patch TEXT, played INTEGER, observed INTEGER, win INTEGER,
      runes TEXT, items TEXT, PRIMARY KEY(match, player))''')
    if 'details' not in {row[1] for row in db.execute('PRAGMA table_info(samples)')}:
        db.execute('ALTER TABLE samples ADD COLUMN details TEXT')
        db.commit()
    return db


def rune_page(participant):
    perks = participant.get('perks', {})
    if not isinstance(perks, dict):
        return None
    styles = perks.get('styles', [])
    if not isinstance(styles, list) or len(styles) != 2 or not all(isinstance(s, dict) and isinstance(s.get('selections'), list) and all(isinstance(r, dict) for r in s['selections']) for s in styles):
        return None
    first = next((s for s in styles if s.get('description') == 'primaryStyle'), {})
    second = next((s for s in styles if s.get('description') == 'subStyle'), {})
    primary = [r.get('perk', 0) for r in first.get('selections', [])]
    secondary = [r.get('perk', 0) for r in second.get('selections', [])]
    if not all(positive_int(i) for i in primary + secondary):
        return None
    shards = perks.get('statPerks', {})
    if not isinstance(shards, dict):
        return None
    ids = primary + sorted(secondary) + [shards.get(k, 0) for k in ('offense', 'flex', 'defense')]
    if len(primary) != 4 or len(secondary) != 2 or not all(positive_int(i) for i in ids):
        return None
    if not positive_int(first.get('style')) or not positive_int(second.get('style')) or first['style'] == second['style']:
        return None
    return {'primary': first['style'], 'secondary': second['style'], 'perks': ids}


def purchases_for(timeline, participant_id):
    if not isinstance(timeline, dict) or not isinstance(timeline.get('info'), dict) or not positive_int(participant_id):
        return None
    frames = timeline['info'].get('frames')
    if not isinstance(frames, list) or not frames:
        return None
    purchases = []
    sales = []
    for frame in frames:
        if not isinstance(frame, dict) or not isinstance(frame.get('events'), list):
            return None
        for event in frame.get('events', []):
            if not isinstance(event, dict):
                return None
            if event.get('participantId') != participant_id:
                continue
            kind = event.get('type')
            if kind == 'ITEM_PURCHASED':
                if not positive_int(event.get('itemId')):
                    return None
                purchases.append((event['itemId'], event.get('timestamp')))
            elif kind == 'ITEM_SOLD':
                if not positive_int(event.get('itemId')):
                    return None
                sales.append(event['itemId'])
            elif kind == 'ITEM_UNDO':
                before, after = event.get('beforeId', 0), event.get('afterId', 0)
                if type(before) is not int or type(after) is not int or before < 0 or after < 0 or (before and after):
                    return None
                if not before and not after:
                    return None  # Known Riot timeline bug: cannot reconstruct safely.
                if before:
                    try:
                        reverse_index = [p[0] for p in purchases][::-1].index(before)
                        purchases.pop(len(purchases) - 1 - reverse_index)
                    except ValueError:
                        return None
                else:
                    if after not in sales:
                        return None
                    sales.pop(len(sales) - 1 - sales[::-1].index(after))
    return purchases


def core_path(timeline, participant_id, catalog):
    purchases = purchases_for(timeline, participant_id)
    if purchases is None:
        return None
    core = []
    for item_id, timestamp in purchases:
        item = catalog.get(str(item_id), {})
        tags = set(item.get('tags', []))
        gold = item.get('gold', {})
        if (item.get('maps', {}).get('11') and gold.get('purchasable')
                and gold.get('total', 0) >= 2000 and not item.get('into')
                and not tags.intersection({'Boots', 'Consumable', 'Trinket'})
                and item_id not in core):
            core.append(item_id)
    return core[:3] if len(core) >= 3 else None


def observed_details(participant, timeline, catalog):
    details = {}
    spells = [participant.get('summoner1Id'), participant.get('summoner2Id')]
    details['summonerSpells'] = sorted(spells) if all(positive_int(i) for i in spells) and len(set(spells)) == 2 else None
    inventory = [participant.get('item' + str(i)) for i in range(6)]
    details['finalItems'] = (sorted(i for i in inventory if i and catalog[str(i)].get('maps', {}).get('11') and 'Trinket' not in catalog[str(i)].get('tags', []))
                             if all(type(i) is int and i >= 0 and (i == 0 or str(i) in catalog) for i in inventory) else None)
    purchases = purchases_for(timeline, participant.get('participantId'))
    details['purchaseOrder'] = details['startingItems'] = None
    if purchases is not None and all(str(i) in catalog for i, stamp in purchases):
        eligible = [(i, stamp) for i, stamp in purchases if catalog[str(i)].get('maps', {}).get('11') and catalog[str(i)].get('gold', {}).get('purchasable')]
        details['purchaseOrder'] = [i for i, stamp in eligible][:20] or None
        if all(type(stamp) is int and stamp >= 0 for i, stamp in purchases):
            details['startingItems'] = sorted(i for i, stamp in eligible if stamp < 90000) or None
            if details['startingItems'] and len(details['startingItems']) > 20:
                details['startingItems'] = None
    details['skillOrder'] = None
    if isinstance(timeline, dict) and isinstance(timeline.get('info'), dict):
        frames = timeline['info'].get('frames')
        if isinstance(frames, list) and frames and all(isinstance(f, dict) and isinstance(f.get('events'), list) for f in frames):
            events = [e for f in frames for e in f['events']]
            if all(isinstance(e, dict) for e in events):
                skills = [e for e in events if e.get('participantId') == participant.get('participantId') and e.get('type') == 'SKILL_LEVEL_UP']
                if skills and len(skills) <= 18 and all(e.get('levelUpType') == 'NORMAL' and type(e.get('skillSlot')) is int and 1 <= e['skillSlot'] <= 4 and type(e.get('timestamp')) is int and e['timestamp'] >= 0 for e in skills):
                    details['skillOrder'] = [e['skillSlot'] for e in sorted(skills, key=lambda e: e['timestamp'])]
    return details


def ingest(db, match, timeline, qualified, region, patch, catalog, now=None):
    now = int(time.time()) if now is None else now
    if not isinstance(match, dict) or not isinstance(match.get('info'), dict) or not isinstance(match.get('metadata'), dict):
        return
    info = match.get('info', {})
    if not positive_int(info.get('gameStartTimestamp')) or not positive_int(info.get('gameDuration')) or not isinstance(info.get('participants'), list):
        return
    played = info['gameStartTimestamp'] // 1000
    match_id = match.get('metadata', {}).get('matchId', '')
    if (not match_name(match_id, region) or info.get('queueId') != 420
            or info.get('mapId') != 11 or info.get('gameMode') != 'CLASSIC'
            or info.get('gameDuration', 0) < 600 or patch_of(info.get('gameVersion')) != patch
            or not now - 7 * 86400 <= played <= now):
        return
    if not isinstance(timeline, dict) or not isinstance(timeline.get('metadata'), dict) or timeline['metadata'].get('matchId') != match_id:
        timeline = None
    for p in info.get('participants', []):
        if (not isinstance(p, dict) or not isinstance(p.get('puuid'), str) or p.get('puuid') not in qualified
                or not isinstance(p.get('teamPosition'), str) or p.get('teamPosition') not in ROLES
                or not positive_int(p.get('championId')) or not positive_int(p.get('participantId')) or p['participantId'] > 10
                or p.get('gameEndedInEarlySurrender') or not isinstance(p.get('win', False), bool)):
            continue
        runes = rune_page(p)
        if not runes:
            continue
        # Only a private irreversible identifier is retained, never exported.
        player = hashlib.sha256(p['puuid'].encode()).hexdigest()
        items = core_path(timeline, p.get('participantId'), catalog)
        with db:
            db.execute('''INSERT INTO samples (match,player,region,champion,role,patch,played,observed,win,runes,items,details)
                       VALUES (?,?,?,?,?,?,?,?,?,?,?,?) ON CONFLICT(match,player) DO UPDATE SET
                       details=excluded.details,items=CASE WHEN ? THEN excluded.items ELSE samples.items END
                       WHERE samples.details IS NULL''',
                       (match_id, player, region, p['championId'], p['teamPosition'], patch,
                        played, now, int(p.get('win', False)), json.dumps(runes, sort_keys=True),
                        json.dumps(items) if items else None,
                        json.dumps(observed_details(p, timeline, catalog), sort_keys=True), timeline is not None))


def export(db, patch, now=None, minimum=30, players_minimum=10):
    now = int(time.time()) if now is None else now
    groups = defaultdict(lambda: defaultdict(lambda: [0, 0, set()]))
    builds = defaultdict(lambda: defaultdict(lambda: {'stat': [0, 0, set()], 'details': defaultdict(lambda: defaultdict(lambda: [0, 0, set()]))}))
    totals = defaultdict(int)
    rows = db.execute('SELECT region,champion,role,win,player,runes,items,details FROM samples WHERE patch=? AND played>=? AND played<=?',
                      (patch, now - 7 * 86400, now))
    for region, champion, role, win, player, runes, items, details in rows:
        if region not in REGIONS:
            continue
        key = (champion, role)
        totals[key] += 1
        for kind, value in (('runes', runes), ('items', items)):
            if value:
                stat = groups[key][(kind, value)]
                stat[0] += 1
                stat[1] += win
                stat[2].add(player)
        if runes and items:
            combined = dict(json.loads(runes), coreItems=json.loads(items))
            group = builds[key][json.dumps(combined, sort_keys=True)]
            stat = group['stat'];stat[0] += 1;stat[1] += win;stat[2].add(player)
            for name, value in (json.loads(details) if details else {}).items():
                if name in ('summonerSpells', 'startingItems', 'purchaseOrder', 'finalItems', 'skillOrder') and value:
                    stat = group['details'][name][json.dumps(value)]
                    stat[0] += 1;stat[1] += win;stat[2].add(player)
    results = []
    for (champion, role), choices in sorted(groups.items()):
        row = {'championId': champion, 'role': role, 'sampleSize': totals[(champion, role)], 'runes': [], 'items': [], 'builds': []}
        for (kind, value), (games, wins, players) in sorted(choices.items(), key=lambda p: -p[1][0]):
            if games < max(30, minimum) or len(players) < max(10, players_minimum):
                continue
            if len(row[kind]) < 5:
                row[kind].append({'value': json.loads(value), 'games': games, 'wins': wins, 'players': len(players)})
        for value, group in sorted(builds[(champion, role)].items(), key=lambda p: (-p[1]['stat'][0], p[0])):
            games, wins, players = group['stat']
            if games < max(30, minimum) or len(players) < max(10, players_minimum):
                continue
            build = {'value': json.loads(value), 'games': games, 'wins': wins, 'players': len(players), 'details': {}, 'situationalItems': []}
            for name in ('summonerSpells', 'startingItems', 'purchaseOrder', 'finalItems', 'skillOrder'):
                build['details'][name] = None
                for detail, (count, victories, people) in sorted(group['details'][name].items(), key=lambda p: (-p[1][0], p[0])):
                    if count >= max(30, minimum) and len(people) >= max(10, players_minimum):
                        build['details'][name] = {'value': json.loads(detail), 'games': count, 'wins': victories, 'players': len(people)}
                        break
            row['builds'].append(build)
            if len(row['builds']) == 20:
                break
        results.append(row)
    return {'format': 'rift-diamond-2', 'patch': patch, 'generatedAt': now,
            'regions': list(REGIONS), 'queue': 420, 'rankBasis': 'diamond-plus-at-collection',
            'windowDays': 7, 'minimumGames': max(30, minimum), 'minimumPlayers': max(10, players_minimum),
            'results': results}


def collect(api, db, patch, catalog, seeds=12, pages=1):
    start = int(time.time()) - 7 * 86400
    # Reserve calls for all three regions instead of spending the run entirely in NA.
    available_seeds = (api.budget // len(REGIONS) - (4 * pages + 3)) // 41
    if available_seeds < 1:
        raise ValueError('Request budget is too small to sample all three regions.')
    seeds = min(seeds, available_seeds)
    for region, route in REGIONS.items():
        entries = []
        for division in ('I', 'II', 'III', 'IV'):
            for page in range(1, pages + 1):
                page_entries = api.get(region.lower(), '/lol/league/v4/entries/RANKED_SOLO_5x5/DIAMOND/' + division + '?page=' + str(page))
                if not isinstance(page_entries, list) or not all(isinstance(e, dict) for e in page_entries):
                    raise RuntimeError('Unexpected Riot ranked entries schema; collection stopped.')
                entries += page_entries
        for tier in ('master', 'grandmaster', 'challenger'):
            league = api.get(region.lower(), '/lol/league/v4/' + tier + 'leagues/by-queue/RANKED_SOLO_5x5')
            if not isinstance(league, dict) or not isinstance(league.get('entries'), list) or not all(isinstance(e, dict) for e in league['entries']):
                raise RuntimeError('Unexpected Riot apex league schema; collection stopped.')
            entries += league['entries']
        qualified = {e['puuid'] for e in entries if isinstance(e.get('puuid'), str) and e['puuid']}
        if entries and not qualified:
            raise RuntimeError('Riot league response lacks PUUIDs; update collector schema before proceeding.')
        selected = random.SystemRandom().sample(sorted(qualified), min(seeds, len(qualified)))
        seen = set()
        for puuid in selected:
            path = '/lol/match/v5/matches/by-puuid/' + quote(puuid, safe='') + '/ids?'
            ids = api.get(route, path + urlencode({'queue': 420, 'startTime': start, 'count': 20}))
            if not isinstance(ids, list) or len(ids) > 20 or not all(match_name(mid, region) for mid in ids):
                raise RuntimeError('Unexpected Riot match list schema; collection stopped.')
            for match_id in ids:
                if match_id in seen:
                    continue
                seen.add(match_id)
                base = '/lol/match/v5/matches/' + quote(match_id, safe='')
                match = api.get(route, base)
                if match is None:
                    continue
                if (not isinstance(match, dict) or not isinstance(match.get('info'), dict)
                        or not isinstance(match.get('metadata'), dict) or match['metadata'].get('matchId') != match_id
                        or not isinstance(match['info'].get('participants'), list)):
                    raise RuntimeError('Unexpected Riot match schema; collection stopped.')
                if patch_of(match['info'].get('gameVersion')) != patch:
                    continue
                candidates = [p for p in match['info']['participants'] if isinstance(p, dict) and isinstance(p.get('puuid'), str) and p['puuid'] in qualified]
                if not candidates or all(db.execute('SELECT 1 FROM samples WHERE match=? AND player=? AND details IS NOT NULL',
                        (match_id, hashlib.sha256(p['puuid'].encode()).hexdigest())).fetchone() for p in candidates):
                    continue
                timeline = api.get(route, base + '/timeline')
                ingest(db, match, timeline, qualified, region, patch, catalog)
    with db:
        db.execute('DELETE FROM samples WHERE played<?', (start,))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--data', type=Path, required=True, help='Data Dragon cache folder')
    parser.add_argument('--output', type=Path, default=Path(__file__).parent / 'output')
    parser.add_argument('--seeds', type=int, default=12)
    parser.add_argument('--pages', type=int, default=1)
    parser.add_argument('--budget', type=int, default=500)
    parser.add_argument('--export-only', action='store_true')
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    patch = patch_of((args.data / 'version.txt').read_text(encoding='utf-8-sig').strip())
    catalog = json.loads((args.data / 'item.json').read_text(encoding='utf-8-sig'))['data']
    with connect(args.output / 'private.sqlite') as db:
        if not args.export_only:
            collect(Riot(os.environ.get('RIOT_API_KEY'), max(1, args.budget)), db, patch, catalog,
                    max(1, min(200, args.seeds)), max(1, min(100, args.pages)))
        feed = export(db, patch)
    target = args.output / 'recommendations.json'
    temporary = target.with_suffix('.tmp')
    temporary.write_text(json.dumps(feed, separators=(',', ':')), encoding='utf-8')
    temporary.replace(target)
    print('Wrote anonymous feed with %d champion/role groups.' % len(feed['results']))


if __name__ == '__main__':
    try:
        main()
    except (RuntimeError, ValueError) as error:
        raise SystemExit(str(error)) from None
