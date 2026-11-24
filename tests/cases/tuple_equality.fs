let main () =
    (print_bool ((1, 2) = (1, 2)); print_newline ();
     print_bool ((1, 2) = (1, 3)); print_newline ();
     print_bool ((1, (2, 3)) = (1, (2, 3))); print_newline ())
