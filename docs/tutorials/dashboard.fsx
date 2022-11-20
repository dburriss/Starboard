(**
---
title: Kubernetes Dashboard
description: Setup the Kubernetes Dashboard
category: Tutorials
categoryindex: 3
index: 3
---
*)

(**
# Overboard: Kubernetes dashboard setup

In this tutorial we will setup the [official Kubernetes Dashboard](https://kubernetes.io/docs/tasks/access-application-cluster/web-ui-dashboard/). 
After installing the dashboard we will use Overboard to create a `ServiceAccount` and assign the necessary role. Then we will get an access token for that user.

## Topics covered

- Dashboard
- Authentication and Authorization resources

## Requirements

1. A [Kubernetes cluster to learn on](https://kubernetes.io/docs/tasks/tools/) such as Kubernetes on Docker or minikube.
1. A basic knowledge of using [kubectl](https://kubernetes.io/docs/reference/kubectl/)
1. [Ingress enabled](https://kubernetes.github.io/ingress-nginx/deploy/) for your clusterW
1. [dotnet SDK](https://dotnet.microsoft.com/en-us/) installed
1. Optionally, any IDE that supports F# (Visual Studio Code, IntelliJ Rider, Visual Studio, NeoVim)

> Visual Studio Code with the [Ionide](https://ionide.io/) is a great choice. See [Setup your environment](../setup-environment.fsx) for more details.

## Enable the Dashboard

To install the Dashboard into the kubernetes cluster you can run the following command. Check the [GitHub repository](https://github.com/kubernetes/dashboard#install) for the latest version.

```bash
kubectl apply -f https://raw.githubusercontent.com/kubernetes/dashboard/v2.7.0/aio/deploy/recommended.yaml
```

This creates about 14 different Kubernetes resources but the important ones for what we will be doing next are:

- A `Namespace` called _kubernetes-dashboard_
- A `ClusterRole` called _kubernetes-dashboard_ in the _kubernetes-dashboard_ `Namespace`

You can navigate to your [dashboard](http://localhost:8001/api/v1/namespaces/kubernetes-dashboard/services/https:kubernetes-dashboard:/proxy/) 
by going the the address [http://localhost:8001/api/v1/namespaces/kubernetes-dashboard/services/https:kubernetes-dashboard:/proxy/](http://localhost:8001/api/v1/namespaces/kubernetes-dashboard/services/https:kubernetes-dashboard:/proxy/).

![dashboard login](../img/tutorials/dashboard-login.png)

Remember to run `proxy` so you can access the cluster from your machine:

```bash
kubectl proxy
```

```bash
kubectl proxy
```

To login, we need to create a `ServiceAccount` and connect the correct role to it. We will do this and then get a token.

## Creating the Service Account

Next, we will create our fsx file and import the necessary packages and namespaces.
*)
//#r "nuget:Overboard"
#r "nuget:YamlDotNet"
#r """..\..\src\Overboard\bin\Debug\net6.0\Overboard.dll"""
// open the required namespaces
open System
open Overboard
open Overboard.Authentication
open Overboard.Authorization

(**

## Creating the ServiceAccount

The `ServiceAccount` is called _admin-user_ and is in the _kubernetes-dashboard_ `Namespace`.
*)
let dashboardAccount = serviceAccount {
    _name "admin-user"
    _namespace "kubernetes-dashboard"
}
(**
## Assign role to ServiceAccount

As mentioned above, the `ClusterRole` _kubernetes-dashboard_ is created when we install the dashboard. We need to connect it to the _admin-user_ `ServiceAccount`.
We do this with a `ClusterRoleBinding`.

*)
let dashboardRoleBinding = clusterRoleBinding {
    _name "admin-user"
    roleRef {
        apiGroup "rbac.authorization.k8s.io"
        kind "ClusterRole"
        name "cluster-admin"
    }
    subject {
        kind "ServiceAccount"
        name "admin-user"
        ns "kubernetes-dashboard"
    }
}

(**
Finally, we add this to a `K8s` config and write the results out to a _dashboard.yaml_ file.
*)

let dashboardConfig = k8s {
    dashboardAccount
    dashboardRoleBinding
}

let configDir = __SOURCE_DIRECTORY__
KubeCtlWriter.toYamlFile dashboardConfig $"{configDir}{IO.Path.DirectorySeparatorChar}dashboard.yaml"

(**
Run the fsx file and then apply the config to your cluster you created to 

```bash
dotnet fsi .\dashboard.fsx
kubectl apply -f .\dashboard.yaml
```

Use the command for your terminal on your operating system to generate a token. 
Copying the token manually can cause issues (newlines etc.) so I recommend putting the token directly into your clipboard like shown below.

Windows cmd:
```powershell
kubectl -n kubernetes-dashboard create token admin-user | clip
```
Powershell:
```powershell
kubectl -n kubernetes-dashboard create token admin-user | Set-Clipboard
```
Mac:
```bash
kubectl -n kubernetes-dashboard create token admin-user | pbcopy
```
Linux (with xcopy installed):
```bash
kubectl -n kubernetes-dashboard create token admin-user | xcopy
```

Remember to start a proxy if you don't still have it running from earlier.

Don't copy/paste this command since the token is in your clipboard ;)
```bash
kubectl proxy
```

Now, navigate to the dashboard again, make sure **Token** is selected, and paste your token in the text field.

[kubnernetes dashboard](http://localhost:8001/api/v1/namespaces/kubernetes-dashboard/services/https:kubernetes-dashboard:/proxy/)

You should now have access to your cluster dashboard.

Congrats!

# Conclusion

In this tutorial we saw how to easily install the Kubernetes dashboard and create a `ServiceAccount` and a `ClusterRoleBinding` to access it. 
We used this `ServiceAccount` to access the dashboard with a generated token.

*)
