$root = "C:\Users\ivano\source\repos\Prym"
$files = git -C $root ls-files "Prym.Client/Pages/**/*.razor"
foreach ($f in $files) {
 $name = [System.IO.Path]::GetFileNameWithoutExtension($f)
 $refs = git -C $root grep -n "@page\s+\"" -- "Prym.Client/**/*.razor"2>$null | Select-String -Pattern $name -SimpleMatch
 if ($refs) { Write-Output "$f|1|Referenced by pages" } else { Write-Output "$f|0|No page refs" }
}
