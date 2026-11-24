let rec even n = if n = 0 then true else odd (n - 1)
and odd n = if n = 0 then false else even (n - 1)

let main () =
    (print_bool (even 10); print_newline ();
     print_bool (even 7); print_newline ();
     print_bool (odd 7); print_newline ())
