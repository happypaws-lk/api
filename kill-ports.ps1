[CmdletBinding()]
param(
    [Parameter(Position = 0, ValueFromRemainingArguments = $true)]
    [int[]]$Ports = @(7141, 5047)
)

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "Requesting administrator privileges..." -ForegroundColor Yellow
    $arguments = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "`"$PSCommandPath`"")
    if ($PSBoundParameters.ContainsKey('Ports')) {
        $arguments += ($Ports | ForEach-Object { "$_" })
    }
    $processPath = (Get-Process -Id $PID).Path
    Start-Process -FilePath $processPath -ArgumentList $arguments -Verb RunAs
    exit
}

$foundAny = $false

foreach ($port in $Ports) {
    Write-Host "Checking for processes on port $port..." -ForegroundColor Cyan

    $pids = @()

    try {
        $connections = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
        if ($connections) {
            $pids = $connections | Select-Object -ExpandProperty OwningProcess -Unique | Where-Object { $_ -gt 0 }
        }
    }
    catch {
        # Fallback to netstat if Get-NetTCPConnection is unavailable
        $netstatLines = netstat -ano | Select-String ":$port\s+"
        $pids = $netstatLines | ForEach-Object {
            $parts = ($_ -split '\s+') | Where-Object { $_ -ne '' }
            if ($parts.Length -ge 5) { [int]$parts[-1] }
        } | Select-Object -Unique | Where-Object { $_ -gt 0 }
    }

    if (-not $pids -or $pids.Count -eq 0) {
        Write-Host "  No process found on port $port." -ForegroundColor Gray
        continue
    }

    foreach ($processId in $pids) {
        $proc = Get-Process -Id $processId -ErrorAction SilentlyContinue
        if ($proc) {
            $foundAny = $true
            Write-Host "  Found process '$($proc.ProcessName)' (PID: $processId) on port $port. Terminating..." -ForegroundColor Yellow
            try {
                Stop-Process -Id $processId -Force -ErrorAction Stop
                Write-Host "  Successfully stopped process '$($proc.ProcessName)' (PID: $processId)." -ForegroundColor Green
            }
            catch {
                Write-Warning "  Failed to stop process '$($proc.ProcessName)' (PID: $processId): $($_.Exception.Message)"
            }
        }
        else {
            Write-Host "  Process with PID $processId on port $port is no longer active." -ForegroundColor Gray
        }
    }
}

if (-not $foundAny) {
    Write-Host "No active processes found on port(s): $($Ports -join ', ')." -ForegroundColor Green
}
