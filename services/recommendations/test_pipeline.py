import copy
import json
import sqlite3
from pathlib import Path
import tempfile
import threading
import unittest
from http.server import HTTPServer, BaseHTTPRequestHandler
from urllib.error import HTTPError, URLError
from urllib.request import urlopen, build_opener, Request
from pipeline import Riot, NoRedirect, collect, connect, core_path, ingest, export, observed_details
from serve import handler_for

NOW = 1800000000
CATALOG = {str(i): {'maps': {'11': True}, 'gold': {'purchasable': True, 'total': 3000}, 'tags': []} for i in (100, 200, 300)}


def fixture():
    p = {'puuid': 'qualified', 'participantId': 1, 'championId': 67, 'teamPosition': 'BOTTOM', 'win': True,
         'perks': {'styles': [{'description': 'primaryStyle', 'style': 8000, 'selections': [{'perk': i} for i in (8005, 9111, 9104, 8014)]},
                              {'description': 'subStyle', 'style': 8100, 'selections': [{'perk': i} for i in (8139, 8135)]}],
                   'statPerks': {'offense': 5005, 'flex': 5008, 'defense': 5001}}}
    match = {'metadata': {'matchId': 'NA1_1'}, 'info': {'queueId': 420, 'mapId': 11, 'gameMode': 'CLASSIC', 'gameDuration': 1800,
             'gameVersion': '16.17.123', 'gameStartTimestamp': (NOW - 3600)*1000, 'participants': [p]}}
    timeline = {'metadata': {'matchId': 'NA1_1'}, 'info': {'frames': [{'events': [{'participantId': 1, 'type': 'ITEM_PURCHASED', 'itemId': i} for i in (100, 200, 300)]}]}}
    return match, timeline


