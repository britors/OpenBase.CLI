# OpenBase CLI — bash/zsh shell integration
# After `openbase new`, automatically changes into the created project directory.
#
# Install (bash):  add to ~/.bashrc
# Install (zsh):   add to ~/.zshrc
#
openbase() {
    command openbase "$@"
    local _exit=$?
    if [ $_exit -eq 0 ] && [ "${1:-}" = "new" ]; then
        local i=1 name=""
        while [ $i -le $# ]; do
            local arg
            arg=$(eval "echo \${$i}")
            if [ "$arg" = "--name" ] || [ "$arg" = "-n" ]; then
                i=$((i + 1))
                name=$(eval "echo \${$i}")
                break
            fi
            i=$((i + 1))
        done
        if [ -n "$name" ] && [ -d "$name" ]; then
            cd "$name" || true
        fi
    fi
    return $_exit
}
