#r "../src/Overboard/bin/debug/net6.0/Overboard.dll"

open Overboard.Resources

// Example of pod using metadata builder
let pod1 = pod {
    metadata {
        name "pod-test"
        ns "test"
    }
    container {
        workingDir "/test-dir"
    }
}

// Example of using `_` shortcut for setting metadata
let pod2 = pod {
    _name "pod-test"
    _namespace "test"

    container {
        workingDir "/test-dir"
    }
}

// Example of free string is always used as the metadata name
let pod3 = pod {
    "pod-test"
    _namespace "test"
    container {
        workingDir "/test-dir"
    }
}

// Example of yielding a metadata variable in the pod
let pod4Metadata = metadata {
    name "pod-test"
    ns "test"
}

let pod4 = pod {
    pod4Metadata
    container {
        workingDir "/test-dir"
    }
}

// Example of explicitly setting the metadata variable
let pod5Metadata = metadata {
    name "pod-test"
    ns "test"
}

let pod5 = pod {
    set_metadata pod5Metadata
    container {
        workingDir "/test-dir"
    }
}