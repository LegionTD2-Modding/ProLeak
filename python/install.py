import os
import sys
import zipfile
import shutil
import subprocess
import requests
import time
from pathlib import Path
import tkinter as tk
from tkinter import filedialog


BEPINEX_WIN_URL = "https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.2/BepInEx_win_x64_5.4.23.2.zip"
BEPINEX_LINUX_URL = "https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.2/BepInEx_linux_x64_5.4.23.2.zip"
PROLEAK_WIN_URL = "https://github.com/LegionTD2-Modding/ProLeak/releases/download/v1.0.0/ProLeakEngine.dll"
PROLEAK_LINUX_URL = "https://github.com/LegionTD2-Modding/ProLeak/releases/download/v1.0.0/ProLeakEngine.so"


def download_file(url, filename):
    try:
        with requests.get(url, stream=True) as r:
            r.raise_for_status()
            with open(filename, 'wb') as f:
                for chunk in r.iter_content(chunk_size=8192):
                    f.write(chunk)
    except requests.exceptions.RequestException as e:
        raise Exception(f"Failed to download {filename}: {e}")


def get_game_path():
    if sys.platform == "win32":
        import winreg
        try:
            key = winreg.OpenKey(winreg.HKEY_LOCAL_MACHINE, r"SOFTWARE\WOW6432Node\Valve\Steam")
            steam_path = winreg.QueryValueEx(key, "InstallPath")[0]
            library_folders = Path(steam_path) / "steamapps" / "libraryfolders.vdf"
            with open(library_folders, 'r') as f:
                content = f.read()
            paths = [steam_path] + [line.split('"')[-2] for line in content.splitlines() if "path" in line]
            for path in paths:
                game_path = Path(path) / "steamapps" / "common" / "Legion TD 2"
                if game_path.exists():
                    return str(game_path)
        except Exception:
            pass

    # If not found or not on Windows, ask user
    root = tk.Tk()
    root.withdraw()
    game_path = filedialog.askdirectory(title="Select Legion TD 2 game folder")
    return game_path if game_path else None


def install_bepinex(game_path):
    bepinex_url = BEPINEX_WIN_URL if sys.platform == "win32" else BEPINEX_LINUX_URL
    bepinex_zip = "BepInEx.zip"

    print("Downloading BepInEx...")
    download_file(bepinex_url, bepinex_zip)

    print("Extracting BepInEx...")
    with zipfile.ZipFile(bepinex_zip, 'r') as zip_ref:
        zip_ref.extractall(game_path)

    os.remove(bepinex_zip)
    print("BepInEx installed successfully.")


def install_proleak(game_path):
    plugins_folder = Path(game_path) / "BepInEx" / "plugins"
    plugins_folder.mkdir(parents=True, exist_ok=True)

    proleak_url = PROLEAK_WIN_URL if sys.platform == "win32" else PROLEAK_LINUX_URL
    proleak_file = "ProLeakEngine.dll" if sys.platform == "win32" else "ProLeakEngine.so"

    print("Downloading ProLeak...")
    download_file(proleak_url, proleak_file)

    shutil.move(proleak_file, plugins_folder / proleak_file)
    print("ProLeak installed successfully.")


def start_game(game_path):
    if sys.platform == "win32":
        subprocess.Popen(["steam", "steam://rungameid/469600"])
    else:
        subprocess.Popen([str(Path(game_path) / "start.sh")])


def run_proleak_demo():
    from proleak import ProLeak

    def example_callback(event, params, unplug):
        print(f"Received event: {event}")
        print(f"Parameters: {params}")
        if event == "GameStarted":
            unplug()

    try:
        proleak = ProLeak()
        proleak.plug(example_callback)
    except Exception as e:
        print(f"Failed to run ProLeak demo: {e}")


def main():
    print("Starting ProLeak installer...")

    game_path = get_game_path()
    if not game_path:
        print("Legion TD 2 installation not found. Installation aborted.")
        return

    print(f"Found Legion TD 2 at: {game_path}")

    try:
        install_bepinex(game_path)
        install_proleak(game_path)

        print("Starting Legion TD 2...")
        start_game(game_path)

        print("Waiting for game to start...")
        time.sleep(10)  # Give the game some time to start

        print("Running ProLeak demo...")
        run_proleak_demo()

        print("Installation and demo complete!")
    except Exception as e:
        print(f"An error occurred during installation: {e}")
        print("Installation failed. Please try again or contact support.")