# OpenBase CLI — fish shell integration
# Copy to ~/.config/fish/functions/openbase.fish.
function openbase
    command openbase $argv
    set -l _exit $status
    if test $_exit -eq 0; and test (count $argv) -ge 1; and test $argv[1] = "new"
        if contains -- --json $argv
            return $_exit
        end
        set -l project_name ""
        set -l output ""
        for i in (seq (count $argv))
            set -l next (math $i + 1)
            if test $argv[$i] = "--name" -o $argv[$i] = "-n"
                if test $next -le (count $argv)
                    set project_name $argv[$next]
                end
            else if test $argv[$i] = "--output" -o $argv[$i] = "-o"
                if test $next -le (count $argv)
                    set output $argv[$next]
                end
            else if string match -q -- '--name=*' $argv[$i]
                set project_name (string replace -- '--name=' '' $argv[$i])
            else if string match -q -- '--output=*' $argv[$i]
                set output (string replace -- '--output=' '' $argv[$i])
            end
        end
        if test -z "$output"
            set output "$project_name"
        end
        if test -n "$output"; and test -d "$output"
            cd -- "$output"
        end
    end
    return $_exit
end
