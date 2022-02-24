$relativeFileDirname = $args[0]
$fileBasenameNoExtension = $args[1]
$fileExtname = $args[2]
$fileBasename = "$fileBasenameNoExtension$fileExtname"

if ($relativeFileDirname -eq "src") {
    $content = Get-Content -Raw "$relativeFileDirname/$fileBasename"
    $found = $content -match '(?s)// start(.*)// end'
    if ($found -and $Matches[1]) {
        $trimmed = ($Matches[1] -replace '(?m)^        ', '').Trim()
        $trimmed > "script.$fileBasenameNoExtension.txt"
        Set-Clipboard -Value $trimmed
        Write-Output "Exported & copied to clipboard"
    }
    else {
        Write-Error "RegExp does not match"
        Set-Clipboard ""
    }
}
else {
    Write-Warning "Current file is not in .\src\"
}
