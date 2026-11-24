let main () =
    print_bool (true && false || true);
    print_newline ();
    print_bool (true || false && false);
    print_newline ();
    print_bool (not (false && true));
    print_newline ()
