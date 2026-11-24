let empty = []

let rec length xs = match xs with | [] -> 0 | h :: t -> 1 + length t

let main () =
    (print_int (length (1 :: empty)); print_newline ();
     print_int (length (true :: empty)); print_newline ())
