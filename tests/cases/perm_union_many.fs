type T = A | B | C | D | E
let f t = match t with A -> 1 | B -> 2 | C -> 3 | D -> 4 | E -> 5
let main () =
    print_int (f A + f B + f C + f D + f E);
    print_newline ()
