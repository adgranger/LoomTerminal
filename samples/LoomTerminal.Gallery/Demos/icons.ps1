# Needs a Nerd Font installed (any family); the primary font may be Cascadia Mono. Run in pwsh 7.
[Console]::OutputEncoding = [Text.Encoding]::UTF8

function Icon([int]$codePoint) { [char]::ConvertFromUtf32($codePoint) }

"Branch   U+E0A0:  |$(Icon 0xE0A0)|"
"Powerline U+E0B0: |$(Icon 0xE0B0)|  (drawn by the control)"
"Folder   U+F07B:  |$(Icon 0xF07B)|"
"Terminal U+F489:  |$(Icon 0xF489)|"
"Windows  U+E70F:  |$(Icon 0xE70F)|"
"Material U+F0001: |$(Icon 0xF0001)|  (plane 15)"
"Ruler:            |0123456789|"
