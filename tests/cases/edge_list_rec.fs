let rec length xs = match xs with [] -> 0 | _ :: t -> 1 + length t
let rec sum xs = match xs with [] -> 0 | h :: t -> h + sum t
let main () =
    print_int (length [1; 2; 3; 4]);
    print_newline ();
    print_int (sum [1; 2; 3; 4]);
    print_newline ()
