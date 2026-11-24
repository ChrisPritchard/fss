let main () =
    (print_int (if 1 < 2 then (if 2 < 3 then 1 else 2) else 3); print_newline ();
     print_int (if 1 > 2 then 1 else (if 2 > 3 then 2 else 3)); print_newline ())
