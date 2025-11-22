param
(
    [string]$Path,
    [string]$OldNamespace,
    [string]$NewNamespace
)

Write-Host "Replacing namespace in files under: $Path"
Write-Host "Old: '$OldNamespace' -> New: '$NewNamespace'"

Get-ChildItem -Path $Path -Recurse -Filter "*.cs" | ForEach-Object{
    $filePath = $_.FullName
    Write-Host "Processing file: $filePath"

    try
    {
        $content = Get-Content $filePath -Raw -Encoding UTF8
        $newContent = $content -replace "\bnamespace $OldNamespace\b", "namespace $NewNamespace"

        if ($content -ne $newContent)
        {
            Set-Content $filePath -Value $newContent -Encoding UTF8 -Force
            Write-Host "  Namespace replaced successfully."
        }
        else
        {
            Write-Host "  No replacement needed or found for this file."
        }
    }
    catch
    {
        Write-Error "Error processing file '$filePath': $($_.Exception.Message)"
    }
}

Write-Host "Namespace replacement completed."
