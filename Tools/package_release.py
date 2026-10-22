"""Stage an existing Windows build and installed Python closure; never downloads.

Use the backend virtual environment. Private acceptance only until redistribution
requirements for the shipped assets and runtimes are verified.
"""
from pathlib import Path
import argparse
import hashlib
import importlib.metadata as metadata
import json
import shutil
import sys
import zipfile
from packaging.requirements import Requirement

PROJECT = Path(__file__).resolve().parents[1]


def copy_runtime(destination, offline_speech=False):
    runtime = destination / 'Runtime/Python'
    runtime.mkdir(parents=True)
    for item in Path(sys.base_prefix).iterdir():
        if item.name not in ('DLLs', 'Lib', 'LICENSE.txt', 'python.exe', 'pythonw.exe',
                             'python3.dll', f'python{sys.version_info.major}{sys.version_info.minor}.dll',
                             'vcruntime140.dll', 'vcruntime140_1.dll'):
            continue
        target = runtime / item.name
        if item.is_dir():
            shutil.copytree(item, target, ignore=shutil.ignore_patterns(
                'site-packages', '__pycache__', 'test', 'tests', 'idlelib', 'tkinter', 'ensurepip', '*.pyc'))
        else:
            shutil.copy2(item, target)
    if not (runtime / 'python.exe').is_file():
        raise RuntimeError('Active base interpreter has no portable python.exe.')
    site = runtime / 'Lib/site-packages'
    site.mkdir(parents=True, exist_ok=True)
    pending = ['Flask', 'Flask-Cors', 'requests', 'Pillow', 'flask-sock', 'google-genai']
    if offline_speech:
        pending.append('faster-whisper')
    seen, records = set(), []
    # Recent GenAI SDK modules have deep paths. Zip pure Google modules so a
    # package in a folder with spaces works without changing Windows policies.
    google_archive = site / 'google-python.zip'
    google_entries = set()
    while pending:
        dist = metadata.distribution(pending.pop())
        name = dist.metadata['Name'].lower().replace('_', '-')
        if name in seen:
            continue
        seen.add(name)
        for entry in dist.files or []:
            if '..' in Path(entry).parts or '__pycache__' in Path(entry).parts or str(entry).endswith('.pyc'):
                continue
            source, target = Path(dist.locate_file(entry)), site / entry
            if source.is_file():
                if Path(entry).parts[0] == 'google':
                    if source.suffix.lower() in ('.pyd', '.dll'):
                        raise RuntimeError('Native Google module requires a reviewed packaging layout.')
                    with zipfile.ZipFile(google_archive, 'a', compression=zipfile.ZIP_DEFLATED) as archive:
                        name_in_zip = Path(entry).as_posix()
                        for parent in reversed(Path(entry).parents):
                            directory = parent.as_posix().rstrip('/') + '/'
                            if directory != './' and directory not in google_entries:
                                archive.writestr(directory, b'')
                                google_entries.add(directory)
                        if name_in_zip not in google_entries:
                            archive.write(source, name_in_zip)
                            google_entries.add(name_in_zip)
                    continue
                target.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(source, target)
        records.append({'name': dist.metadata['Name'], 'version': dist.version,
                        'licenseMetadata': dist.metadata.get('License-Expression') or dist.metadata.get('License') or 'Unknown',
                        'notices': 'Runtime/Python/Lib/site-packages/' + dist._path.name})
        for raw in dist.requires or []:
            dep = Requirement(raw)
            if dep.marker is None or dep.marker.evaluate({'extra': ''}):
                pending.append(dep.name)
    if google_archive.exists():
        (site / 'lumen-google.pth').write_text('google-python.zip\n', encoding='utf-8')
    return sorted(records, key=lambda r: r['name'])


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--build', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True, help='New destination; never overwritten')
    parser.add_argument('--stt-model', type=Path, help='Optional installed Whisper model directory')
    parser.add_argument('--piper', type=Path, help='Optional licensed Piper runtime/voice directory')
    args = parser.parse_args()
    build, dest = args.build.resolve(), args.output.resolve()
    if not (build / 'Lumen.exe').is_file() or not (build / 'Lumen_Data').is_dir():
        parser.error('Build must contain Lumen.exe and Lumen_Data; packaging cannot substitute for building.')
    if dest.exists() or dest == build or dest.is_relative_to(build) or build.is_relative_to(dest):
        parser.error('Choose a new output directory separate from the build.')
    if args.stt_model and not (args.stt_model / 'model.bin').is_file():
        parser.error('Optional speech model must contain model.bin.')
    if args.piper and not args.piper.is_dir():
        parser.error('Optional Piper runtime directory is missing.')
    dest.mkdir(parents=True)
    for item in build.iterdir():
        if item.name.endswith(('_BurstDebugInformation_DoNotShip', '_BackUpThisFolder_ButDontShipItWithYourGame')) or item.suffix in ('.pdb', '.log'):
            continue
        if item.is_dir():
            shutil.copytree(item, dest / item.name)
        else:
            shutil.copy2(item, dest / item.name)
    records = copy_runtime(dest, bool(args.stt_model))
    backend = dest / 'embodied-ai-backend'
    backend.mkdir()
    for source in (PROJECT / 'embodied-ai-backend').glob('*.py'):
        shutil.copy2(source, backend / source.name)
    (backend / 'data').mkdir()
    shutil.copy2(PROJECT / 'Assets/Resources/LumenCharacterIdentities.json', backend / 'data/characters.json')
    if args.stt_model:
        shutil.copytree(args.stt_model, backend / 'runtime/models/base.en',
                        ignore=shutil.ignore_patterns('.cache', '__pycache__', '*.pyc'))
    if args.piper:
        shutil.copytree(args.piper, dest / 'Runtime/Piper', ignore=shutil.ignore_patterns('__pycache__'))
    (dest / 'Tools').mkdir()
    shutil.copy2(PROJECT / 'Tools/Start-Lumen.ps1', dest / 'Tools/Start-Lumen.ps1')
    shutil.copy2(PROJECT / 'Docs/AI/PackageStartHere.md', dest / 'START_HERE.md')
    shutil.copy2(PROJECT / 'Docs/AI/DistributionLedger-2026-10-22.md', dest / 'DISTRIBUTION_STATUS.md')
    (dest / 'Notices').mkdir()
    shutil.copy2(PROJECT / 'Assets/Art/Rocketbox/LICENSE.md', dest / 'Notices/Microsoft-Rocketbox-MIT.md')
    shutil.copy2(PROJECT / 'Assets/UnityTechnologies/license.txt', dest / 'Notices/UnityTechnologies-license.txt')
    for source, name in (
        ('Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt', 'LiberationSans-OFL.txt'),
        ('Assets/ImportedAssets/Quaternius/Animations/License.txt', 'Quaternius-Animations-CC0.txt'),
        ('Assets/ImportedAssets/Quaternius/BaseCharacters/License_Standard.txt', 'Quaternius-Standard-CC0.txt'),
        ('Assets/Resources/MuseumPaintings/manifest.json', 'Met-Painting-Credits.json'),
        ('Assets/ImportedAssets/OnlineModels/README.md', 'NASA-Smithsonian-Source-Notes.md'),
        ('Docs/AI/Met-Provenance-2026-10-22.json', 'Met-Current-API-Evidence.json'),
    ):
        shutil.copy2(PROJECT / source, dest / 'Notices' / name)
    (dest / 'Start-Lumen.cmd').write_text('@echo off\npowershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\\Start-Lumen.ps1"\n')
    (dest / 'RUNTIME_INVENTORY.json').write_text(json.dumps({
        'python': sys.version, 'distributions': records,
        'offlineSpeechModel': bool(args.stt_model), 'piperIncluded': bool(args.piper),
        'distributionStatus': 'Private acceptance only; asset and runtime redistribution review remains required.'
    }, indent=2), encoding='utf-8')
    (dest / 'PACKAGE_SHA256.txt').write_text('\n'.join(
        hashlib.sha256(p.read_bytes()).hexdigest() + '  ' + p.relative_to(dest).as_posix()
        for p in sorted(dest.rglob('*')) if p.is_file()), encoding='utf-8')
    print('Staged', dest, 'with', len(records), 'installed Python distributions.')


if __name__ == '__main__':
    main()
