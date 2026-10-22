"""Run one existing player harness with an isolated, owned backend process.

Use the backend venv. The live suite restarts only its own server during the
harness's 20-second restart window. No human microphone is recorded.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import site
import socket
import subprocess
import sys
import time
from urllib.request import urlopen

PROJECT = Path(__file__).resolve().parents[1]
SUITES = {'functional': (1000, 'functional.json'), 'live-walk': (960, 'live-walk.json'),
          'images': (600, 'journey.json'), 'soak': (2800, 'soak.json'),
          'characters': (700, 'journey.json'), 'conversation': (600, 'journey.json')}


def stop_owned(process):
    if process is not None and process.poll() is None:
        process.terminate()
        try:
            process.wait(timeout=15)
        except subprocess.TimeoutExpired:
            process.kill()
            process.wait(timeout=15)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--player', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--suite', choices=SUITES, required=True)
    parser.add_argument('--port', type=int, default=5078)
    args = parser.parse_args()
    player, output = args.player.resolve(), args.output.resolve()
    if not player.is_file() or output.exists() or not 1024 <= args.port <= 65535:
        parser.error('Require an existing player, a new output directory, and a valid nonprivileged port.')
    with socket.socket() as probe:
        try:
            probe.bind(('127.0.0.1', args.port))
        except OSError:
            parser.error('Acceptance port is occupied; existing services will not be stopped.')
    output.mkdir(parents=True)
    profile = output / 'harness'
    profile.mkdir()
    offline = args.suite in ('functional', 'soak', 'characters')
    (profile / 'isolated-preferences.json').write_text(json.dumps({'entries': [
        {'key': 'Lumen.VoiceEnabled', 'value': '0'}, {'key': 'Lumen.VoiceChoiceSeen', 'value': '1'},
        {'key': 'Lumen.Offline', 'value': '1' if offline else '0'},
        {'key': 'Lumen.VisualAids', 'value': '0'}]}), encoding='utf-8')
    env = os.environ.copy()
    env.update(FLASK_PORT=str(args.port), LUMEN_BACKEND_URL=f'http://127.0.0.1:{args.port}',
               LUMEN_IMAGE_CACHE=str(output / 'image-cache'))
    backend, game, server_log = None, None, None
    receipt = {'suite': args.suite, 'player': str(player), 'startedUtc': time.strftime('%Y-%m-%dT%H:%M:%SZ', time.gmtime()),
               'playerSha256': hashlib.sha256(player.read_bytes()).hexdigest(),
               'commit': subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=PROJECT, text=True).strip(),
               'sourceDiffSha256': hashlib.sha256(subprocess.check_output(['git', 'diff', 'HEAD', '--binary'], cwd=PROJECT)).hexdigest(),
               'backendPort': args.port, 'backendRestarted': False, 'naturalMicrophoneTest': False}
    base_python = Path(sys.base_prefix) / 'python.exe'
    sites = [p for p in site.getsitepackages() if Path(p).is_dir()]
    source = f'import site,runpy; [site.addsitedir(p) for p in {sites!r}]; runpy.run_path({str(PROJECT / "embodied-ai-backend/run_local.py")!r},run_name="__main__")'

    def start_backend():
        nonlocal server_log
        server_log = (output / ('backend-restarted.log' if receipt['backendRestarted'] else 'backend.log')).open('w', encoding='utf-8')
        process = subprocess.Popen([str(base_python), '-I', '-u', '-c', source], cwd=PROJECT,
                                   env=env, stdout=server_log, stderr=subprocess.STDOUT,
                                   creationflags=subprocess.CREATE_NO_WINDOW)
        try:
            for _ in range(60):
                if process.poll() is not None:
                    raise RuntimeError('Owned backend exited; inspect its log.')
                try:
                    with urlopen(env['LUMEN_BACKEND_URL'] + '/health', timeout=5) as response:
                        health = json.load(response)
                    if health.get('service') == 'embodied-ai-local' and health.get('status') == 'ok':
                        return process
                except OSError:
                    pass
                time.sleep(.5)
            raise RuntimeError('Owned backend did not become healthy.')
        except BaseException:
            stop_owned(process)
            raise

    try:
        backend = start_backend()
        flag = '-lumen-verify-dir' if args.suite == 'characters' else '-lumen-' + args.suite + '-dir'
        arguments = [str(player), '-screen-fullscreen', '0', '-screen-width', '1280', '-screen-height', '720',
                     '-force-d3d11', '-logFile', str(output / 'player.log'), flag, str(output / 'harness')]
        if args.suite == 'live-walk':
            arguments += ['-lumen-metrics-dir', str(output / 'metrics')]
        game = subprocess.Popen(arguments, cwd=output, env=env)
        receipt.update(playerPid=game.pid, backendPid=backend.pid)
        started = last_progress = time.monotonic()
        while game.poll() is None:
            elapsed = time.monotonic() - started
            if elapsed > SUITES[args.suite][0]:
                raise TimeoutError('Bounded acceptance timeout; partial evidence is preserved.')
            report_path = output / 'harness' / SUITES[args.suite][1]
            try:
                report = json.loads(report_path.read_text())
            except (OSError, json.JSONDecodeError):
                report = {}
            if args.suite == 'live-walk' and not receipt['backendRestarted'] and len(report.get('answers', [])) == 1 and report.get('questions') == 1:
                stop_owned(backend)
                server_log.close()
                receipt['backendRestarted'] = True
                receipt['restartUtc'] = time.strftime('%Y-%m-%dT%H:%M:%SZ', time.gmtime())
                backend = start_backend()
                receipt['restartedBackendPid'] = backend.pid
                print('Restarted only the owned backend during the dedicated harness window.', flush=True)
            if time.monotonic() - last_progress >= 30:
                print(json.dumps({'suite': args.suite, 'elapsedSeconds': round(elapsed), 'status': report.get('status'),
                                  'checks': len(report.get('checks', [])), 'errors': len(report.get('errors', [])),
                                  'completedCycles': report.get('completedCycles')}), flush=True)
                last_progress = time.monotonic()
            time.sleep(.5)
        receipt['playerExitCode'] = game.returncode
        receipt['result'] = json.loads((output / 'harness' / SUITES[args.suite][1]).read_text())
    except Exception as error:
        receipt['runnerError'] = type(error).__name__ + ': ' + str(error)
    finally:
        stop_owned(game)
        stop_owned(backend)
        if server_log:
            server_log.close()
        receipt['finishedUtc'] = time.strftime('%Y-%m-%dT%H:%M:%SZ', time.gmtime())
        (output / 'run.json').write_text(json.dumps(receipt, indent=2), encoding='utf-8')
    print(json.dumps({'suite': args.suite, 'playerExitCode': receipt.get('playerExitCode'),
                      'runnerError': receipt.get('runnerError'), 'evidence': str(output)}), flush=True)
    return 1 if receipt.get('runnerError') or receipt.get('playerExitCode') else 0


if __name__ == '__main__':
    raise SystemExit(main())
