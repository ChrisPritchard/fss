let main () =
    print_int (match (1, "x", true) with (a, b, c) -> a);
    print_newline ();
    print_string (match (1, "x", true) with (a, b, c) -> b);
    print_newline ();
    print_bool (match (1, "x", true) with (a, b, c) -> c);
    print_newline ()
