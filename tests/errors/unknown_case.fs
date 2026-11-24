type Shape = Circle of int

let main () = print_int (match (Circle 3) with | Square x -> x)
