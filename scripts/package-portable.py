#!/usr/bin/env python3
"""Build a portable Linux x64 Clipwell archive that needs no .NET installation.

The archive holds the daemon, native picker, CLI and stdio MCP server as
framework-dependent apps, the compiled web UI, and one copy of the .NET and
ASP.NET Core runtime taken from the SDK that built them. Small launchers in
bin/ run each app on that bundled runtime.

Usage: scripts/package-portable.py VERSION [--output DIR]
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import shutil
import subprocess
import tarfile
import tempfile
import xml.etree.ElementTree as ElementTree

ROOT = Path(__file__).resolve().parents[1]
# Package directory -> (project, assembly, launcher name in bin/)
APPS = {
    'daemon': ('daemon/Clipwell.Daemon.csproj', 'Clipwell.Daemon', 'clipwell-daemon'),
    'picker': ('ui/Clipwell.Ui.csproj', 'Clipwell.Ui', 'clipwell-picker'),
    'cli': ('cli/Clipwell.Cli.csproj', 'Clipwell.Cli', 'clipwell'),
    'mcp': ('mcp/Clipwell.Mcp.csproj', 'Clipwell.Mcp', 'clipwell-mcp'),
}
LAUNCHER = '''#!/bin/sh
set -eu
root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
exec "$root/runtime/dotnet" "$root/{app}/{assembly}.dll" "$@"
'''


def latest(directory: Path) -> str:
    versions = [path.name for path in directory.iterdir() if re.fullmatch(r'\d+\.\d+\.\d+', path.name)]
    if not versions:
        raise SystemExit(f'No runtime versions under {directory}')
    return max(versions, key=lambda version: tuple(map(int, version.split('.'))))


def dotnet_root() -> Path:
    dotnet = shutil.which('dotnet')
    if not dotnet:
        raise SystemExit('The .NET 10 SDK must be on PATH to build the package')
    return Path(dotnet).resolve().parent


def nuget_cache() -> Path:
    listing = subprocess.run(['dotnet', 'nuget', 'locals', 'global-packages', '--list'],
                             capture_output=True, text=True, check=True).stdout
    return Path(listing.split('global-packages:', 1)[1].strip())


def bundle_runtime(source: Path, target: Path) -> str:
    """Copy the dotnet host plus one NETCore and ASP.NET Core runtime version."""
    version = latest(source / 'shared/Microsoft.AspNetCore.App')
    if not (source / 'shared/Microsoft.NETCore.App' / version).is_dir():
        raise SystemExit(f'No Microsoft.NETCore.App {version} beside ASP.NET Core in {source}')
    target.mkdir()
    for name in ('dotnet', 'LICENSE.txt', 'ThirdPartyNotices.txt'):
        shutil.copy2(source / name, target / name)
    fxr = latest(source / 'host/fxr')
    shutil.copytree(source / 'host/fxr' / fxr, target / 'host/fxr' / fxr)
    for framework in ('Microsoft.NETCore.App', 'Microsoft.AspNetCore.App'):
        shutil.copytree(source / 'shared' / framework / version, target / 'shared' / framework / version)
    return version


def nuget_notices(package: Path, cache: Path) -> str:
    """List every NuGet package the apps load, with its declared license."""
    packages = {}
    for deps in package.glob('*/*.deps.json'):
        for key, library in json.loads(deps.read_text())['libraries'].items():
            if library.get('type') == 'package':
                packages[key] = None
    lines = ['NuGet packages', '==============', '']
    for key in sorted(packages, key=str.lower):
        name, version = key.rsplit('/', 1)
        license_text = 'see https://www.nuget.org/packages/' + key
        nuspec = cache / name.lower() / version.lower() / f'{name.lower()}.nuspec'
        if nuspec.is_file():
            element = next((node for node in ElementTree.parse(nuspec).iter() if node.tag.endswith('}license')), None)
            if element is not None and element.text:
                license_text = element.text.strip()
        lines.append(f'{name} {version} — {license_text}')
    return '\n'.join(lines) + '\n'


def webui_notices() -> str:
    """List the web UI's production dependencies and their licenses."""
    manifest = json.loads((ROOT / 'webui/package.json').read_text())
    lines = ['', 'Web UI packages', '===============', '']
    for name in sorted(manifest.get('dependencies', {})):
        installed = ROOT / 'webui/node_modules' / name / 'package.json'
        if installed.is_file():
            metadata = json.loads(installed.read_text())
            lines.append(f'{name} {metadata.get("version", "?")} — {metadata.get("license", "see package")}')
    return '\n'.join(lines) + '\n'


def build(version: str, output: Path) -> Path:
    if not re.fullmatch(r'\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?', version):
        raise SystemExit('VERSION must be a semantic version such as 0.3.0')
    name = f'clipwell-{version}-linux-x64'
    archive = output / f'{name}.tar.gz'
    if archive.exists():
        raise SystemExit(f'{archive} already exists; choose a new version or output directory')

    subprocess.run(['bun', 'install', '--frozen-lockfile'], cwd=ROOT / 'webui', check=True)
    subprocess.run(['bun', 'run', 'build'], cwd=ROOT / 'webui', check=True)

    with tempfile.TemporaryDirectory(prefix='clipwell-package-') as temp:
        package = Path(temp) / name
        for app, (project, assembly, launcher) in APPS.items():
            subprocess.run(['dotnet', 'publish', str(ROOT / project), '-c', 'Release',
                            '-r', 'linux-x64', '--self-contained', 'false', '-p:UseAppHost=false',
                            f'-p:Version={version}', '-p:DebugType=None', '-p:DebugSymbols=false',
                            '-o', str(package / app)], check=True)
            if not (package / app / f'{assembly}.dll').is_file():
                raise SystemExit(f'dotnet publish produced no {assembly}.dll')
        shutil.copytree(ROOT / 'webui/dist', package / 'daemon/wwwroot/app')

        runtime_version = bundle_runtime(dotnet_root(), package / 'runtime')
        (package / 'bin').mkdir()
        for app, (_, assembly, launcher) in APPS.items():
            script = package / 'bin' / launcher
            script.write_text(LAUNCHER.format(app=app, assembly=assembly))
            script.chmod(0o755)

        shutil.copy2(ROOT / 'LICENSE', package / 'LICENSE')
        shutil.copy2(ROOT / 'docs/portable-install.md', package / 'INSTALL.md')
        (package / 'THIRD-PARTY-NOTICES.txt').write_text(
            '.NET runtime: see runtime/LICENSE.txt and runtime/ThirdPartyNotices.txt\n\n'
            + nuget_notices(package, nuget_cache()) + webui_notices())
        (package / 'VERSION').write_text(f'{version}\n.NET runtime {runtime_version}\n')

        output.mkdir(parents=True, exist_ok=True)
        with tarfile.open(archive, 'w:gz') as tar:
            tar.add(package, arcname=name)
    digest = hashlib.sha256(archive.read_bytes()).hexdigest()
    archive.with_name(archive.name + '.sha256').write_text(f'{digest}  {archive.name}\n')
    return archive


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('version')
    parser.add_argument('--output', type=Path, default=ROOT / 'artifacts')
    arguments = parser.parse_args()
    print(build(arguments.version, arguments.output.resolve()))
