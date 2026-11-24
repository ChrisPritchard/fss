let main () =
    print_bool ([1; 2] = [1; 2]);
    print_newline ();
    print_bool ([1; 2] <> [2; 1]);
    print_newline ();
    print_bool ([] = []);
    print_newline ()
