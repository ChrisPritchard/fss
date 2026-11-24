type P = { X: int; Y: int }
let p = { X = 3; Y = 4 }
let main () =
    print_int (p.X + p.Y);
    print_newline ()
