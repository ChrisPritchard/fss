let main () =
    print_bool (1 < 2 && 3 < 4);
    print_newline ();
    print_bool (1 < 2 || 3 > 4);
    print_newline ();
    print_bool (not (1 = 2) && not (2 = 3));
    print_newline ()
