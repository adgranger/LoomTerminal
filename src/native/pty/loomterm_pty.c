// libloomterm_pty: starts a process under a pseudo terminal (ADR 0007).
// fork() is unsafe from a running .NET process, so the fork happens here and the child does nothing but
// chdir + execve: no malloc, no stdio. Resize, wait and kill are small wrappers so that C# never calls the variadic
// ioctl (broken through P/Invoke on Apple arm64) nor depends on how "libc" resolves.
#define _GNU_SOURCE
#include <errno.h>
#include <signal.h>
#include <pwd.h>
#include <stddef.h>
#include <string.h>
#include <sys/ioctl.h>
#include <sys/types.h>
#include <sys/wait.h>
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

// Sets the size of the pty. Returns 0, or -1 (errno is set).
EXPORT int loomterm_resize(int masterFd, int cols, int rows)
{
    struct winsize size = { (unsigned short)rows, (unsigned short)cols, 0, 0 };
    return ioctl(masterFd, TIOCSWINSZ, &size);
}

// Blocks until the child ends. Returns its exit code, 128 + signal when it was killed, or -1.
EXPORT int loomterm_wait(int pid)
{
    int status = 0;
    pid_t result;
    do
    {
        result = waitpid((pid_t)pid, &status, 0);
    } while (result < 0 && errno == EINTR);

    if (result < 0)
    {
        return -1;
    }

    if (WIFEXITED(status))
    {
        return WEXITSTATUS(status);
    }

    return WIFSIGNALED(status) ? 128 + WTERMSIG(status) : -1;
}

// Sends a signal to the whole process group of the child (forkpty made it a session leader: group id == pid).
EXPORT int loomterm_kill_group(int pid, int signal)
{
    return kill(-(pid_t)pid, signal);
}

// Copies the login shell of the current user (passwd database) into buffer. Returns its length, or -1.
EXPORT int loomterm_login_shell(char* buffer, int size)
{
    struct passwd entry, *found = NULL;
    char scratch[1024];
    if (getpwuid_r(getuid(), &entry, scratch, sizeof(scratch), &found) != 0 || found == NULL || entry.pw_shell == NULL)
    {
        return -1;
    }

    size_t length = strlen(entry.pw_shell);
    if (length >= (size_t)size)
    {
        return -1;
    }

    memcpy(buffer, entry.pw_shell, length + 1);
    return (int)length;
}

#ifdef LOOMTERM_TEST
#include <stdio.h>

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
