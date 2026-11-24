let f x = if x > 0 then (if x > 10 then "big" else "small") else "neg"
let main () =
    print_string (f 5);
    print_newline ();
    print_string (f 15);
    print_newline ();
    print_string (f (-1));
    print_newline ()
