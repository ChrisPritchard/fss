let f x = if x = 0 then "zero" elif x = 1 then "one" elif x = 2 then "two" else "many"
let main () =
    print_string (f 0);
    print_newline ();
    print_string (f 1);
    print_newline ();
    print_string (f 2);
    print_newline ();
    print_string (f 3);
    print_newline ()
