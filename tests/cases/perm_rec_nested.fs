let rec f n = if n = 0 then 1 else g (n - 1)
and g n = if n = 0 then 1 else f (n - 1) + 1
let main () =
    print_int (f 5);
    print_newline ();
    print_int (g 5);
    print_newline ()
