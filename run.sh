#!/bin/bash

while true; do
    dotnet run --no-restore --no-dependencies
    n=$?
    echo exit code: $n
    if test "$n" -ne 0; then
        exit
    fi
done
