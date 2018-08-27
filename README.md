# ParallelGameOfLife
Parallel implementation of Game of Life without explicit synchronization

This is implemention of Conway's Game of Life in both synchronous (centralized) and asynchronous (parallel) fashion.

It shows how extension of both state and rules for classical Game of Life can result in asychronous model which works well in distributed environments without explicit sychronization. In such setup there is no threat of deadlocks since synchronization is implicit via carefully designed model which is fault-tolerant and self repairing.

This is barely a humble implemntation and all credits go to authors of following papers:
- Nehaniv, C. L. "Self reproduction in Asynchronous Cellular Automata"
- Nehaniv, C. L. "Asynchronous automata networks can emulate any synchronous automata network"

As mentioned above most important bit of implementation are the rules and states which are clearly labelled in the code.

Examples are easily run in Unity 2017+

Asynchronous board is tinted to show area of influence of each thread.

Initial state is injectable via property inspector and serialized on prefabs.

At the moment off speaking initial state is "Gosper's Glider Gun"
