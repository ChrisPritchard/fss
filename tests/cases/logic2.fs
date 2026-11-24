let main () =
    (print_bool (not true); print_newline ();
     print_bool (not false); print_newline ();
     print_bool (false && false); print_newline ();
     print_bool (false && true); print_newline ();
     print_bool (true && true); print_newline ();
     print_bool (false || false); print_newline ();
     print_bool (true || false); print_newline ();
     print_bool ((1 < 2) && (2 < 3) || false); print_newline ())
