# About 10 MB of colored output, then the elapsed time. Run in pwsh 7.
$e = [char]27
$line = ("lorem ipsum dolor sit amet " * 3) + "0123456789"
$block = (1..1000 | ForEach-Object { "$e[3$($_ % 8)m$line$e[0m" }) -join "`n"
$count = [int](10MB / ($block.Length + 1))

$watch = [Diagnostics.Stopwatch]::StartNew()
foreach ($i in 1..$count) { [Console]::Out.WriteLine($block) }
$watch.Stop()
"{0} blocks of {1} KB in {2:N1} s" -f $count, [int](($block.Length + 1) / 1KB), $watch.Elapsed.TotalSeconds
