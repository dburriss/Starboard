(**
---
title: Setup your environment
category: How-to
categoryindex: 2
index: 1
---
*)

(**
# Setup F# scripting

## Install dotnet SDK

To work with Overboard you will need to be able to create, edit, and run F# script files (.fsx). This page will give you a quick-start on how to get setup as well as links to resources for further reading if you would like to deepen your knowledge.
Running fsx files requires FSharp Interactive (FSI) which is bundled as part of the [dotnet SDK](https://dotnet.microsoft.com/en-us/download).

That is it! You can now run F# script files.

To enter FSI: `dotnet fsi`

To run a script: `dotnet fsi /path/to/script.fsx [optional argument]`

## Setup an editor

If you are using VS Code, you can install the [Ionide plugin](https://ionide.io/Editors/Code/getting_started.html) to get get you going. Just search for Ionide on the VS Code extensions marketplace. Alternatively you can use Visual Studio, Rider, or NeoVim (with the [Ionide plugin](https://ionide.io/Editors/Vim/overview.html)).async

Ionide has a handy run button for executing your script right from the IDE.

## Use Overboard

To get started using Overboard, all you need to do is include the following line in a fsx file:

`#r "nuget: Overboard"`
*)
