let id = fun x -> x
let hello = id "hi"
let flag = id true
let double = fun x -> x * 2
let rec fact = fun n -> if n = 0 then 1 else n * fact (n - 1)

double (id 21) + fact (id 5)
