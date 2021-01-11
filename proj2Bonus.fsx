#r "nuget: Akka.FSharp"
#r "nuget: Akka.TestKit"

open System
open Akka.Actor
open Akka.FSharp
open Akka.TestKit
open System.Collections.Generic

let system1 = ActorSystem.Create("FSharp")

let numNodes = int fsi.CommandLineArgs.[1]
let topology = fsi.CommandLineArgs.[2]
let algorithm = fsi.CommandLineArgs.[3]
let nodesToKill = int fsi.CommandLineArgs.[4]

type ReturnOfReceive =
    | StartGossipMaster of int * string * string
    | CallGossipNode of int * string
    | EndGossipNode of string * int
    | DoneGossipMaster of string
    | CallPushSumNode of int * float * float
    | EndPushSumNode of string * int * float * float
    | EndPushSumMaster of string * int * float

type SampleTuple = float * float


let myMap = new Dictionary<int, List<int>>()
let rand = Random(1234)
let mutable randIndex = rand.Next()
let nodeRatio = new List<float>()
let mutable globalCounter: int = 0
let deadNodes = new List<int>()

for i in 0 .. numNodes - 1 do
    nodeRatio.Add(float 0)

let mutable closingFlag = false

let Node id (mailbox: Actor<_>) =

    let rec nodeLoop (nodeCount: int)
                     (pushsumFlag: bool)
                     (nodeSum: float)
                     (nodeWeight: float)
                     (endCounter: int)
                     (prevRatio: float)
                     ()
                     =
        actor {
            let! returnOfReceive = mailbox.Receive()
            let mutable updatedSum = nodeSum
            let mutable updatedWeight = nodeWeight
            let mutable updatedflag = pushsumFlag
            let mutable updatedCounter = endCounter

            let mutable updatedPrevRatio = prevRatio

            match returnOfReceive with
            | CallGossipNode (index, message) ->
                if nodeCount < 10 then
                    if nodeCount = 9 then
                        printfn "Node finished: %d" id
                        system1.ActorSelection("user/GossipMaster")
                        <! DoneGossipMaster("Done")

                    let len = myMap.Item(index).Count
                    let randIndex = rand.Next(len)
                    let neighbourIndex = myMap.Item(index).Item(randIndex)

                    system1.ActorSelection(String.Concat("user/Node", neighbourIndex))
                    <! CallGossipNode(neighbourIndex, message)
                    system1.ActorSelection(String.Concat("user/Node", index))
                    <! CallGossipNode(index, message)
                else
                    system1.ActorSelection(String.Concat("user/Node", index))
                    <! EndGossipNode("Done", index)

            | EndGossipNode (someString, index) ->
                let len = myMap.Item(index).Count
                let randIndex = rand.Next(len)
                let neighbourIndex = myMap.Item(index).Item(randIndex)
                system1.ActorSelection(String.Concat("user/Node", neighbourIndex))
                <! CallGossipNode(neighbourIndex, "Gossip")

            | CallPushSumNode (index, sum, weight) ->
                if updatedflag then
                    updatedSum <- updatedSum + sum
                    updatedWeight <- updatedWeight + weight
                    let ratio = updatedSum / updatedWeight
                    updatedSum <- updatedSum / float 2
                    updatedWeight <- updatedWeight / float 2

                    if Math.Abs(updatedPrevRatio - ratio)
                       <= float 0.0000000001 then
                        if updatedCounter = 2 then
                            updatedflag <- false
                            printfn "Node finished: %d" index
                            system1.ActorSelection("user/GossipMaster")
                            <! EndPushSumMaster("Done", index, ratio)
                        else
                            updatedCounter <- updatedCounter + 1
                    else
                        updatedCounter <- 0
                    updatedPrevRatio <- ratio

                    let len = myMap.Item(index).Count
                    let randIndex = rand.Next(len)
                    let neighbourIndex = myMap.Item(index).Item(randIndex)


                    system1.ActorSelection("user/Node" + string neighbourIndex)
                    <! CallPushSumNode(neighbourIndex, updatedSum, updatedWeight)
                    system1.ActorSelection("user/Node" + string index)
                    <! CallPushSumNode(index, updatedSum, updatedWeight)
                else
                    system1.ActorSelection(String.Concat("user/Node", index))
                    <! EndPushSumNode("Done", index, sum, weight)

            | EndPushSumNode (someString, index1, sum1, weight1) ->
                let len = myMap.Item(index1).Count
                randIndex <- rand.Next(len)
                let neighbour = myMap.Item(index1).Item(randIndex)
                system1.ActorSelection(String.Concat("user/Node", neighbour))
                <! CallPushSumNode(neighbour, sum1, weight1)

            | _ -> failwith "fail from node"

            return! nodeLoop (nodeCount + 1) updatedflag updatedSum updatedWeight updatedCounter updatedPrevRatio ()
        }

    nodeLoop 0 true (float id) (float 1) 0 (float 1000) ()


