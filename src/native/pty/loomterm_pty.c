// libloomterm_pty: starts a process under a pseudo terminal (ADR 0007).
// fork() is unsafe from a running .NET process, so the fork happens here and the child does nothing but
// chdir + execve: no malloc, no stdio. Resize (TIOCSWINSZ) and wait (waitpid) are done from C#.
#define _GNU_SOURCE
#include <stddef.h>
#include <sys/ioctl.h>
#include <sys/types.h>
#include <unistd.h>
#if defined(__APPLE__)
#include <util.h>
#else
#include <pty.h>
#endif

#define EXPORT __attribute__((visibility("default")))

// Returns 0 on success (masterFd and pid are set), -1 when forkpty fails (errno is set).
// The child exits with 126 when chdir(cwd) fails and 127 when execve fails.
EXPORT int loomterm_spawn(const char* file, char* const argv[], char* const envp[], const char* cwd,
                          int cols, int rows, int* masterFd, int* pid)
{
    struct winsize size = { (unsigned short)rows, (unsigned short)cols, 0, 0 };
    int master;
    pid_t child = forkpty(&master, NULL, NULL, &size);
    if (child < 0)
    {
        return -1;
    }

    if (child == 0)
    {
        if (cwd != NULL && chdir(cwd) != 0)
        {
            _exit(126);
        }

        execve(file, argv, envp);
        _exit(127);
    }

    *masterFd = master;
    *pid = (int)child;
    return 0;
}

#ifdef LOOMTERM_TEST
#include <stdio.h>
#include <string.h>

// Smoke test (build.sh --test): spawn /bin/echo hi and read "hi" from the master.
int main(void)
{
    char* argv[] = { "/bin/echo", "hi", NULL };
    char* envp[] = { NULL };
    int master = -1, pid = -1;
    if (loomterm_spawn("/bin/echo", argv, envp, NULL, 80, 24, &master, &pid) != 0)
    {
        perror("loomterm_spawn");
        return 1;
    }

    char buffer[64] = { 0 };
    size_t used = 0;
    ssize_t n;
    while (used < sizeof(buffer) - 1 && (n = read(master, buffer + used, sizeof(buffer) - 1 - used)) > 0)
    {
        used += (size_t)n;
    }

    if (strstr(buffer, "hi") == NULL)
    {
        fprintf(stderr, "expected \"hi\", got \"%s\"\n", buffer);
        return 1;
    }

    puts("ok");
    return 0;
}
#endif
