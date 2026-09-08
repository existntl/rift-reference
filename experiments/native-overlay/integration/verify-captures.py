"""Validate GPU captures from the owned hook-test host, without image dependencies."""
import pathlib
import struct
import sys

directory = pathlib.Path(sys.argv[1])
counts = []
for name in ('on.bmp', 'off.bmp'):
    data = (directory / name).read_bytes()
    offset = struct.unpack_from('<I', data, 10)[0]
    width, height = struct.unpack_from('<ii', data, 18)
    assert struct.unpack_from('<H', data, 28)[0] == 32
    pixels = list(struct.iter_unpack('4B', data[offset:]))
    assert len(pixels) == width * abs(height)
    different = sum(pixel != (8, 6, 4, 255) for pixel in pixels)
    counts.append(different)
    print(f'{name}: {width}x{abs(height)}, {different} non-background pixels')
assert counts[0] > 100, 'Hook-on frame did not contain the fixture overlay'
assert counts[1] == 0, 'Disabled hook still changed the host frame'
print('PASS: hooked Present composed the overlay; disable restored the background.')
