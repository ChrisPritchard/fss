let main () =
    print_bool (Some (1, "a") = Some (1, "a"));
    print_newline ();
    print_bool (None = None);
    print_newline ()
