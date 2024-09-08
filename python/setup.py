from setuptools import setup, find_packages

setup(
    name="proleak",
    version="0.1.0",
    packages=find_packages(),
    install_requires=[
        "requests",
        # other dependencies...
    ],
    package_data={
        "proleak": ["installer.py", "ProLeakEngine.dll"],
    },
    entry_points={
        "console_scripts": [
            "proleak-install=proleak.installer:main",
        ],
    },
    # other setup parameters...
)