class PipelineTests(unittest.TestCase):
    def setUp(self):
        self.db = connect(':memory:')
        self.match, self.timeline = fixture()

    def tearDown(self):
        self.db.close()

    def add(self, match=None, timeline=None):
        ingest(self.db, match or self.match, timeline or self.timeline, {'qualified'}, 'NA1', '16.17', CATALOG, NOW)

    def test_dedup_and_cohort(self):
        other = copy.deepcopy(self.match['info']['participants'][0]);other['puuid'] = 'unqualified'
        self.match['info']['participants'].append(other)
        self.add();self.add()
        self.assertEqual(self.db.execute('SELECT count(*) FROM samples').fetchone()[0], 1)

    def test_connected_build_cohorts_do_not_mix_details(self):
        for group in range(2):
            for n in range(30):
                match, timeline = fixture()
                match_id = 'NA1_' + str(group * 100 + n)
                match['metadata']['matchId'] = timeline['metadata']['matchId'] = match_id
                p = match['info']['participants'][0]
                p['puuid'] = 'player' + str(n % 10)
                p['summoner1Id'], p['summoner2Id'] = (4, 7) if group == 0 else (4, 12)
                p['win'] = group == 0
                if group:
                    p['perks']['styles'][0]['selections'][0]['perk'] = 8008
                ingest(self.db, match, timeline, {p['puuid']}, 'NA1', '16.17', CATALOG, NOW)
        row = export(self.db, '16.17', NOW)['results'][0]
        self.assertEqual(len(row['builds']), 2)
        for build in row['builds']:
            self.assertEqual(build['games'], 30)
            self.assertEqual(build['value']['coreItems'], [100, 200, 300])
            expected = [4, 7] if build['value']['perks'][0] == 8005 else [4, 12]
            self.assertEqual(build['details']['summonerSpells']['value'], expected)
            self.assertIsNone(build['details']['startingItems'])
            self.assertIsNone(build['details']['skillOrder'])
            self.assertEqual(build['situationalItems'], [])
        self.db.execute("UPDATE samples SET details=NULL WHERE match='NA1_0'")
        first = next(b for b in export(self.db, '16.17', NOW)['results'][0]['builds'] if b['value']['perks'][0] == 8005)
        self.assertIsNone(first['details']['summonerSpells'])

    def test_migrate_and_enrich_without_duplicate(self):
        with tempfile.TemporaryDirectory() as root:
            path = Path(root) / 'old.sqlite'
            old = sqlite3.connect(path)
            old.execute('CREATE TABLE samples (match TEXT,player TEXT,region TEXT,champion INTEGER,role TEXT,patch TEXT,played INTEGER,observed INTEGER,win INTEGER,runes TEXT,items TEXT,PRIMARY KEY(match,player))')
            old.close()
            migrated = connect(path)
            ingest(migrated, self.match, self.timeline, {'qualified'}, 'NA1', '16.17', CATALOG, NOW)
            migrated.execute('UPDATE samples SET details=NULL')
            migrated.commit()
            ingest(migrated, self.match, self.timeline, {'qualified'}, 'NA1', '16.17', CATALOG, NOW)
            self.assertEqual(migrated.execute('SELECT count(*) FROM samples WHERE details IS NOT NULL').fetchone()[0], 1)
            migrated.close()

    def test_legacy_enrichment_keeps_core_when_timeline_unavailable(self):
        self.add()
        self.db.execute('UPDATE samples SET details=NULL');self.db.commit()
        ingest(self.db, self.match, None, {'qualified'}, 'NA1', '16.17', CATALOG, NOW)
        self.assertEqual(json.loads(self.db.execute('SELECT items FROM samples').fetchone()[0]), [100, 200, 300])
        self.db.execute('UPDATE samples SET details=NULL');self.db.commit()
        self.timeline['info']['frames'][0]['events'].append({'participantId': 1, 'type': 'ITEM_UNDO', 'beforeId': 0, 'afterId': 0})
        self.add()
        self.assertIsNone(self.db.execute('SELECT items FROM samples').fetchone()[0])

    def test_observed_details_undo_sales_and_partial_skills(self):
        p = self.match['info']['participants'][0]
        p.update(summoner1Id=7, summoner2Id=4, item0=300, item1=100, item2=0, item3=0, item4=0, item5=0)
        events = self.timeline['info']['frames'][0]['events']
        for n, e in enumerate(events): e['timestamp'] = n * 100000
        events.extend([
            {'participantId': 1, 'type': 'ITEM_SOLD', 'itemId': 100, 'timestamp': 300000},
            {'participantId': 1, 'type': 'ITEM_UNDO', 'afterId': 100, 'timestamp': 300001},
            {'participantId': 1, 'type': 'SKILL_LEVEL_UP', 'levelUpType': 'NORMAL', 'skillSlot': 1, 'timestamp': 0},
            {'participantId': 1, 'type': 'SKILL_LEVEL_UP', 'levelUpType': 'NORMAL', 'skillSlot': 3, 'timestamp': 60000}])
        details = observed_details(p, self.timeline, CATALOG)
        self.assertEqual(details['startingItems'], [100])
        self.assertEqual(details['purchaseOrder'], [100, 200, 300])
        self.assertEqual(details['skillOrder'], [1, 3])
        self.assertEqual(details['finalItems'], [100, 300])
        events.append({'participantId': 1, 'type': 'ITEM_UNDO', 'beforeId': 0, 'afterId': 0})
        details = observed_details(p, self.timeline, CATALOG)
        self.assertIsNone(details['purchaseOrder']);self.assertIsNone(details['startingItems'])
        self.assertEqual(details['skillOrder'], [1, 3])
        events[-2]['skillSlot'] = True
        self.assertIsNone(observed_details(p, self.timeline, CATALOG)['skillOrder'])

    def test_filters(self):
        for key, value in [('queueId', 440), ('mapId', 12), ('gameMode', 'ARAM'), ('gameDuration', 500),
                           ('gameVersion', '16.16.1'), ('gameStartTimestamp', (NOW-8*86400)*1000)]:
            with self.subTest(key=key):
                match = copy.deepcopy(self.match);match['info'][key] = value;self.add(match)
        self.assertEqual(self.db.execute('SELECT count(*) FROM samples').fetchone()[0], 0)

    def test_omitted_false_win_and_zero_undo_fields(self):
        del self.match['info']['participants'][0]['win']
        self.timeline['info']['frames'][0]['events'].append(
            {'participantId': 1, 'type': 'ITEM_UNDO', 'beforeId': 300})
        self.add()
        row = self.db.execute('SELECT win,items FROM samples').fetchone()
        self.assertEqual(row, (0, None))

    def test_minimum_distinct_players_and_privacy(self):
        for n in range(30):
            self.match['metadata']['matchId'] = 'NA1_' + str(n)
            self.timeline['metadata']['matchId'] = 'NA1_' + str(n);self.add()
        feed = export(self.db, '16.17', NOW)
        self.assertEqual(feed['results'][0]['runes'], [])
        for n in range(30):
            self.db.execute('UPDATE samples SET player=? WHERE match=?', ('player'+str(n%10), 'NA1_'+str(n)))
        feed = export(self.db, '16.17', NOW)
        row = feed['results'][0]
        self.assertEqual(row['items'][0]['value'], [100, 200, 300])
        self.assertEqual(row['runes'][0]['games'], 30)
        self.assertEqual(row['runes'][0]['wins'], 30)
        self.assertNotIn('player0', json.dumps(feed));self.assertNotIn('qualified', json.dumps(feed))
        self.assertEqual(export(self.db, '16.18', NOW)['results'], [])
        self.assertEqual(export(self.db, '16.17', NOW+8*86400)['results'], [])

    def test_undo_and_unknown_undo(self):
        events = self.timeline['info']['frames'][0]['events']
        events.insert(1, {'participantId': 1, 'type': 'ITEM_UNDO', 'beforeId': 100, 'afterId': 0})
        self.assertIsNone(core_path(self.timeline, 1, CATALOG))
        events.append({'participantId': 1, 'type': 'ITEM_PURCHASED', 'itemId': 100})
        self.assertEqual(core_path(self.timeline, 1, CATALOG), [200, 300, 100])
        events.append({'participantId': 1, 'type': 'ITEM_UNDO', 'beforeId': 0, 'afterId': 0})
        self.add()
        self.assertIsNone(self.db.execute('SELECT items FROM samples').fetchone()[0])
        self.assertIsNotNone(self.db.execute('SELECT runes FROM samples').fetchone()[0])

    def test_rate_limit_and_auth(self):
        now = [0];calls = []
        def sleep(seconds): now[0] += seconds
        def limited(req, **kwargs):
            calls.append(now[0]);raise HTTPError(req.full_url, 429, '', {'Retry-After': '9'}, None)
        api = Riot('secret', clock=lambda: now[0], sleep=sleep, open_url=limited)
        with self.assertRaises(RuntimeError): api.get('na1', '/test')
        self.assertEqual(calls, [0, 9, 18]);self.assertEqual(now[0], 27)
        def denied(req, **kwargs):
            calls.append(now[0]);raise HTTPError(req.full_url, 403, '', {}, None)
        calls.clear();api.open_url = denied
        with self.assertRaises(RuntimeError): api.get('na1', '/test')
        self.assertEqual(len(calls), 1)

    def test_server_does_not_expose_database(self):
        with tempfile.TemporaryDirectory() as root:
            feed = Path(root)/'recommendations.json';feed.write_text('{}')
            (Path(root)/'private.sqlite').write_text('private')
            server = HTTPServer(('127.0.0.1', 0), handler_for(feed))
            thread = threading.Thread(target=server.serve_forever, daemon=True);thread.start()
            try:
                base = 'http://127.0.0.1:' + str(server.server_port)
                self.assertEqual(urlopen(base+'/recommendations.json').read(), b'{}')
                for path in ('/', '/private.sqlite', '/../private.sqlite'):
                    with self.assertRaises(HTTPError) as err: urlopen(base+path)
                    self.assertEqual(err.exception.code, 404)
            finally:
                server.shutdown();server.server_close();thread.join()

    def test_redirect_does_not_forward_token(self):
        reached = []
        class Redirect(BaseHTTPRequestHandler):
            def log_message(self, *args): pass
            def do_GET(self):
                reached.append(self.path)
                self.send_response(302)
                self.send_header('Location', '/stolen')
                self.end_headers()
        server = HTTPServer(('127.0.0.1', 0), Redirect)
        thread = threading.Thread(target=server.serve_forever, daemon=True);thread.start()
        try:
            request = Request('http://127.0.0.1:%d/start' % server.server_port, headers={'X-Riot-Token': 'test-only'})
            with self.assertRaises(HTTPError): build_opener(NoRedirect()).open(request)
            self.assertEqual(reached, ['/start'])
        finally:
            server.shutdown();server.server_close();thread.join()

    def test_network_error_redacted(self):
        def broken(req, **kwargs): raise URLError('secret-player / secret-token')
        with self.assertRaises(RuntimeError) as error:
            Riot('secret-token', open_url=broken).get('na1', '/test')
        self.assertNotIn('secret', str(error.exception))

    def test_bad_shapes_and_mismatched_timeline(self):
        for value in (None, [], 'private response'):
            ingest(self.db, value, self.timeline, {'qualified'}, 'NA1', '16.17', CATALOG, NOW)
            self.assertIsNone(core_path(value, 1, CATALOG))
        for field, value in [('perks', []), ('championId', True), ('participantId', 11), ('teamPosition', [])]:
            match = copy.deepcopy(self.match);match['info']['participants'][0][field] = value
            self.add(match)
        self.assertEqual(self.db.execute('SELECT count(*) FROM samples').fetchone()[0], 0)
        self.timeline['metadata']['matchId'] = 'NA1_999'
        malformed = copy.deepcopy(self.match)
        malformed['info']['participants'][0]['perks']['styles'][1]['selections'][0]['perk'] = 'bad'
        self.add(malformed)
        self.assertEqual(self.db.execute('SELECT count(*) FROM samples').fetchone()[0], 0)
        self.add()
        self.assertIsNone(self.db.execute('SELECT items FROM samples').fetchone()[0])

    def test_all_regions_fit_seed_budget(self):
        class Fake:
            budget = 144
            hosts = []
            def get(self, host, path):
                self.budget -= 1;self.hosts.append(host)
                if '/DIAMOND/' in path: return [{'puuid': host + '-player'}]
                if 'leagues/by-queue' in path: return {'entries': []}
                return []
        api = Fake()
        collect(api, self.db, '16.17', CATALOG)
        self.assertTrue({'na1', 'euw1', 'kr', 'americas', 'europe', 'asia'}.issubset(api.hosts))
        self.assertEqual(api.budget, 120)
        api.budget = 143
        with self.assertRaises(ValueError): collect(api, self.db, '16.17', CATALOG)

    def test_collect_refetches_legacy_but_not_processed_unknown_details(self):
        self.add()
        self.db.execute('UPDATE samples SET details=NULL');self.db.commit()
        match, timeline = self.match, self.timeline
        match['info']['gameStartTimestamp'] = int(__import__('time').time() - 3600) * 1000
        class Fake:
            budget = 500
            timelines = 0
            def get(self, host, path):
                if '/DIAMOND/' in path: return [{'puuid': 'qualified'}] if host == 'na1' else []
                if 'leagues/by-queue' in path: return {'entries': []}
                if '/ids?' in path: return ['NA1_1']
                if path.endswith('/timeline'):
                    self.timelines += 1
                    return timeline
                return match
        api = Fake()
        collect(api, self.db, '16.17', CATALOG)
        self.assertEqual(api.timelines, 1)
        collect(api, self.db, '16.17', CATALOG)
        self.assertEqual(api.timelines, 1)

    def test_bad_league_schema_stops_without_response_details(self):
        class Fake:
            budget = 500
            def get(self, host, path): return {'sensitive-player': 'secret'}
        with self.assertRaises(RuntimeError) as error: collect(Fake(), self.db, '16.17', CATALOG)
        self.assertNotIn('sensitive', str(error.exception))


if __name__ == '__main__':
    unittest.main()
