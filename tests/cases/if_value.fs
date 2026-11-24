let main () =
    (print_int (if 1 < 2 then 10 else 20); print_newline ();
     print_int (if 1 > 2 then 10 else 20); print_newline ();
     print_bool (if true then (1 = 1) else (1 = 2)); print_newline ())
