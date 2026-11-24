type T = S of int | Empty
let f t = match t with S n -> n | Empty -> 0
let main () =
    print_int (f (S 42));
    print_newline ();
    print_int (f Empty);
    print_newline ()
