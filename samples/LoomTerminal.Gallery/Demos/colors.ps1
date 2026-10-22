# 16 / 256 / truecolor tables. Run in pwsh 7 (uses `e).
$e = [char]27

"16 colors (foreground, then background)"
foreach ($base in 30, 90) {
    -join (0..7 | ForEach-Object { "$e[$($base + $_)m██$e[0m" })
}
foreach ($base in 40, 100) {
    -join (0..7 | ForEach-Object { "$e[$($base + $_)m  $e[0m" })
}

""
"256 colors"
foreach ($row in 0..15) {
    -join (0..15 | ForEach-Object { "$e[48;5;$($row * 16 + $_)m  $e[0m" })
}

""
"Truecolor gradient"
foreach ($line in 0..3) {
    -join (0..71 | ForEach-Object {
        $r = [int](255 - $_ * 255 / 71)
        $g = [int]($_ * 510 / 71); if ($g -gt 255) { $g = 510 - $g }
        $b = [int]($_ * 255 / 71)
        "$e[48;2;$r;$g;${b}m $e[0m"
    })
}
