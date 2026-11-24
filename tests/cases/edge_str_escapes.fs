let main () =
    print_int (str_len "a\nb");
    print_newline ();
    print_int (str_len "\t");
    print_newline ();
    print_int (str_len "\"");
    print_newline ();
    print_int (str_len "\\");
    print_newline ()
