"""Private loopback preview. Public deployment serves ONLY recommendations.json over HTTPS."""
import argparse
from http.server import BaseHTTPRequestHandler, HTTPServer
from pathlib import Path


def handler_for(feed):
    class FeedHandler(BaseHTTPRequestHandler):
        def do_GET(self):
            if self.path != '/recommendations.json':
                self.send_error(404)
                return
            try:
                body = feed.read_bytes()
                if len(body) > 2000000:
                    raise OSError('Feed too large')
            except OSError:
                self.send_error(503)
                return
            self.send_response(200)
            self.send_header('Content-Type', 'application/json')
            self.send_header('Content-Length', str(len(body)))
            self.send_header('Cache-Control', 'no-store')
            self.end_headers()
            self.wfile.write(body)

        def log_message(self, *args):
            pass
    return FeedHandler


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--feed', type=Path, default=Path(__file__).parent / 'output/recommendations.json')
    parser.add_argument('--port', type=int, default=8769)
    args = parser.parse_args()
    HTTPServer(('127.0.0.1', args.port), handler_for(args.feed)).serve_forever()
