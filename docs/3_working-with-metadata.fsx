(**
---
title: Working with metadata
category: How-to
categoryindex: 2
index: 3
---
*)

(**
# Working with metadata

Setting metadata like `name`, `namespace`, `labels`, and `annotations` are a common part of defining a Kubernetes resource.
*)

(*** hide ***)
// import from Nuget
#r "nuget:YamlDotNet"
#r "nuget:Newtonsoft.Json"
#r "../src/Overboard/bin/debug/net6.0/Overboard.dll"

// open the required namespaces

open Overboard
open Overboard.Common
open Overboard.Workload

(*** show ***)
// Using an inline metadata builder
k8s {
    deployment {
        metadata {
            name "my-name"
            ns "my-namespace" // `ns` used since namespace is a keyword in F#
            labels ["label1", "value1"]
            annotations ["annotation-key", "annotation-value"]
        }
    }
}

(**
It is of course always possible to pass in a variable instead.
*)
// Assign the builder result to a variable first
let meta = metadata {
            name "my-name"
            ns "my-namespace"
            labels ["label1", "value1"]
            annotations ["annotation-key", "annotation-value"]
        }
k8s {
    deployment {
        set_metadata meta
    }
}

(**
A **convention** that is enabled is that you can set the metadata on a resource using any of the following operations `_name`, `_namespace`, `_labels`, and `_annotations`
*)

// use the _ convention
k8s {
    deployment {
        _name "my-name"
        _namespace "my-namespace"
        _labels ["label1", "value1"]
        _annotations ["annotation-key", "annotation-value"]
    }
}

(**
Another **convention** is that instead of using `_name` you can drop a string into the body of most resources and it will be assigned to the `metadata.name` property. This also works for some data that does not have metadata but has a name property.
*)

// use the _ convention
k8s {
    deployment {
        "my-name" // <- No operation specified
        _labels ["label1", "value1"]
    }
}

(**
One last neat trick for metadata and many other resources is that you can just drop the variable into the body of the builder. For this to work, the type of the variable must be unique to a single field on the builder.
*)

let deploymentMetadata = 
        metadata {
            name "my-name"
            ns "my-namespace"
            labels ["label1", "value1"]
            annotations ["annotation-key", "annotation-value"]
        }

// use the _ convention
k8s {
    deployment {
        deploymentMetadata // <- just drop the variable in the resource expression
    }
}

(**
So there are many ways to define your metadata. I suggest you decide for one on your project and use that method throughout.
*)