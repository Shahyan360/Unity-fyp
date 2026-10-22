"""Packaging boundaries and a real relocated interpreter import check."""
from pathlib import Path
import subprocess
import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import package_release


def test_missing_player_is_rejected_before_writing(tmp_path):
    destination = tmp_path / 'must-not-exist'
    result = subprocess.run([sys.executable, str(Path(package_release.__file__)),
                             '--build', str(tmp_path / 'missing'), '--output', str(destination)],
                            capture_output=True, text=True, timeout=20)
    assert result.returncode == 2
    assert 'Build must contain' in result.stderr
    assert not destination.exists()


def test_installed_runtime_imports_after_relocation_from_external_directory(tmp_path):
    package = tmp_path / 'Package With Spaces'
    records = package_release.copy_runtime(package)
    assert {'flask', 'google-genai', 'flask-sock', 'pillow'} <= {r['name'].lower() for r in records}
    result = subprocess.run([str(package / 'Runtime/Python/python.exe'), '-I', '-c',
                             'import sys; from flask import Flask; from google import genai; '
                             'import PIL, flask_sock; print(sys.prefix)'],
                            cwd=tmp_path, capture_output=True, text=True, timeout=90)
    assert result.returncode == 0, result.stderr
    assert Path(result.stdout.strip()).resolve() == (package / 'Runtime/Python').resolve()
