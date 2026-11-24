let rec sum xs = match xs with | [] -> 0 | h :: t -> h + sum t

let main () =
    print_int (sum [1; 2; 3; 4; 5; 6; 7; 8; 9; 10]); print_newline ()
