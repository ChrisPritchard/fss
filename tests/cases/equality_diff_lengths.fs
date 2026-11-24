let main () =
    (print_bool ([1; 2] = [1; 2; 3]); print_newline ();
     print_bool ([] = []); print_newline ();
     print_bool ([] = [1]); print_newline ())
