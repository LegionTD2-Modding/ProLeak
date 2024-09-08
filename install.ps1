# Check if Python is installed
if (!(Get-Command python -ErrorAction SilentlyContinue)) {
    Write-Host "Python not found. Installing..."
    
    # Download Python installer
    $pythonUrl = "https://www.python.org/ftp/python/3.9.7/python-3.9.7-amd64.exe"
    $pythonInstaller = "$env:TEMP\python-installer.exe"
    Invoke-WebRequest -Uri $pythonUrl -OutFile $pythonInstaller

    # Install Python
    Start-Process -FilePath $pythonInstaller -ArgumentList "/quiet", "InstallAllUsers=1", "PrependPath=1" -Wait

    # Refresh environment variables
    $env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
}

# Install ProLeak
pip install proleak

# Run ProLeak installer
python -m proleak.installer

Write-Host "ProLeak installation complete!"
