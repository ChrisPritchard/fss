let main () =
    print_bool ((1, "a") = (1, "a"));
    print_newline ();
    print_bool ((1, "a") <> (2, "a"));
    print_newline ()
