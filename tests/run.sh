#!/usr/bin/env bash
# Build the compiler once, then run every positive and negative test.
set -euo pipefail
cd "$(dirname "$0")/.."

dotnet build src/Fss.fsproj -c Release -v q >/dev/null

# Use the freshly-built managed compiler for tests (the AOT binary under
# build/aot is a manual optimization and can be stale).
FSS_CMD=(dotnet src/bin/Release/net9.0/fss.dll)

rm -rf build/cases
mkdir -p build/cases

pass=0
fail=0

# Positive cases: compile, run, compare output.
for src in tests/cases/*.fs; do
    name="$(basename "$src" .fs)"
    out="build/cases/$name"

    if ! "${FSS_CMD[@]}" build "$src" -o "$out" >/dev/null 2>build/cases/"$name".cerr; then
        echo "FAIL  $name (compile)"
        cat build/cases/"$name".cerr
        fail=$((fail + 1))
        continue
    fi

    bin="$out"
    [ -f "$out.exe" ] && bin="$out.exe"
    if [ -f "tests/cases/$name.stdin" ]; then
        "$bin" < "tests/cases/$name.stdin" | tr -d '\r' > build/cases/"$name".actual
    else
        "$bin" | tr -d '\r' > build/cases/"$name".actual
    fi

    if diff -u "tests/cases/$name.expected" build/cases/"$name".actual >/dev/null; then
        echo "PASS  $name"
        pass=$((pass + 1))
    else
        echo "FAIL  $name (output)"
        diff -u "tests/cases/$name.expected" build/cases/"$name".actual || true
        fail=$((fail + 1))
    fi
done

# Multi-file cases: a .multi file lists source paths on one line.
for mfile in tests/cases/*.multi; do
    name="$(basename "$mfile" .multi)"
    out="build/cases/$name"
    srcs="$(cat "$mfile")"
    if ! "${FSS_CMD[@]}" build $srcs -o "$out" >/dev/null 2>build/cases/"$name".cerr; then
        echo "FAIL  $name (compile)"
        cat build/cases/"$name".cerr
        fail=$((fail + 1))
        continue
    fi
    bin="$out"
    [ -f "$out.exe" ] && bin="$out.exe"
    "$bin" | tr -d '\r' > build/cases/"$name".actual
    if diff -u "tests/cases/$name.expected" build/cases/"$name".actual >/dev/null; then
        echo "PASS  $name"
        pass=$((pass + 1))
    else
        echo "FAIL  $name (output)"
        diff -u "tests/cases/$name.expected" build/cases/"$name".actual || true
        fail=$((fail + 1))
    fi
done

# Bootstrap check: the lexer is written in fss itself and compiled by fss.
if "${FSS_CMD[@]}" build bootstrap/lexer.fs -o build/cases/bootstrap_lexer >/dev/null 2>build/cases/bootstrap_lexer.cerr; then
    bin=build/cases/bootstrap_lexer
    [ -f build/cases/bootstrap_lexer.exe ] && bin=build/cases/bootstrap_lexer.exe
    "$bin" tests/sample.fs | tr -d '\r' > build/cases/bootstrap_lexer.actual
    if diff -u bootstrap/lexer.expected build/cases/bootstrap_lexer.actual >/dev/null; then
        echo "PASS  bootstrap/lexer"
        pass=$((pass + 1))
    else
        echo "FAIL  bootstrap/lexer (output)"
        diff -u bootstrap/lexer.expected build/cases/bootstrap_lexer.actual || true
        fail=$((fail + 1))
    fi
else
    echo "FAIL  bootstrap/lexer (compile)"
    cat build/cases/bootstrap_lexer.cerr
    fail=$((fail + 1))
fi

# Bootstrap parser: parses an expression and prints an S-expression.
if "${FSS_CMD[@]}" build bootstrap/parser.fs -o build/cases/bootstrap_parser >/dev/null 2>build/cases/bootstrap_parser.cerr; then
    bin=build/cases/bootstrap_parser
    [ -f build/cases/bootstrap_parser.exe ] && bin=build/cases/bootstrap_parser.exe
    "$bin" bootstrap/input.fs | tr -d '\r' > build/cases/bootstrap_parser.actual
    if diff -u bootstrap/parser.expected build/cases/bootstrap_parser.actual >/dev/null; then
        echo "PASS  bootstrap/parser"
        pass=$((pass + 1))
    else
        echo "FAIL  bootstrap/parser (output)"
        diff -u bootstrap/parser.expected build/cases/bootstrap_parser.actual || true
        fail=$((fail + 1))
    fi
else
    echo "FAIL  bootstrap/parser (compile)"
    cat build/cases/bootstrap_parser.cerr
    fail=$((fail + 1))
fi

# Bootstrap type checker: written in fss, compiled by fss.
if "${FSS_CMD[@]}" build bootstrap/typer.fs -o build/cases/bootstrap_typer >/dev/null 2>build/cases/bootstrap_typer.cerr; then
    bin=build/cases/bootstrap_typer
    [ -f build/cases/bootstrap_typer.exe ] && bin=build/cases/bootstrap_typer.exe
    "$bin" bootstrap/type_input.fs | tr -d '\r' > build/cases/bootstrap_typer.actual
    if diff -u bootstrap/typer.expected build/cases/bootstrap_typer.actual >/dev/null; then
        echo "PASS  bootstrap/typer"
        pass=$((pass + 1))
    else
        echo "FAIL  bootstrap/typer (output)"
        diff -u bootstrap/typer.expected build/cases/bootstrap_typer.actual || true
        fail=$((fail + 1))
    fi
else
    echo "FAIL  bootstrap/typer (compile)"
    cat build/cases/bootstrap_typer.cerr
    fail=$((fail + 1))
fi

# Bootstrap compiler: fss-written compiler emits C, which we compile and run.
if "${FSS_CMD[@]}" build bootstrap/compiler.fs -o build/cases/bootstrap_compiler >/dev/null 2>build/cases/bootstrap_compiler.cerr; then
    bbin=build/cases/bootstrap_compiler
    [ -f build/cases/bootstrap_compiler.exe ] && bbin=build/cases/bootstrap_compiler.exe
    "$bbin" bootstrap/compiler_input.fs > build/cases/bootstrap_compiler.c 2>build/cases/bootstrap_compiler.err
    if cc -O1 build/cases/bootstrap_compiler.c -o build/cases/bootstrap_compiler_out >/dev/null 2>&1; then
        obin=build/cases/bootstrap_compiler_out
        [ -f build/cases/bootstrap_compiler_out.exe ] && obin=build/cases/bootstrap_compiler_out.exe
        "$obin" | tr -d '\r' > build/cases/bootstrap_compiler.actual
        if diff -u bootstrap/compiler.expected build/cases/bootstrap_compiler.actual >/dev/null; then
            echo "PASS  bootstrap/compiler"
            pass=$((pass + 1))
        else
            echo "FAIL  bootstrap/compiler (output)"
            diff -u bootstrap/compiler.expected build/cases/bootstrap_compiler.actual || true
            fail=$((fail + 1))
        fi
    else
        echo "FAIL  bootstrap/compiler (C compile)"
        fail=$((fail + 1))
    fi
else
    echo "FAIL  bootstrap/compiler (fss compile)"
    cat build/cases/bootstrap_compiler.cerr
    fail=$((fail + 1))
fi

# Self-hosting check: the self-hosted compiler (built above) compiles the
# earlier bootstrap stages to native binaries; their output must match the
# .NET-built versions.
if [ -x build/cases/bootstrap_compiler ] || [ -x build/cases/bootstrap_compiler.exe ]; then
    sbin=build/cases/bootstrap_compiler
    [ -f build/cases/bootstrap_compiler.exe ] && sbin=build/cases/bootstrap_compiler.exe
    for stage in lexer parser typer; do
        if "$sbin" "bootstrap/$stage.fs" > build/cases/self_$stage.c 2>build/cases/self_$stage.err \
            && cc -O1 build/cases/self_$stage.c -o build/cases/self_$stage >/dev/null 2>&1; then
            b=build/cases/self_$stage
            [ -f build/cases/self_$stage.exe ] && b=build/cases/self_$stage.exe
            if [ "$stage" = lexer ]; then input=tests/sample.fs
            elif [ "$stage" = parser ]; then input=bootstrap/input.fs
            else input=bootstrap/type_input.fs; fi
            "$b" "$input" | tr -d '\r' > build/cases/self_$stage.actual
            if diff -u "bootstrap/$stage.expected" build/cases/self_$stage.actual >/dev/null; then
                echo "PASS  selfhost/$stage"
                pass=$((pass + 1))
            else
                echo "FAIL  selfhost/$stage (output)"
                diff -u "bootstrap/$stage.expected" build/cases/self_$stage.actual || true
                fail=$((fail + 1))
            fi
        else
            echo "FAIL  selfhost/$stage (compile)"
            head -3 build/cases/self_$stage.err
            fail=$((fail + 1))
        fi
    done

    # The ultimate self-hosting check: the self-hosted compiler compiles
    # itself, and the resulting binary compiles the bootstrap input.
    if "$sbin" bootstrap/compiler.fs > build/cases/self_compiler.c 2>build/cases/self_compiler.err \
        && cc -O1 -Wl,--stack,67108864 build/cases/self_compiler.c -o build/cases/self_compiler >/dev/null 2>&1; then
        cb=build/cases/self_compiler
        [ -f build/cases/self_compiler.exe ] && cb=build/cases/self_compiler.exe
        if "$cb" bootstrap/compiler_input.fs > build/cases/self_compiler2.c 2>build/cases/self_compiler2.err \
            && cc -O1 build/cases/self_compiler2.c -o build/cases/self_compiler2 >/dev/null 2>&1; then
            c2=build/cases/self_compiler2
            [ -f build/cases/self_compiler2.exe ] && c2=build/cases/self_compiler2.exe
            got="$("$c2" | tr -d '\r')"
            if [ "$got" = "$(cat bootstrap/compiler.expected)" ]; then
                echo "PASS  selfhost/compiler"
                pass=$((pass + 1))
            else
                echo "FAIL  selfhost/compiler (output: $got)"
                fail=$((fail + 1))
            fi
        else
            echo "FAIL  selfhost/compiler (round-2 compile)"
            head -3 build/cases/self_compiler2.err
            fail=$((fail + 1))
        fi
    else
        echo "FAIL  selfhost/compiler (self compile)"
        head -3 build/cases/self_compiler.err
        fail=$((fail + 1))
    fi
fi

# Negative cases: must fail with a type error or a clear error message.
for src in tests/errors/*.fs; do
    name="$(basename "$src" .fs)"
    log=build/cases/err_"$name".log

    if "${FSS_CMD[@]}" build "$src" -o build/cases/err_"$name" >"$log" 2>&1; then
        echo "FAIL  error/$name (expected failure, but it compiled)"
        fail=$((fail + 1))
    elif grep -qE "type error|error:" "$log"; then
        echo "PASS  error/$name"
        pass=$((pass + 1))
    else
        echo "FAIL  error/$name (unexpected message)"
        cat "$log"
        fail=$((fail + 1))
    fi
done

echo
echo "$pass passed, $fail failed"
[ "$fail" -eq 0 ]
