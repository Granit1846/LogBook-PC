param(
    [Parameter(Mandatory=$true)][string]$Executable,
    [Parameter(Mandatory=$true)][string]$NotesFile,
    [string]$DownloadUrl = ''
)
$ErrorActionPreference='Stop'
$taskExe=(Resolve-Path -LiteralPath $Executable).Path
$taskVersion=[Diagnostics.FileVersionInfo]::GetVersionInfo($taskExe).FileVersion
$taskParts=$taskVersion.Split('.')
$taskVersion=$taskParts[0..2] -join '.'
$taskNotes=Get-Content -LiteralPath $NotesFile -Raw -Encoding UTF8 | ConvertFrom-Json
$taskNotesProperty=$taskNotes.PSObject.Properties[$taskVersion]
if($null -eq $taskNotesProperty -or [string]::IsNullOrWhiteSpace([string]$taskNotesProperty.Value)) {
    throw "Release notes missing for $taskVersion"
}
if($DownloadUrl -and (![Uri]::IsWellFormedUriString($DownloadUrl,[UriKind]::Absolute) -or ! $DownloadUrl.StartsWith('https://'))) {
    throw 'DownloadUrl must be an HTTPS URL'
}
$taskHasher=[Security.Cryptography.SHA256]::Create()
$taskStream=[IO.File]::OpenRead($taskExe)
try { $taskHash=([BitConverter]::ToString($taskHasher.ComputeHash($taskStream))).Replace('-','').ToLowerInvariant() }
finally { $taskStream.Dispose();$taskHasher.Dispose() }
$taskManifest=[ordered]@{
    version=$taskVersion
    url=$DownloadUrl
    notes=[string]$taskNotesProperty.Value
    notesEn=[string]$taskNotes.PSObject.Properties[($taskVersion+'.en')].Value
    builtAt=[DateTime]::UtcNow.ToString('o')
    platform='windows-x64'
    sha256=$taskHash
    sizeBytes=(Get-Item -LiteralPath $taskExe).Length
}
$taskDirectory=Split-Path -Parent $taskExe
$taskHistory=Join-Path $taskDirectory 'releases'
New-Item -ItemType Directory -Path $taskHistory -Force | Out-Null
$taskJson=$taskManifest | ConvertTo-Json -Depth 5
$taskUtf8=New-Object Text.UTF8Encoding($false)
[IO.File]::WriteAllText((Join-Path $taskHistory ($taskVersion+'.json')),$taskJson,$taskUtf8)
[IO.File]::WriteAllText((Join-Path $taskDirectory 'version.json'),$taskJson,$taskUtf8)
Write-Output "Release JSON created: $taskVersion"
