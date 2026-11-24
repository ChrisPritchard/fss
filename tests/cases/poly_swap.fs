let swap p = match p with | (a, b) -> (b, a)

let main () =
    (print_bool (swap (1, true) = (true, 1)); print_newline ();
     print_bool (swap (false, 2) = (2, false)); print_newline ())
