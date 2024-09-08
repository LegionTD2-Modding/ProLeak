#!/bin/bash

# Check if Python is installed
if ! command -v python3 &> /dev/null
then
    echo "Python3 not found. Installing..."
    
    # Install Python (this might need sudo)
    if [ -f /etc/debian_version ]; then
        sudo apt-get update
        sudo apt-get install -y python3 python3-pip
    elif [ -f /etc/redhat-release ]; then
        sudo yum install -y python3 python3-pip
    else
        echo "Unsupported distribution. Please install Python3 manually."
        exit 1
    fi
fi

# Install ProLeak
pip3 install proleak

# Run ProLeak installer
python3 -m proleak.installer

echo "ProLeak installation complete!"
