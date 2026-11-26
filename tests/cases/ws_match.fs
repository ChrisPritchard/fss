let describe n =
    match n with
    | 0 -> "zero"
    | 1 -> "one"
    | _ -> "many"

let main () =
    print_string (describe 1)
    print_newline ()
    print_string (describe 5)
    print_newline ()
