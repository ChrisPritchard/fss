let rec countList xs acc = match xs with | [] -> acc | h :: t -> countList t (acc + 1)

let main () =
    print_int (countList [1; 2; 3; 4; 5; 6; 7; 8; 9; 10] 0); print_newline ()
