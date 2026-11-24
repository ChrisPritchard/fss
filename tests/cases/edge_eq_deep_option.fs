let main () =
    print_bool (Some [1; 2] = Some [1; 2]);
    print_newline ();
    print_bool (Some [1] <> Some [2]);
    print_newline ()
