#!/usr/bin/env python3
"""Select only this product's actual GitHub Pages binding."""
import argparse
import json
import os
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument("metadata")
parser.add_argument("--expected-base")
args = parser.parse_args()
metadata = json.loads(Path(args.metadata).read_text())
if metadata.get("build_type") != "workflow":
    raise SystemExit("Unexpected Pages build mode")
if metadata.get("cname") is None:
    base = "/clipwell"
    allowed = {"https://aylith-labs.github.io/clipwell/"}
elif metadata.get("cname") == "clipwell.aylith.com":
    base = ""
    allowed = {"http://clipwell.aylith.com/", "https://clipwell.aylith.com/"}
else:
    raise SystemExit("Unexpected Pages custom domain")
if metadata.get("html_url") not in allowed:
    raise SystemExit("Unexpected Pages URL")
if args.expected_base is not None and args.expected_base != base:
    raise SystemExit("Pages target changed after build")
if args.expected_base is None:
    with open(os.environ["GITHUB_OUTPUT"], "a") as output:
        output.write(f"base={base}\n")
