let main () =
    print_bool (str_len "abc" = 3);
    print_newline ();
    print_bool (str_len "" = 0);
    print_newline ();
    print_bool (str_len "a" <> 2);
    print_newline ()
