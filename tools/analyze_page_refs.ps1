$root = "C:\Users\ivano\source\repos\EventForge"
$files = git -C $root ls-files "EventForge.Client/Pages/**/*.razor"
foreach ($f in $files) {
 $name = [System.IO.Path]::GetFileNameWithoutExtension($f)
 $refs = git -C $root grep -n "@page\s+\"" -- "EventForge.Client/**/*.razor"2>$null | Select-String -Pattern $name -SimpleMatch
 if ($refs) { Write-Output "$f|1|Referenced by pages" } else { Write-Output "$f|0|No page refs" }
}
