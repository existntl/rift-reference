"""Exercise the actual pipe loop with a fake HTTP opener; no League connection."""
import io
import json
import pathlib
import runpy
import unittest
from unittest.mock import patch

class TransportTests(unittest.TestCase):
    def run_messages(self, messages, response=b''):
        calls=[]
        class Reply(io.BytesIO):
            pass
        class Opener:
            def open(self, request, timeout):
                calls.append(request)
                return Reply(response)
        output=io.StringIO()
        source=''.join(json.dumps(message)+'\n' for message in messages)
        with patch('urllib.request.build_opener',return_value=Opener()), patch('sys.stdin',io.StringIO(source)), patch('sys.stdout',output):
            runpy.run_path(str(pathlib.Path(__file__).parents[1]/'helpers/transport.py'))
        return calls,[json.loads(line) for line in output.getvalue().splitlines()]
    def test_empty_success_and_unicode(self):
        calls,replies=self.run_messages([dict(url='https://127.0.0.1:1234/lol-perks/v1/pages',auth='fake',method='POST',body={'name':'RR · Vayne'})])
        self.assertTrue(replies[0]['ok'])
        self.assertIsNone(replies[0]['data'])
        self.assertEqual(json.loads(calls[0].data)['name'],'RR · Vayne')
    def test_restricted_writes(self):
        messages=[dict(url=url,auth='fake',method=method,body={}) for url,method in [
            ('https://example.com/lol-perks/v1/pages','POST'),
            ('https://127.0.0.1/lol-perks/v1/pages/3','DELETE'),
            ('https://127.0.0.1/lol-item-sets/v1/item-sets/123/sets','PUT'),
            ('https://127.0.0.1/lol-champ-select/v1/session','POST'),
            ('https://127.0.0.1/lol-perks/v1/pages?x=1','POST')]]
        calls,replies=self.run_messages(messages)
        self.assertEqual(calls,[])
        self.assertTrue(all(not r['ok'] for r in replies))
    def test_itemset_append_and_live_get(self):
        calls,replies=self.run_messages([
            dict(url='https://127.0.0.1:1234/lol-item-sets/v1/item-sets/123/sets',auth='fake',method='POST',body={'title':'RR'}),
            dict(url='https://127.0.0.1:2999/liveclientdata/gamestats')],b'{"gameTime":12}')
        self.assertEqual([r.method for r in calls],['POST','GET'])
        self.assertTrue(all(r['ok'] for r in replies))
        self.assertEqual(replies[1]['data']['gameTime'],12)

    def test_history_is_bounded_authenticated_read_only(self):
        url='https://127.0.0.1:1234/lol-match-history/v1/products/lol/fake-puuid/matches?begIndex=0&endIndex=19'
        calls,replies=self.run_messages([dict(url=url,auth='fake'),dict(url=url),dict(url=url.replace('19','999'),auth='fake'),dict(url=url,auth='fake',method='POST',body={}),dict(url='https://127.0.0.1:1234/lol-ranked/v1/current-ranked-stats',auth='fake')],b'{}')
        self.assertEqual(len(calls),2)
        self.assertEqual([r['ok'] for r in replies],[True,False,False,False,True])

    def test_postgame_is_read_only(self):
        url='https://127.0.0.1:1234/lol-match-history/v1/products/lol/fake-puuid/matches?begIndex=0&endIndex=99'
        calls,replies=self.run_messages([dict(url=url,auth='fake'),dict(url=url),dict(url=url.replace('99','999'),auth='fake')],b'{}')
        self.assertEqual(len(calls),1)
        self.assertEqual([r['ok'] for r in replies],[True,False,False])
        calls,replies=self.run_messages([
            dict(url='https://127.0.0.1:1234/lol-end-of-game/v1/eog-stats-block'),
            dict(url='https://127.0.0.1:1234/lol-end-of-game/v1/eog-stats-block',method='POST',auth='fake',body={}),
            dict(url='https://127.0.0.1:1234/lol-end-of-game/v1/other')],b'{"gameId":789}')
        self.assertEqual(len(calls),1)
        self.assertEqual([r['ok'] for r in replies],[True,False,False])

if __name__=='__main__': unittest.main()
