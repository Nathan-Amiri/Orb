Orb is a simple PvP game created to learn and practice Unity's Lobby, Relay, and Netcode for Gameobjects systems

Created in Unity and coded in C#

Check out the game's code [here](https://github.com/Nathan-Amiri/Orb/tree/main/Assets/Scripts)

Play the game [here](https://machine-box.itch.io/orb)

Orb has 3 game modes: Practice, Vs. AI, and Battle (online PvP).
In each game mode, Input and 'Sensor' (aka player collision) data is passed through remote procedure calls (RPCs) passed between the server and client(s).
When playing an offline game mode, these RPCs never leave the player's device. To simplify the networking code within the project, all RPCs are placed either within the GameManager class
(which handles Lobby & Relay connection for the online game mode) or within the InputRelay/SensorRelay classes, which relay Input/Sensor data between Players.

This data is relayed in the following direction:
Input data: PlayerInput OR EnemyAIInput > InputRelay > Player
Sensor data: PlayerSensor > SensorRelay > Player

EnemyAI objects are identical to Player objects, except that they contain an EnemyAIInput component rather than a PlayerInput component. Their behavior is governed by this EnemyAIInput class
