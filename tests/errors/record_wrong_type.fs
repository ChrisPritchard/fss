type P = { X: int }

let main () = print_bool ({ X = true } = { X = false })
