// Top-level functions can be used as values (eta-expanded into closures).
let id x = x
let add a b = a + b

let twice f =
    f ()
    f ()

let applyTwice f x = f (f x)

let greet () = println "hi"

let main () =
    let g = id
    println (g 5)
    let h = add 10
    println (h 32)
    println (applyTwice (fun x -> x * 3) 2)
    let k = add
    println (k 1 2)
    twice greet
    let u = fun () -> println "lambda"
    u ()
    0
