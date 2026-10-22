"""Fixture checks for packaged launcher quoting, shutdown and port ownership.

The fixture child is Python, not a game acceptance result. Player harness runs
remain separate. Run using the development backend venv after staging a package.
"""
import argparse
import json
import os
from pathlib import Path
import socket
import subprocess
import time


def free_port():
    with socket.socket() as probe:
        probe.bind(('127.0.0.1', 0))
        return probe.getsockname()[1]


def listening(port):
    with socket.socket() as probe:
        probe.settimeout(.5)
        return probe.connect_ex(('127.0.0.1', port)) == 0


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--package', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    package, output = args.package.resolve(), args.output.resolve()
    python, launcher = package / 'Runtime/Python/python.exe', package / 'Tools/Start-Lumen.ps1'
    if not python.is_file() or not (package / 'Lumen.exe').is_file() or output.exists():
        parser.error('Require a staged player/runtime and a new output directory.')
    output.mkdir(parents=True)
    outside = output / 'Working Outside Package'
    outside.mkdir()
    fixture, result = None, {}
    try:
        port = free_port()
        echo = output / 'echo_args.py'
        echo.write_text('import json,sys\nfrom pathlib import Path\nPath(sys.argv[1]).write_text(json.dumps(sys.argv[2:]))\n')
        values = ['space value', 'embedded"quote', 'trailing\\']
        config = {'launcher': str(launcher), 'python': str(python), 'script': str(echo),
                  'argsOut': str(output / 'actual-args.json'), 'port': port, 'args': values}
        (output / 'config.json').write_text(json.dumps(config), encoding='utf-8')
        wrapper = output / 'run-fixture.ps1'
        wrapper.write_text('''$ErrorActionPreference='Stop'
$config=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'config.json') -Raw|ConvertFrom-Json
Remove-Item Env:LUMEN_PYTHON -ErrorAction SilentlyContinue
& $config.launcher -PlayerPath $config.python -Port $config.port -PlayerArguments (@('-I',$config.script,$config.argsOut)+@($config.args))
''', encoding='utf-8')
        env = os.environ.copy()
        env.pop('LUMEN_PYTHON', None)
        command = ['powershell', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', str(wrapper)]
        run = subprocess.run(command, cwd=outside, env=env, capture_output=True, text=True, timeout=120,
                             creationflags=subprocess.CREATE_NO_WINDOW)
        (output / 'launcher-fixture.log').write_text(run.stdout + run.stderr, encoding='utf-8')
        result['launcherExitCode'] = run.returncode
        actual = json.loads((output / 'actual-args.json').read_text()) if (output / 'actual-args.json').exists() else None
        result['argumentsPreserved'] = actual == values
        result['ownedBackendStopped'] = not listening(port)
        fixture_port = free_port()
        fixture = subprocess.Popen([str(python), '-I', '-m', 'http.server', str(fixture_port), '--bind', '127.0.0.1'],
                                   cwd=outside, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
                                   creationflags=subprocess.CREATE_NO_WINDOW)
        for _ in range(40):
            if listening(fixture_port):
                break
            time.sleep(.25)
        if not listening(fixture_port):
            raise RuntimeError('Owned unrelated-service fixture did not start.')
        conflict = subprocess.run(['powershell', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', str(launcher),
                                   '-PlayerPath', str(python), '-Port', str(fixture_port)], cwd=outside, env=env,
                                  capture_output=True, text=True, timeout=60, creationflags=subprocess.CREATE_NO_WINDOW)
        (output / 'port-conflict.log').write_text(conflict.stdout + conflict.stderr, encoding='utf-8')
        result['occupiedPortRefused'] = conflict.returncode != 0 and 'Another service uses port' in conflict.stderr
        result['unrelatedFixturePreserved'] = fixture.poll() is None and listening(fixture_port)
    except Exception as error:
        result['error'] = type(error).__name__ + ': ' + str(error)
    finally:
        if fixture is not None and fixture.poll() is None:
            fixture.terminate()
            fixture.wait(timeout=15)
        (output / 'launcher-checks.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(json.dumps(result))
    return 0 if result.get('launcherExitCode') == 0 and all(result.get(k) for k in (
        'argumentsPreserved', 'ownedBackendStopped', 'occupiedPortRefused', 'unrelatedFixturePreserved')) else 1


if __name__ == '__main__':
    raise SystemExit(main())
