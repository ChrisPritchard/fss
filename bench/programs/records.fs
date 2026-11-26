// Record-heavy: create + update records in a loop, read fields back.
type Point = { X: int; Y: int }
type Pair = { A: Point; B: Point }

let shift (p: Point) (dx: int) (dy: int) = { p with X = p.X + dx; Y = p.Y + dy }

let dist2 (p: Point) (q: Point) =
    let dx = p.X - q.X
    let dy = p.Y - q.Y
    dx * dx + dy * dy

[<EntryPoint>]
let main _ =
    let origin = { X = 0; Y = 0 }
    let mutable acc = 0
    let mutable i = 0
    while i < 2000000 do
        let p = { X = i; Y = i * 2 }
        let q = shift p 1 1
        let r = { A = p; B = q }
        acc <- acc + dist2 r.A r.B
        i <- i + 1
    System.Console.WriteLine(acc)
    0
