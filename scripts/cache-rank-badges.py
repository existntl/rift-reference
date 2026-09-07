"""Cache Riot client rank emblems from the pinned CommunityDragon asset mirror."""
from pathlib import Path
from urllib.request import urlopen

root = Path(__file__).resolve().parents[1] / 'cache/data/rank-badges'
root.mkdir(parents=True, exist_ok=True)
base = 'https://raw.communitydragon.org/16.17/plugins/rcp-fe-lol-shared-components/global/default/'
for tier in ('iron', 'bronze', 'silver', 'gold', 'platinum', 'emerald', 'diamond', 'master', 'grandmaster', 'challenger', 'unranked'):
    with urlopen(base + tier + '.png', timeout=30) as response:
        image = response.read(2_000_000)
    if not image.startswith(b'\x89PNG\r\n\x1a\n'):
        raise ValueError('Invalid rank badge: ' + tier)
    (root / (tier + '.png')).write_bytes(image)
print('Cached 11 rank badges')
