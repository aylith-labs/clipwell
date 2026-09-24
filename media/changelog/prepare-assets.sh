#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"
mkdir -p public
for name in picker-light.png picker-dark.png usage-light.webm usage-dark.webm; do
  curl --fail --location --silent --show-error \
    "https://media.aylith.com/clipwell/media/$name" -o "public/$name"
done

sha256sum -c <<'CHECKSUMS'
80a77f54b2650939fd47e8875c2897ccb8b39270ce9a52954a6d921840022f35  public/picker-light.png
235069c30f5a191789c9db2a9b3bb717f14faff9570e385953a992f27d5fd727  public/picker-dark.png
fdad1573636bf331859b8f30c894e50e6e59a33d356494921f958a8bd1eb5108  public/usage-light.webm
8d6f2b156a9cee5fb01ff39cb262c35cc63d38abd3ef9389c4dfeebb1cb454f8  public/usage-dark.webm
CHECKSUMS
