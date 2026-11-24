let main () =
    print_string (str_sub ("hello" + "world") 4 6);
    print_newline ();
    print_int (str_len (str_sub "abcdef" 2 3));
    print_newline ()
