let rec length xs = match xs with | [] -> 0 | h :: t -> 1 + length t

let main () =
    (print_int (length []); print_newline ();
     print_int (length [1]); print_newline ();
     print_int (length [1; 2; 3; 4; 5]); print_newline ())
