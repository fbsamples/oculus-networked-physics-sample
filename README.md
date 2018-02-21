Networked Physics in Virtual Reality: Unity Sample
==================================================

Welcome to the Oculus Rift networked physics in VR sample!

General controls:

    Grip button         = grab, release cube
    Index button        = snap cube to hand
    Stick up/down       = move held cube in/out
    Stick left/right    = rotate held cube

There are three scenes in the Unity project:

    1. Loopback
    2. Host
    3. Guest

To run the networked physics demo, open up unity and select the "Loopback" scene in the "Scenes" folder. This is the best place to start.

The loopback scene simulates a network connection between a host and a guest in one application. The host is the initial set of cubes you spawn in front of. The guest is the set of cubes to the right. Actions you make in either simulation are mirrored to the other via a virtual network. You can switch between host context (cubes turn blue when you interact with them), to the guest context (cubes red when you interact with them) by pressing "A" and "B" buttons on your touch controller. Press "SPACE" to reset the simulation.

Next, you can run a host and guest. The host acts as a server that up to three other players can connect to. Guests use the Oculus Platform SDK matchmaker to find a host to connect to. Pressing "SPACE" as host resets the simulation. Guests pressing "SPACE" disconnect from the current game and go back to matchmaking to find a new host (if there is only one host running, guests will simply reconnect).

A "Build" menu has been added to the Unity menu so you can easily create standalone builds. Builds are created in the "Builds" directory under the Unity project. You can build Loopback.exe, Host.exe and Guest.exe, corresponding to each scene in the Unity project.

If you want to restrict play between yourself and friends, edit the "Version" string in Constants.cs and set it to some unique phrase before making builds. The host and guest will only matchmake with players that have the same version string.

## License

The sample in [Assets/Scripts](Networked%20Physics/Assets/Scripts) is licensed under the BSD [License](Networked%20Physics/Assets/Scripts/LICENSE) with an additional grant of [patent rights](Networked%20Physics/Assets/Scripts/PATENTS) except as otherwise noted.

The Oculus SDK, Platform, and Avatar components are licensed under the Oculus SDK [License](LICENSE).

Enjoy!
    
Glenn Fiedler <glenn.fiedler@gmail.com>
