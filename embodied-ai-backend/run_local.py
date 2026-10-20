"""Isolated portable entrypoint: -I excludes arbitrary working-directory imports."""
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import app
import os

if __name__ == '__main__':
    app.create_app().run(host='127.0.0.1', port=int(os.getenv('FLASK_PORT', '5000')), debug=False)
