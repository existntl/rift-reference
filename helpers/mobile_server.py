"""Opt-in local dashboard. Only sanitized snapshots arrive over the private stdin pipe."""
import concurrent.futures
import hmac
import http.server
import importlib.util
import ipaddress
import json
import pathlib
import re
import socket
import sys
import threading
import time

ROOT = pathlib.Path(__file__).resolve().parent
NETWORKS = tuple(ipaddress.ip_network(n) for n in ('10.0.0.0/8', '172.16.0.0/12', '192.168.0.0/16', '127.0.0.0/8'))

def private_address(value):
    try:
        address = ipaddress.ip_address(value)
        return any(address in network for network in NETWORKS)
    except ValueError:
        return False

def serve(config):
    address = config['address']
    token = config['token']
    if not private_address(address) or not re.fullmatch('[a-f0-9]{64}', token):
        raise ValueError('Invalid local configuration')
    state = {'value': None, 'updated': 0}
    lock = threading.Lock()
    html = (ROOT / 'mobile.html').read_bytes()

    class Handler(http.server.BaseHTTPRequestHandler):
        def setup(self):
            self.request.settimeout(3)
            super().setup()

        def log_message(self, *args):
            pass  # Never log pairing credentials or request URLs.

        def reply(self, code, body, kind='text/plain; charset=utf-8'):
            if isinstance(body, str):
                body = body.encode('utf-8')
            self.send_response(code)
            self.send_header('Content-Type', kind)
            self.send_header('Content-Length', str(len(body)))
            self.send_header('Cache-Control', 'no-store')
            self.send_header('Referrer-Policy', 'no-referrer')
            self.send_header('X-Content-Type-Options', 'nosniff')
            self.send_header('X-Frame-Options', 'DENY')
            self.send_header('Content-Security-Policy', "default-src 'none'; img-src data:; script-src 'unsafe-inline'; style-src 'unsafe-inline'; connect-src 'self'; base-uri 'none'; frame-ancestors 'none'; form-action 'none'")
            self.send_header('Connection', 'close')
            self.end_headers()
            self.wfile.write(body)

        def do_GET(self):
            if not private_address(self.client_address[0]) or self.headers.get('Host') != authority:
                return self.reply(403, 'Local network only')
            origin = self.headers.get('Origin')
            if origin and origin != 'http://' + authority:
                return self.reply(403, 'Origin rejected')
            if self.path == '/':
                return self.reply(200, html, 'text/html; charset=utf-8')
            if self.path != '/state':
                return self.reply(404, 'Not found')
            if not hmac.compare_digest(self.headers.get('Authorization', ''), 'Bearer ' + token):
                return self.reply(401, 'Pair from the desktop app')
            with lock:
                value = state['value']
                age = time.monotonic() - state['updated']
            if value is None or age > 40:
                return self.reply(503, 'Desktop data unavailable')
            return self.reply(200, json.dumps({'age': age, 'snapshot': value}), 'application/json; charset=utf-8')

    class Server(http.server.HTTPServer):
        # Bound workers and queued connections; a slow LAN client cannot create unbounded threads.
        def __init__(self, *args):
            self.slots = threading.BoundedSemaphore(8)
            self.pool = concurrent.futures.ThreadPoolExecutor(max_workers=8)
            super().__init__(*args)

        def process_request(self, request, client_address):
            if not self.slots.acquire(False):
                self.shutdown_request(request)
                return
            self.pool.submit(self.finish, request, client_address)

        def finish(self, request, client_address):
            try:
                self.finish_request(request, client_address)
            except (OSError, ValueError):
                pass
            finally:
                self.shutdown_request(request)
                self.slots.release()

    server = Server((address, 0), Handler)
    authority = address + ':' + str(server.server_port)
    url = 'http://' + authority + '/#' + token
    spec = importlib.util.spec_from_file_location('local_qr', ROOT / 'qrcodegen.py')
    qr_module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(qr_module)
    qr = qr_module.QrCode.encode_text(url, qr_module.QrCode.Ecc.MEDIUM)
    matrix = [''.join('1' if qr.get_module(x, y) else '0' for x in range(qr.get_size())) for y in range(qr.get_size())]
    print(json.dumps({'url': url, 'qr': matrix}), flush=True)
    worker = threading.Thread(target=server.serve_forever, daemon=True)
    worker.start()
    try:
        for line in sys.stdin:
            if len(line) > 262144:
                break
            value = json.loads(line)
            with lock:
                state.update(value=value, updated=time.monotonic())
    finally:
        server.shutdown()
        server.server_close()
        server.pool.shutdown(wait=False, cancel_futures=True)

if __name__ == '__main__':
    try:
        serve(json.loads(sys.stdin.readline()))
    except Exception:
        print(json.dumps({'error': 'Could not start local sharing. Check your network and try again.'}), flush=True)
        sys.exit(1)
