param(
    [Parameter(Mandatory=$true)][ValidateSet('Enable','Disable')][string]$Mode,
    [Parameter(Mandatory=$true)][string]$Executable
)
$ErrorActionPreference = 'Stop'
try {
    $identity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    $taskName = 'StatsScreen-' + $identity.User.Value
    $existing = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
    if ($existing -and $existing.Actions.Execute -ne $Executable) {
        throw 'A Stats Screen startup task points to a different installation. Remove that installation first.'
    }
    if ($Mode -eq 'Disable') {
        if ($existing) { Unregister-ScheduledTask -TaskName $taskName -Confirm:$false }
        exit 0
    }
    if (-not (Test-Path -LiteralPath $Executable -PathType Leaf)) { throw 'Installed executable is missing.' }
    $action = New-ScheduledTaskAction -Execute $Executable -WorkingDirectory (Split-Path -Parent $Executable)
    $trigger = New-ScheduledTaskTrigger -AtLogOn -User $identity.Name
    $principal = New-ScheduledTaskPrincipal -UserId $identity.Name -LogonType Interactive -RunLevel Highest
    $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit ([TimeSpan]::Zero) -MultipleInstances IgnoreNew
    Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Description 'Start Stats Screen for this user at Windows sign-in.' -Force | Out-Null
    exit 0
} catch {
    Write-Error $_ -ErrorAction Continue
    exit 1
}
