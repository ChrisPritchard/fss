let rec sum xs = match xs with | [] -> 0 | h :: t -> h + sum t

let main () =
    print_int (sum (1 :: 2 :: 3 :: [])); print_newline ();
    print_int (sum [4; 5; 6]); print_newline ();
    print_bool ([1; 2] = [1; 2]); print_newline ()
