# OpenBase CLI — fish shell integration
# After `openbase new`, automatically changes into the created project directory.
#
# Install:
#   cp openbase.fish ~/.config/fish/functions/openbase.fish
#
function openbase
    command openbase $argv
    set -l _exit $status
    if test $_exit -eq 0; and test (count $argv) -ge 1; and test $argv[1] = "new"
        for i in (seq (count $argv))
            if test $argv[$i] = "--name" -o $argv[$i] = "-n"
                set -l next (math $i + 1)
                if test $next -le (count $argv); and test -d $argv[$next]
                    cd $argv[$next]
                end
                break
            end
        end
    end
    return $_exit
end
