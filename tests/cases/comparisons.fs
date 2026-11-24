let main () =
    (print_bool (1 < 2); print_newline ();
     print_bool (1 > 2); print_newline ();
     print_bool (2 <= 2); print_newline ();
     print_bool (2 >= 3); print_newline ();
     print_bool (1 = 1); print_newline ();
     print_bool (1 <> 2); print_newline ();
     print_bool (true = true); print_newline ();
     print_bool (true <> false); print_newline ())
