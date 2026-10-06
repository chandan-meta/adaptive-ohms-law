"""Speaks a manifest of lines with one of Microsoft's free neural voices through edge-tts, four lines at a time.
Usage: python Tools/edge_bake.py <manifest.json> [--voice=en-US-AnaNeural] [--rate=+10%] [--pitch=-4Hz]
The manifest is a UTF-8 JSON array of {"id", "text", "out"}; each "out" (an absolute .mp3 path) is written only when its
line succeeded. One line per result on stdout, "OK <id>" or "FAIL <id> <reason>"; exit code 0 only when every line succeeded.
Called by Assets/OhmsLaw/Editor/BakeBlockoutVO.cs when the Wattson proxy has no ElevenLabs key (`pip install edge-tts`)."""
import argparse
import asyncio
import json
import os
import sys

import edge_tts

RETRIES, PARALLEL = 3, 4


async def speak(line, args, gate):
    reason = ""
    for attempt in range(RETRIES):
        try:
            async with gate:
                tts = edge_tts.Communicate(line["text"], args.voice, rate=args.rate, pitch=args.pitch)
                data = b"".join([m["data"] async for m in tts.stream() if m["type"] == "audio"])
            if not data:
                raise RuntimeError("no audio received")
            os.makedirs(os.path.dirname(line["out"]) or ".", exist_ok=True)
            with open(line["out"], "wb") as f:
                f.write(data)
            print("OK", line["id"], flush=True)
            return True
        except Exception as e:  # any failure is retried, then reported
            reason = f"{type(e).__name__}: {e}".strip()
            if attempt + 1 < RETRIES:
                await asyncio.sleep(1.5 * (attempt + 1))
    print("FAIL", line["id"], reason, flush=True)
    return False


async def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("manifest")
    parser.add_argument("--voice", default="en-US-AnaNeural")
    parser.add_argument("--rate", default="+0%")
    parser.add_argument("--pitch", default="+0Hz")
    args = parser.parse_args()
    with open(args.manifest, encoding="utf-8") as f:
        lines = json.load(f)
    gate = asyncio.Semaphore(PARALLEL)
    results = await asyncio.gather(*(speak(line, args, gate) for line in lines))
    failed = results.count(False)
    if failed:
        print(f"{failed} of {len(lines)} line(s) failed", file=sys.stderr)
    return 1 if failed else 0


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    sys.exit(asyncio.run(main()))
