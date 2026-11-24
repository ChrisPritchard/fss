let main () =
    print_string (str_sub "hello" 0 5);
    print_newline ();
    print_string (str_sub "hello" 5 0);
    print_newline ();
    print_string (str_sub "hello" 0 0);
    print_newline ()
