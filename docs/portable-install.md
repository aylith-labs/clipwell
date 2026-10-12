# Clipwell portable Linux package

The portable archive runs Clipwell on Linux x64 without installing .NET. It
contains the daemon, the native picker, the `clipwell` CLI, the stdio MCP
server, the web UI, and the .NET runtime they share.

## Install

Download `clipwell-VERSION-linux-x64.tar.gz` and its `.sha256` file from a
[GitHub release](https://github.com/aylith-labs/clipwell/releases), then:

```sh
sha256sum -c clipwell-VERSION-linux-x64.tar.gz.sha256
tar -xzf clipwell-VERSION-linux-x64.tar.gz
cd clipwell-VERSION-linux-x64
```

No root access is needed; the directory can live anywhere.

## Run

```sh
./bin/clipwell-daemon     # clipboard daemon on http://127.0.0.1:8787
./bin/clipwell-picker     # native picker, in another terminal
./bin/clipwell list       # recent history from the CLI
./bin/clipwell-mcp        # stdio MCP server for AI tools
```

The web UI is at `http://127.0.0.1:8787/app/` while the daemon runs. To use
another address, set `CLIPWELL_URL` for the daemon and `CLIPWELL_API` for its
clients.

## Requirements

- A desktop session with the libraries Avalonia needs for the picker.
- `wl-clipboard` on Wayland or `xclip` on X11, so the daemon can read the clipboard.
- `xdotool` for paste-back on X11. Wayland has no global hotkey; open the picker
  from the tray icon.

History is stored in your Clipwell data directory (`~/.config/Clipwell`); set
`CLIPWELL_DATA_DIR` to keep it elsewhere.

## CLI timeouts

`clipwell list` and `clipwell clear` give up after 15 seconds and never retry.
If `clear` times out, check whether the history was cleared before running it
again. `clipwell watch` streams until you press Ctrl+C.

## Licenses

Clipwell is MIT licensed (`LICENSE`). `THIRD-PARTY-NOTICES.txt` lists the
bundled packages and their licenses; the .NET runtime's own notices are in
`runtime/`.
