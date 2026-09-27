# OpenBase CLI — bash/zsh shell integration
# Source this file from ~/.bashrc or ~/.zshrc.
openbase() {
    command openbase "$@"
    local _exit=$?
    if [ "$_exit" -eq 0 ] && [ "${1:-}" = "new" ]; then
        local name="" output=""
        while [ "$#" -gt 0 ]; do
            case "$1" in
                -n|--name) [ "$#" -ge 2 ] || break; name=$2; shift ;;
                -o|--output) [ "$#" -ge 2 ] || break; output=$2; shift ;;
                --name=*) name=${1#*=} ;;
                --output=*) output=${1#*=} ;;
                --json) return "$_exit" ;;
            esac
            shift
        done
        local destination=${output:-$name}
        if [ -n "$destination" ] && [ -d "$destination" ]; then
            cd -- "$destination" || return 1
        fi
    fi
    return "$_exit"
}
