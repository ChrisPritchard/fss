let main () =
    (print_string "Hello, "; print_string "world"; print_newline ();
     print_string ("foo" + "bar"); print_newline ();
     print_string ("" + "x"); print_newline ();
     print_bool ("abc" = "abc"); print_newline ();
     print_bool ("abc" = "abd"); print_newline ();
     print_bool ("" = ""); print_newline ();
     print_int (str_len "hello"); print_newline ();
     print_int (str_len ""); print_newline ())
