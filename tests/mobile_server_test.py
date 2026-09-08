"""Integration checks against the actual bundled Python HTTP helper."""
import http.client
import json
import pathlib
import subprocess
import sys
import time
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
APP = ROOT / 'build' / 'app'

class CompanionTests(unittest.TestCase):
    def setUp(self):
        self.token = 'ab' * 32
        self.child = subprocess.Popen([str(APP / 'runtime/python.exe'), '-I', '-X', 'utf8', '-u', str(APP / 'mobile_server.py')], stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, encoding='utf-8')
        self.child.stdin.write(json.dumps({'address': '127.0.0.1', 'token': self.token})+'\n')
        self.child.stdin.flush()
        ready = json.loads(self.child.stdout.readline())
        self.authority = ready['url'].split('/')[2]
        self.port = int(self.authority.split(':')[1])
        self.assertGreater(len(ready['qr']), 20)

    def tearDown(self):
        self.child.stdin.close()
        self.child.wait(timeout=6)
        self.child.stdout.close()
        self.child.stderr.close()

    def get(self, path, headers=None):
        connection = http.client.HTTPConnection('127.0.0.1', self.port, timeout=5)
        connection.request('GET', path, headers=headers or {})
        response = connection.getresponse()
        code, data, response_headers = response.status, response.read(), dict(response.getheaders())
        connection.close()
        return code, data, response_headers

    def test_auth_and_routes(self):
        code, html, headers = self.get('/')
        self.assertEqual(code, 200)
        self.assertNotIn(self.token.encode(), html)
        self.assertEqual(headers['Cache-Control'], 'no-store')
        self.assertNotIn('Access-Control-Allow-Origin', headers)
        self.assertEqual(self.get('/state')[0], 401)
        self.assertEqual(self.get('/state', {'Authorization':'Bearer incorrect'})[0], 401)
        self.assertEqual(self.get('/state?token='+self.token)[0], 404)
        self.assertEqual(self.get('/../preferences.json')[0], 404)
        self.assertEqual(self.get('/', {'Host':'attacker.example'})[0], 403)
        self.assertEqual(self.get('/state', {'Authorization':'Bearer '+self.token, 'Origin':'http://attacker.example'})[0], 403)
        self.assertEqual(self.get('/state', {'Authorization':'Bearer '+self.token})[0], 503)

    def test_updates_and_clear(self):
        def publish(value):
            self.child.stdin.write(json.dumps(value)+'\n');self.child.stdin.flush();time.sleep(.15)
            code, data, headers = self.get('/state', {'Authorization':'Bearer '+self.token})
            self.assertEqual(code, 200)
            return json.loads(data)
        first = publish({'phase':'In game','players':['Vayne · 4s — estimate']})
        self.assertEqual(first['snapshot']['players'], ['Vayne · 4s — estimate'])
        self.assertLess(first['age'], 2)
        second = publish({'phase':'Waiting','players':[]})
        self.assertEqual(second['snapshot']['players'], [])

    def test_pipe_shutdown(self):
        self.child.stdin.close()
        self.child.wait(timeout=6)
        with self.assertRaises(OSError):
            self.get('/')

if __name__ == '__main__':
    unittest.main()
