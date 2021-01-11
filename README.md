Team Members:
Aarthi Kashikar, UFID: 0366-8968
Sri Kanth Juluru, UFID: 9279-1918

Working scenarios:
Algorithms: Gossip, PushSum
Topologies: FullNetwork, Line, 2D, Imperfect2D

Largest Network for each topology:
					      Gossip	        PushSum
FullNetwork				   30000              200
Line				       2000               50
2D						   5000               100
Imperfect2D			       5000               100

How to run the code?
dotnet fsi --langversion:preview proj2.fsx numNodes topology algorithm

In place of numNodes, we will pass number of nodes. Possible values of topology are “Line”, “2D”, “FullNetwork”, and “Imperfect2D”. Possible values of algorithm are “Gossip” and “Pushsum”. For example, 

dotnet fsi --langversion:preview proj2.fsx 200 2D Gossip

Bonus:

Working scenarios:
Algorithms: Gossip
Topologies: FullNetwork, Line, 2D, Imperfect2D

How to run the code?

dotnet fsi --langversion:preview proj2Bonus.fsx numNodes topology algorithm nodesToKill

In place of numNodes, we will pass number of nodes. Possible values of topology are “Line”, “2D”, “FullNetwork”, and “Imperfect2D”. Possible values of algorithm are “Gossip” since we have implemented this for Gossip and nodesToKill takes number of nodes to kill. For example, 

dotnet fsi --langversion:preview proj2Bonus.fsx 1000 2D Gossip 100