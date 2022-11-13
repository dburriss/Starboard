# Starboard

Starboard is a library providing strongly typed builders over kubernetes configuration. It allows you to retain a declarative style to your configuration while putting the full power of the F# programming language in your hands.

Starboard outputs plain Kubernetes YAML or JSON resource config files, so no need to change what you already have.

## Why?

Defining infrastructure as code can get complicated. Infrastructure code can run up to thousands of lines of configuration files. We then add layer upon layer of tools on top of this to manage the complexity while trying to keep our structured text correct and understandable.

The problems being solved by layers of tooling on top of mountains of yaml are the following:

- control flow
- sharing
- templating
- packaging

These are all problems that mature languages have solved decades ago. Instead of layering new tools on top of YAML files, what if we instead used a declarative programming language to define our configuration? Starboard enables this approach for Kubernetes.

## Feature summary

- A familiar declarative style
- The full power of a proper programming language (it's just F#, no magic)
- Built-in validations for quick feedback
- Outputs to Kubernetes YAML or JSON config
- Strongly typed for reduced mistakes
- Supported by your favourite IDE (it's just F# after all)
- Sane defaults for authentication and resource constraints

## Getting started

Show me the code! 

Of course. Say we have a file called `infra.fsx`.

```fsharp
// infra.fsx
// include the Starboard package from Nuget
#r "nuget:Starboard"

// open  the namespaces for the resources you need
open Starboard.Common
open Starboard.Workloads

// define the deployment Kubernetes resource
let theDeployment = k8s {
    deployment {
        "my-starboard-deployment"
        replicas 2
        add_matchLabel ("app", "nginx")
        pod {
            _labels [("app", "nginx")]
            container {
                name "nginx"
                image "nginx:latest"
            }
        }
    }
}

// write your YAML file to disk
KubeCtlWriter.toYamlFile theDeployment "deployment.yaml"
```

That's it! You now have a kubernetes config file for a deployment.

```bash
dotnet fsi infra.fsx
kubectl apply -f deployment.yaml
```

Ready to try it yourself? Try the hello-world tutorial. To explore more examples, check out the *How-to* or *Tutorials* section.

## Example use-cases

1. Template out repeat config as a function.
2. Fetch data from databases, APIs, environment variables, git, or wherever else to generate Kubernetes config.
3. Package up common infrastructure patterns and ship them to teams as Nuget packages.
4. Easily build CLI tools to output collections of Kubernetes config for development teams.
5. Let your imagination ship out.

## About this documentation

The docs follow the guidance from [The documentation system](https://documentation.divio.com/).

## About the logo

Ship by Aleksandr Vector from <a href="https://thenounproject.com/browse/icons/term/ship/" target="_blank" title="Ship Icons">Noun Project</a>

https://thenounproject.com/icon/ship-1016334/