let Master =
    spawn system1 "GossipMaster"
    <| fun mailbox ->
        let rec masterLoop masterCount () =
            actor {
                let computeLine (numNodes: int) =
                    for i = 0 to numNodes - 1 do
                        if (i = 0) then
                            let list1 = new List<int>()
                            list1.Add(i + 1)
                            myMap.Add(i, list1)
                        elif (i = numNodes - 1) then
                            let list1 = new List<int>()
                            list1.Add(i - 1)
                            myMap.Add(i, list1)
                        else
                            let list1 = new List<int>()
                            list1.Add(i - 1)
                            list1.Add(i + 1)
                            myMap.Add(i, list1)

                ()

                let compute2D (numNodes: int) =
                    let side: int = int (MathF.Sqrt(float32 numNodes))
                    for i = 0 to numNodes - 1 do

                        let list1 = new List<int>()
                        if ((i - 1) >= 0) then list1.Add(i - 1)
                        if ((i + 1) < numNodes) then list1.Add(i + 1)
                        if ((i - side) >= 0) then list1.Add(i - side)
                        if ((i + side) < numNodes) then list1.Add(i + side)
                        myMap.Add(i, list1)

                ()

                let computeImperfect2D (numNodes: int) =
                    let side: int = int (MathF.Sqrt(float32 numNodes))
                    for i = 0 to numNodes - 1 do

                        let list1 = new List<int>()
                        if ((i - 1) >= 0) then list1.Add(i - 1)
                        if ((i + 1) < numNodes) then list1.Add(i + 1)
                        if ((i - side) >= 0) then list1.Add(i - side)
                        if ((i + side) < numNodes) then list1.Add(i + side)
                        let mutable randIndex = rand.Next(numNodes - 1)

                        while list1.Contains(randIndex) && randIndex <> i do
                            randIndex <- rand.Next(numNodes - 1)
                        myMap.Add(i, list1)

                ()

                let computeFull (numNodes: int) =
                    for i = 0 to numNodes - 1 do
                        let list1 = new List<int>()
                        for j = 0 to numNodes - 1 do
                            if (i <> j) then list1.Add(j)
                        myMap.Add(i, list1)

                ()
                let! returnOfReceive = mailbox.Receive()

                match returnOfReceive with
                | StartGossipMaster (numNodes, topology, algorithm) ->
                    printfn "%s topology with %i num of Nodes" topology numNodes
                    match topology with
                    | "Line" -> computeLine numNodes
                    | "2D" -> compute2D numNodes
                    | "FullNetwork" -> computeFull numNodes
                    | "Imperfect2D" -> computeImperfect2D numNodes
                    | _ -> failwith "Incorrect topology"

                    let allNodes =
                        [ 0 .. numNodes - 1 ]
                        |> List.map (fun id -> spawn system1 ("Node" + string id) (Node id))

                    let mutable breakWhile = false

                    for i = 0 to nodesToKill do
                        randIndex <- rand.Next(numNodes - 1)
                        breakWhile <- false
                        while not (deadNodes.Contains(randIndex))
                              && not breakWhile do
                            deadNodes.Add(randIndex)
                            allNodes.Item(randIndex).Tell(PoisonPill.Instance)
                            breakWhile <- true


                    randIndex <- rand.Next(numNodes - 1)

                    while deadNodes.Contains(randIndex) do
                        randIndex <- rand.Next(numNodes - 1)

                    match algorithm with
                    | "Gossip" ->
                        allNodes.Item(randIndex)
                        <! CallGossipNode(randIndex, "Hello")
                    | "Pushsum" ->
                        allNodes.Item(randIndex)
                        <! CallPushSumNode(randIndex, float 0, float 1)
                    | _ -> failwith "Incorrect algorithm"



                | DoneGossipMaster (someMsg) ->
                    globalCounter <- globalCounter + 1
                    if masterCount = numNodes - 1 then
                        printfn "All nodes are converged"
                        printfn "------Exiting----------"
                        closingFlag <- true


                | EndPushSumMaster ("Done", index, ratio) ->
                    nodeRatio.Item(index) <- ratio
                    globalCounter <- globalCounter + 1
                    if masterCount = numNodes then
                        printfn "Master Done"
                        for i in 0 .. numNodes - 1 do
                            printfn "Node%d has ratio %f" i (nodeRatio.Item(i))
                        closingFlag <- true

                | _ -> failwith "unknown message"

                return! masterLoop (masterCount + 1) ()

            }

        masterLoop 0 ()

let startTime = DateTime.Now
let timer = Diagnostics.Stopwatch.StartNew()

Master
<! StartGossipMaster(numNodes, topology, algorithm)

while not closingFlag
      && (((DateTime.Now - startTime)).TotalMinutes < 3.0) do
    ignore ()


timer.Stop()

printfn "Number of nodes converged: %d" globalCounter
printfn "Time taken by %s for Convergence: %f milliseconds" algorithm timer.Elapsed.TotalMilliseconds
