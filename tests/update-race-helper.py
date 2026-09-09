"""Offline, isolated updater race fixture; never packaged into the app."""
import pathlib
import sys
import time

time.sleep(0.2)
pathlib.Path(sys.argv[2]).write_bytes((pathlib.Path(__file__).parent / 'race-fixture.bin').read_bytes())
