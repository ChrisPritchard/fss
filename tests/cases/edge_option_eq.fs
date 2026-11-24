let main () =
    print_bool (Some 1 = Some 1);
    print_newline ();
    print_bool (None = None);
    print_newline ();
    print_bool (Some 1 <> None);
    print_newline ()
