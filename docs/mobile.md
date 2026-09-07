# Phone and tablet companion

Preferences > Phone / tablet > choose the Wi-Fi or Ethernet address > Start sharing.
Scan the QR code on a phone/tablet on the same home network, or copy the link to its
browser. Leave Rift Reference running; minimizing is supported. Stop sharing revokes
all links; starting again generates a fresh 256-bit code. Sharing never starts automatically.

If prompted by Windows Firewall, allow the bundled Python helper on Private networks.
Do not enable router port forwarding. Guest networks/client isolation may prevent access.
Choose a different address if a VPN or virtual adapter was selected. A network change
requires Stop / Start and a new QR scan. QR generation is local; no external QR service.

This is local HTTP, not encrypted HTTPS. Use a trusted home network. No League credentials,
account names, review notes, raw client endpoints or write controls are exposed. The QR
contains a per-session secret in a URL fragment, removed from the address bar on load.
Only the state endpoint needs authentication; the empty UI shell has no private data.
State requests require the authorization header, exact host and same origin. Only one
selected private IPv4 interface is bound, and non-private remote addresses are rejected.
Eight bounded HTTP workers use socket timeouts. No firewall rules are changed by the app.

Phones poll every two seconds; the source follows the desktop refresh interval (3–30s,
default 5s), not frame-by-frame telemetry. A lost connection hides champion information.
Demo is clearly labeled. Audio stays on the PC. Wake Lock is offered only where supported
in a secure browser context; ordinary LAN HTTP usually requires changing device auto-lock.
No enemy casts, remaining timers, positions or extra inferred data are added.

QR library: Project Nayuki, MIT, python/qrcodegen.py from QR-Code-generator commit
777682a64202fdb837b50e351b25b7ddb27852c4. License retained in the bundled source.

Policy references rechecked 2026-09-06: https://developer.riotgames.com/docs/lol and
https://developer.riotgames.com/policies/general. Registration/audit remains unverified.
