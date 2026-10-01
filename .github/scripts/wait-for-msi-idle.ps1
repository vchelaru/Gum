# Waits until no Windows Installer install is running, or until TimeoutSeconds pass. Windows
# Installer holds the Global\_MSIExecute mutex for the whole of an install, so it exists exactly
# while another install would fail with 0x652.
#
# Usage: wait-for-msi-idle.ps1 [-TimeoutSeconds 240] [-MutexName 'Global\_MSIExecute']
param(
    [int]$TimeoutSeconds = 240,
    [string]$MutexName = 'Global\_MSIExecute'
)

function Test-InstallRunning {
    try {
        $mutex = $null
        if ([System.Threading.Mutex]::TryOpenExisting($MutexName, [ref]$mutex)) {
            $mutex.Dispose()
            return $true
        }
        return $false
    }
    catch [System.UnauthorizedAccessException] {
        # The mutex exists but this account can't open it.
        return $true
    }
}

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
while (Test-InstallRunning) {
    if ($stopwatch.Elapsed.TotalSeconds -ge $TimeoutSeconds) {
        Write-Output "::warning::an MSI install was still running after $TimeoutSeconds s"
        exit 0
    }
    Start-Sleep -Seconds 5
}
Write-Output "No MSI install running (waited $([int]$stopwatch.Elapsed.TotalSeconds) s)"
