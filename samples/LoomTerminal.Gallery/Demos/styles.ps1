# SGR styles and underline variants. Run in pwsh 7.
$e = [char]27

"$e[1mbold$e[0m $e[2mdim$e[0m $e[3mitalic$e[0m $e[7minverse$e[0m $e[8minvisible$e[0m $e[9mstrikethrough$e[0m $e[53moverline$e[0m"
"$e[1;3mbold italic$e[0m $e[1;4munderlined bold$e[0m $e[31;1mred bold$e[0m $e[5mblink$e[0m"

""
"Underlines: $e[4:0mnone$e[0m $e[4:1msingle$e[0m $e[4:2mdouble$e[0m $e[4:3mcurly$e[0m $e[4:4mdotted$e[0m $e[4:5mdashed$e[0m"
"Colored curly: $e[4:3m$e[58;2;255;80;80mred wave$e[0m $e[4:2m$e[58;5;39mblue double$e[0m"

""
"Cursor shapes (DECSCUSR 1-6, back to default with 0):"
foreach ($n in 1..6) {
    Write-Host -NoNewline "$e[$n q  shape $n  "
    Start-Sleep -Milliseconds 700
}
Write-Host "$e[0 q"
