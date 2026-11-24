let main () =
    print_int (str_len "hello");
    print_newline ();
    print_int (str_get "abc" 0);
    print_newline ();
    print_string (str_sub "hello" 1 3);
    print_newline ();
    print_int (int_of_string "42");
    print_newline ();
    print_string (int_to_string 42);
    print_newline ()
