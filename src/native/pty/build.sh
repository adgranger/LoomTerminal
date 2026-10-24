#!/bin/sh
# Builds libloomterm_pty for the current OS and architecture into artifacts/native/<rid>/ (repository root).
# Usage: src/native/pty/build.sh [--test]   (--test also builds and runs the C smoke test)
set -eu

here=$(cd "$(dirname "$0")" && pwd)
root=$(cd "$here/../../.." && pwd)

case "$(uname -m)" in
    x86_64|amd64) arch=x64 ;;
    aarch64|arm64) arch=arm64 ;;
    *) echo "Unsupported architecture: $(uname -m)" >&2; exit 1 ;;
esac

case "$(uname -s)" in
    Linux) rid="linux-$arch"; ext=so; libs="-lutil"; shared="-shared" ;;
    Darwin) rid="osx-$arch"; ext=dylib; libs=""; shared="-dynamiclib" ;; # forkpty is in libc on macOS
    *) echo "Unsupported OS: $(uname -s)" >&2; exit 1 ;;
esac

out="$root/artifacts/native/$rid"
mkdir -p "$out"
cc -O2 -fPIC -Wall -Wextra -Werror $shared "$here/loomterm_pty.c" -o "$out/libloomterm_pty.$ext" $libs
echo "Built $out/libloomterm_pty.$ext"

if [ "${1:-}" = "--test" ]; then
    cc -O2 -Wall -Wextra -Werror -DLOOMTERM_TEST "$here/loomterm_pty.c" -o "$out/loomterm_pty_test" $libs
    "$out/loomterm_pty_test"
    rm -f "$out/loomterm_pty_test"
fi
