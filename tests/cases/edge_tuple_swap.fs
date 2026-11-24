let swap p = match p with (a, b) -> (b, a)
let main () =
    print_int (match swap (1, 2) with (a, b) -> a);
    print_newline ();
    print_int (match swap (1, 2) with (a, b) -> b);
    print_newline ()
