let rec max xs = match xs with | [] -> 0 | h :: t -> (let m = max t in if h > m then h else m)

let main () =
    print_int (max [3; 1; 4; 1; 5]); print_newline ()
