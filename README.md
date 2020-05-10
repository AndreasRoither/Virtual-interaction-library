
# Table of Contents
- [Virtual Reality Interaction Library](#virtual-reality-interaction-library)
  - [Summary and Goal](#summary-and-goal)
  - [Example Integration](#example-integration)
    - [Keyboard and Mouse](#keyboard-and-mouse)
    - [SteamVR](#steamvr)
  - [Interaction Techniques](#interaction-techniques)
    - [Ray-Cast](#ray-cast)
    - [Depth-Ray](#depth-ray)
    - [iSith](#isith)
    - [Spindle + Wheel technique](#spindle--wheel-technique)
  - [Navigation Techniques](#navigation-techniques)
    - [Steering](#steering)
    - [Grab the Air](#grab-the-air)
    - [Point and Teleport](#point-and-teleport)
    - [World in Miniature](#world-in-miniature)


# Virtual Reality Interaction Library
Library for interaction and navigation techniques. References as links with names.

## Summary and Goal
The main goal of this library is to provide an easy to use framework with a few interaction and navigation techniques as a base for developers to implement new techniques. 
Techniques that are created using this library do not have to rely on a specific hardware solution. 
This is achieved by using a component that manages all techniques and registered controllers while providing a method to notify techniques that can be invoked from any script. 
Upon invocation, the library notifies all interaction and navigation techniques that are registered for a controller with the necessary information on which button was pressed or in which state a button currently is. 
This allows for integration into any existing solution by simply rerouting the inputs to the managing component (VRIL_Manager).

## Example Integration
To show how the interaction library can be used, two example scripts are implemented. Scenes for each example have been set up in Unity that utilize said example scripts.

### Keyboard and Mouse
The *VRIL_Keyboard* script allows the user to move, rotate, select and activate a technique.
This script simply remaps any input from the keyboard or mouse and notifies the *VRIL_Manger*.
Additionally, the script also shows which controller is currently selected which can be seen in the figure below on the top left side.

<img src="./images/VRIL_Keyboard.png" width="400"/>

###  SteamVR
The [SteamVR]([https://www.google.com](https://steamcommunity.com/steamvr)) example script utilizes the default actions that are generated on start. 
Actions were introduced with SteamVR 2.0 and allow developers to subscribe to an action instead of checking input from the controller manually.
To use said actions or create new ones, the SteamVR input window in Unity offers a variety of options (package has to be installed first, as well as the SteamVR software).
If these actions have been generated, they can be bound to a specific controller input action like a button or a touchpad.
After actions are bound and generated, developers can subscribe or attach a new listener to an action.
The example script utilizes 4 of the default actions, that are auto-generated when opening the SteamVR Input for the first time, and registers listeners to each one.
After any input from the controller was detected, the *VRIL_Manager* will be notified.
In the figure below, an example for the *iSith* technique with SteamVR can be seen.

<img src="./images/VRIL_SteamVR.png" width="400"/>

## Interaction Techniques
These are the interaction techniques that are implemented in the library. An example image demonstrates the use of the technique described.

### Ray-Cast
The Ray-Casting technique (earliest version mentioned by [Bolt](https://dl.acm.org/citation.cfm?doid=800250.807503), later again by [Bowman and Hodges](https://dl.acm.org/citation.cfm?doid=253284.253301)) is an interaction technique that utilizes a single controller with a line to show the user what objects can be selected. Anything the line touches and is selectable can be selected.
The ray-casting technique is implemented by using a single [line renderer](https://docs.unity3d.com/ScriptReference/LineRenderer.html). 
By using a [special attribute](https://docs.unity3d.com/ScriptReference/RequireComponent.html) from the Unity scripting API, a line renderer will automatically be attached to the parent game object of the script if no line renderer is present:
```csharp
[RequireComponent(typeof(LineRenderer))]
```
The line renderer is used to create a single line moving forward from the first registered controller with a customizable maximum distance. 
Line width, starting and ending width as well as the color of the line can be adjusted in the automatically created line renderer script.
Since the ray should be able to select interactable objects (objects that have the *VRIL_Interactable* component attached), a [Physics.Raycast](https://docs.unity3d.com/ScriptReference/Physics.Raycast.html) from the Unity scripting API is used.
The ray is provided with a point of origin, direction and a maximum length and any objects that collide with said ray will be selected. 

An example of the Ray-Cast technique can be seen in below.


<img src="./images/gifs/VRIL_RayCast_Example.gif" width="400"/>

### Depth-Ray
The Depth-Ray technique ([Grossman and Balakrishnan](https://dl.acm.org/citation.cfm?doid=1166253.1166257)) is similar to the single Ray-Cast technique but instead of relying on the ray to select objects, a sphere is used to indicate the area where objects can be selected. 
Together with another controller, the distance between the two controllers determines the distance of the model on the ray. Linear mapping is used to determine the distance and can be adjusted freely. 
The greater the distance between the two registered controllers for this technique, the greater the distance of the model on the line to the controller where the ray originates. 
The selection model indicates the area in which an object can be selected. If another model with a different mesh is used like a quad or cylinder, the model will not accurately represent the selection area since a sphere ray cast is used to check for objects in the area.
This technique provides a small set of options like the model size, initial sphere distance and linear distance mapping.

An example of this technique can be seen below.

<img src="./images/gifs/VRIL_DepthRay_Example.gif" width="400"/>


### iSith
The iSith technique ([Wyss, Blach, and Bues](https://ieeexplore.ieee.org/abstract/document/1647507)) utilizes two controllers to select an object. A ray from each of the controllers is created that expands in front of the controller with a maximum distance. 
If those two intersect with one another a model is created. 
This model can grow or shrink in size depending on the distance between the two main rays from the controllers.
The model indicates the region in which an object can be selected.
  
The iSith technique implements three line renderers.
Since the line renderers are only instantiated at runtime (there is currently no way to require 3 instances of a line renderer via class attribute), a small list of options is provided for the ray: width, color, the line material, the shadow casting mode and the maximum distance of the ray.
One line is created at each controller position moving forward with the options specified. The third line from the last renderer is used to indicate the shortest distance between the two forward vectors from each controller. 
The line will not be displayed if one vector is not in front of the other (one controller is pointing behind the other controller).
If there is a shortest distance between the two vectors, a custom model of a transparent sphere is placed in the middle between the two vectors.
  
The provided model will be automatically resized based on the two options *MinSphereDiagonal and MaxSphereDiagonal*. As the rays are further apart from each other the model can be resized to fit the desired size.
This model can be replaced with any other model as long as no component that creates a [collider](https://docs.unity3d.com/ScriptReference/Collider.html) around the model is attached to it.
The model, however, does not indicate the space in which objects can be selected if it has a different mesh than a sphere.
Every frame, the interaction technique will cast a sphere, which size corresponds to the *MinSphereDiagonal and MaxSphereDiagonal* options that are used to resize the used model.
Any overlapping objects that have the *VRIL_Interactable* component attached will be selected by using a sphere ray-cast.
To make development easier, the *OnDrawGizmos* function from the Unity scripting API is used to show the size of the actual sphere which can be seen in the scene view in Unity if activated.
  
Depending on the position and if there is a shortest distance between the two lines, the provided model will be set active or inactive to provide better performance instead of deleting it. 
Setting the model inactive means it will not be visible anymore.
If the model is invisible no objects can be selected even if the rays point at objects as the main usage is to select objects with the sphere instead of the rays.

An example of the iSith implementation in action can be seen below.

<img src="./images/gifs/VRIL_iSith_Example.gif" width="400"/>


### Spindle + Wheel technique
The Spindle + Wheel technique ([Cho and Wartell](https://ieeexplore.ieee.org/abstract/document/7131738)) is an extension of the original Spindle ([Mapes and Moshell.](https://doi.org/10.1162/pres.1995.4.4.403)) technique.
While the iSith technique focuses on the selection part of an interaction technique by providing a unique way of selection with two rays, the Spindle + Wheel technique focuses on the manipulation part after selecting an object. 
  
An object is selected by using both controllers and a selection model similar to the iSith technique. 
But instead of using two rays only one ray is instantiated. At the middle between the two controllers, a transparent model is placed to indicate the area in which objects can be selected.
After an object has been selected the technique can be switched to the manipulation mode which results in the selected object being placed in between the two controllers.
The manipulation mode then allows users to manipulate an object's rotation, position and the scale:  
- **Scaling**:  
    The distance between the two controllers translates to the scale of the object.  
- **XYZ axis translation**:  
    Moving both controllers allow for the placement of the object along all axes.  
- **YZ axis rotation**:  
    Asymmetric hand movement result in yz rotations on the object.  
- **X rotations**:  
    Moving the dominant-hand controller results in changed x rotations on the object that translate to the controller rotation.  
  
An example of this technique can be seen below.

<img src="./images/gifs/VRIL_SpindleWheel_Example.gif" width="400"/>

## Navigation Techniques
These are the navigation techniques that are implemented in the library. An example image demonstrates the use of the technique described.

### Steering
The library provides both gaze-directed steering and hand-directed steering techniques ([Mine](https://pdfs.semanticscholar.org/69ff/1367d0221357a806d3c05df2b787ed90bdb7.pdf)). On application startup, it is identified whether the camera or controller object is used for steering, depending on the value provided in option *Technique*. When the steering technique is activated, the viewpoint is transferred repeatedly to a new position as long as the button is pressed. The target position is calculated by adding the object's forwards to the current position of the viewpoint. In addition, the result is calculated by the provided velocity. The steering technique provides options that must be set in advance to enable movement along the desired axes. For example, to be able to fly through the virtual space, all axes have to be enabled. When the hand-directed steering technique is used, an additional option provided in the Inspector window allows to enable either the pointing mode or crosshairs mode. While former simply uses the forward vector of the controller object as steering direction, the latter calculates a vector from camera to controller object. This vector is used then for the direction.

<img src="./images/gifs/VRIL_Steering_Example.gif" width="400"/>

### Grab the Air
The grab the air technique ([Mapes and Moshell.](https://dl.acm.org/doi/abs/10.1162/pres.1995.4.4.403)) transfers not the viewpoint to a new position, but the whole virtual world around the user. The position of the user remains the same. To avoid moving each individual object separately, a parent world object containing all objects of the virtual world has to be attached to the technique component in the Unity editor. This world object is then used by the technique to apply position changes of the controller to the world. When the technique is activated, a co-routine is started which continuously applies the position changes of the controller to the world object as long as the button is pressed. Each controller position change is determined by the vector from its previous position (world is not attached to the controller!). Similar to steering, options are provided to enable individual axes. To prevent the user from being affected by any position changes, the viewpoint cannot be a child object of the world, it must be separated from it. Since many grabbing motions are necessary to cover larger distances within the virtual world, a scaling factor is provided to scale every motion of the controller by a given value. The distance of both controller positions (previous and new) is multiplied by this scaling factor to achieve controller motions result in larger position changes of the world.

<img src="./images/gifs/VRIL_GrabTheAir_Example.gif" width="400"/>

### Point and Teleport
The point and teleport technique ([Bozgeyikli, Raij, Katkoori and Dubey](https://dl.acm.org/doi/abs/10.1145/2967934.2968105)) allows the user to select the desired target position in advance. The viewpoint is then transferred to this selected target position. The library provides two types: Blink teleport ([Cloudhead Games](https://cloudheadgames.com/blink-and-youll-miss-us-at-pax/)) and dash teleport ([Bhandari, MacNeilage and Folmer](https://dl.acm.org/doi/10.20380/GI2018.22)). On activation, the technique enables a selection mode where a coroutine is started to display a parabola which can be controlled by the user to target to any object surfaces in the virtual space. This parabola is drawn from the controller object up to a maximum distance. When the parabola hits any object, the collision point is checked whether it can be used as a valid position to travel to. For the position detection, the target object has to be navigable and a [collider](https://docs.unity3d.com/ScriptReference/Collider.html) component must be attached to it. Also a maximum surface angle can be specified. The parabola itself is based on the [projectile motion curve](https://en.wikipedia.org/wiki/Projectile_motion) and is composed of multiple rays. The curve can be modified by setting the projectile velocity in Unity editor. A ray cast is then used that moves along these points in the parabola to check for collision within the distance between them until either a defined maximum number of segments is reached or a navigable object is hit.

For the visualization, a [line renderer](https://docs.unity3d.com/Manual/class-LineRenderer.html) is used, to which all points are passed. This line renderer is instantiated at runtime and can be modified in advance by the provided options for length, width, line material and the maximum number of points. Also two different colors can be set in order to visualize whether a valid target position has been selected. Depending on the decision whether the desired point can be used for travel, the color of the line renderer is updated accordingly (for example using a green color to show a successful selection and a red one for hitting obstacles or no hit). In addition, a selected position is highlighted by an object called "hit entity". This object must be attached to the script in the Unity editor and is placed at the selected position in case it is valid.

#### Blink Teleport
The blink teleport transfers the viewpoint instantaneously to a selected position by simply updating the viewpoint position. To achieve the effect of eye-blinking, the scene is faded out for a moment of time. Here, *SteamVR\_Fade* from the [Valve.VR API](https://valvesoftware.github.io/steamvr\_unity\_plugin/api/Valve.VR) is used, whereby the scene fades out immediately and the given color is shown (invoking function with zero seconds as time to fade in). After a specified period of time, the scene fades in with the given duration:

```csharp
SteamVR_Fade.View(Color.clear, FadeInDuration);
```

#### Dash Teleport
The dash teleport transfers the viewpoint continuously towards the selected position by a given velocity. For this dash movement, a co-routine is started which moves the viewpoint step by step towards the target position until it is close enough. For each step, the *MoveTowards* function from the Unity scripting API is used to update the viewpoint position.

An example of the point and teleport technique using dash mode can be seen below.

<img src="./images/gifs/VRIL_Teleport_Example.gif" width="400"/>

### World in Miniature
The world in miniature (WIM) technique ([Stoakley, Conway and Pausch](https://dl.acm.org/doi/pdf/10.1145/223904.223938)) allows the user to select a target position by pointing with a ray on a miniature representation of the virtual world. The viewpoint is then transferred to the corresponding position in the large-scaled world. This technique requires two controllers. The ray hand controller activates the technique and enables the ray for the selection on the miniature world, which is attached to the second controller (WIM hand). On activation, the technique enables the ray and the miniature world is created. First, all objects having a [MeshRenderer](https://docs.unity3d.com/560/Documentation/Manual/class-MeshRenderer.html) component attached are selected. Since not all objects are necessary in the WIM, these objects are filtered in advance. Objects having the ignore component attached, the controllers and too small objects are ignored in the WIM. For the latter, options are provided to define threshold values for X, Y and Z an object have to exceed to be included in the miniature world. The filtered objects are then cloned by invoking [Object.Instantiate](https://docs.unity3d.com/ScriptReference/Object.Instantiate.html}). These clones are assigned to the WIM object as children, which is an empty game object. The newly created world is scaled down by simply setting the local scale of the WIM object to a provided scale factor. The WIM is then rendered on top of the controller of the WIM hand. To avoid the miniature version becoming poorly visible in darker virtual worlds, a light source can be added (placed above the controller). It is possible to provide a layer in order the light source only illuminates the WIM by using a suitable culling mask. The WIM objects can also be updated according to their original's in the large-scaled world. When the option "Refresh WIM" is enabled, the synchronization of both worlds is performed whereby new objects get cloned and attached to the WIM and deleted ones get also deleted in the WIM. 

In order to visualize the user’s current position and orientation, an avatar is used for the representation in the WIM. The avatar's local Y-rotation is updated according to the rotation of the viewpoint camera and shows the position as well as the current viewing direction in the WIM. In addition to this, a second avatar is provided in order to visualize the orientation at the desired position during the selection. For this "shadow avatar" it is recommended to use the same avatar object but with a higher transparency to make both visually different. There is an option provided to specify whether the shadow avatar statically looks away from camera or its orientation can be modified by the user. For the latter, it is manipulate the shadow avatar’s viewing direction by rotating the controller.

In addition to the miniature world, a ray is instantiated which is drawn from the ray hand directing to the controller’s forward vector. Analogous to the point and teleport technique, only surfaces of navigable objects can be selected. Furthermore, an option is provided to define a maximum angle for a valid surface in the Unity editor.

#### Teleportation Based Approach
The teleportation based approach transfers the user instantaneously by updating the viewpoint position according to the selected position on the miniature. As the target orientation is changing as well, it is necessary to rotate the viewpoint accordingly. Thereby the difference between the avatar's local Y rotation is added to the Y rotation of the viewpoint object. Since the forward direction of the camera does not always match the forward direction of the viewpoint, the local Y rotation of the camera is subtracted from the viewpoint rotation. This way the user's viewing direction remains the same even when not looking straight ahead. When the viewpoint is transferred to the target position, the hit entity is removed from the WIM object (instance is reused on next activation). Finally, the WIM object is destroyed. Both avatar objects and the hit entity are essential for travel task. However, these objects do not necessarily have to be attached in the Unity editor. Here, an empty game object is used in case no object is provided.

#### Flight Into the Miniature
This approach allows the user to fly into the miniature world until the position and orientation of the viewpoint is identical to the figure ([Pausch, Burnette, Brockway and Weiblen](https://dl.acm.org/doi/pdf/10.1145/218380.218495)). Instead of translating the viewpoint once, the technique starts an animation by using a co-routine to move the user towards the target position while continuously scaling up the WIM. For each step, the current local scale of the WIM is multiplied by the scaling velocity. The WIM then is rendered with the newly calculated local scale. Furthermore, the viewpoint moves towards the hit entity position by a provided velocity (invoking *Vector3.MoveTowards* of the Unity scripting API). Additionally, the viewpoint rotation changes until it is identical with the viewing direction of the shadow avatar. The function [Quaternion.RotateTowards](https://docs.unity3d.com/ScriptReference/Quaternion.RotateTowards.html) from the Unity scripting API is used which performs a rotation from a quaternion towards a provided target quaternion by an angular step:

```csharp
Viewpoint.transform.rotation = Quaternion.RotateTowards(Viewpoint.transform.rotation, rotation, step);
```
The function is provided with the viewpoint rotation as the quaternion of origin, the rotation of the shadow avatar as target quaternion and an angular step. The latter is calculated by a provided rotation factor, the difference of rotation between viewpoint and shadow avatar and the ray length. When the viewpoint arrives at the target position, the flight animation ends and the WIM object is destroyed.

To keep the user's focus on the miniature world during the travel, a property in the Unity editor provides the possibility to hide the large-scaled world while flying into the miniature. Here the references to the original objects are used to deactivate the renderer component for the object. This prevents the object from being drawn.

An example of the WIM technique can be seen below.

<img src="./images/gifs/VRIL_WIM_Example.gif" width="400"/>
