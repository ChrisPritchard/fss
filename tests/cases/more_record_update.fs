type P = { X: int; Y: int }
let p = { X = 1; Y = 2 }
let q = { X = p.X + 10; Y = p.Y + 20 }
let main () =
    print_int (q.X + q.Y);
    print_newline ()
