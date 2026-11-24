let rec length xs = match xs with | [] -> 0 | h :: t -> 1 + length t

let main argv =
    (print_int (length argv); print_newline ();
     print_bool (0 < length argv); print_newline ())
