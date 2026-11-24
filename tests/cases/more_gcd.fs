let rec gcd a b = if b = 0 then a else gcd b (a % b)
let main () =
    print_int (gcd 48 36);
    print_newline ();
    print_int (gcd 17 5);
    print_newline ();
    print_int (gcd 100 10);
    print_newline ()
