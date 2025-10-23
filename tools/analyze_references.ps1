$root = "C:\Users\ivano\source\repos\EventForge"
$files = git -C $root ls-files "EventForge.Client/**/*.razor"
foreach ($f in $files) {
 $name = [System.IO.Path]::GetFileNameWithoutExtension($f)
 $pattern1 = "<${name}[ >]"
 $pattern2 = "\b${name}\b"
 $refs = git -C $root grep -n -E $pattern1 -- ":!$f"2>$null
 if (-not $refs) { $refs = git -C $root grep -n -E $pattern2 -- ":!$f"2>$null }
 if ($refs) { $count = ($refs | Measure-Object).Count; Write-Output "$f|1|$count" } else { Write-Output "$f|0|0" }
}
