## Status: STARTED? NO! CLARIFIED? NO! HISTORY? NO! REVIEWED? NO! DONE? NO! WORKER: {subagent_id} OTHER: {state here}

## Task
Imported objects wrong/missing in transform.


### Description
Example:
Flowerbeds_03_LOD00 at:
UnityEditor.TransformWorldPlacementJSON:{"position":{"x":-27.930999755859376,"y":-0.3075700104236603,"z":0.2583700120449066},"rotation":{"x":0.0,"y":0.7071068286895752,"z":0.0,"w":0.7071068286895752},"scale":{"x":1.0,"y":1.0,"z":1.0}}
is missing or i cant find it, in the sbox export.

Floor_03 at: UnityEditor.TransformWorldPlacementJSON:{"position":{"x":-0.0012005999451503158,"y":-0.3000001907348633,"z":2.9491000175476076},"rotation":{"x":0.0,"y":0.0,"z":0.0,"w":1.0},"scale":{"x":-1.0,"y":1.0,"z":1.0}}
is missing or i cant find it, in the sbox export.

Restroom_Floor_02 IN SBOX is in the wrong location, definitely.
At: World Transform: {
  "Position": "-23.3464108,-862.09906,4.52517557",
  "Scale": "1,1,1",
  "Rotation": "0,0,-0.707106829,0.707106829"
}

Outdoor_Floor_01 IN SBOX is in the wrong location, definitely.
At SBOX World Transform: {
  "Position": "535.433594,393.700439,-11.2204437",
  "Scale": "1,1,1",
  "Rotation": "0,0,0,1"
}
NOTE: The collider is much more correct.

While work is being done, the following oracle data is available:
Unity play screenshot to compare: /Testing/Unity-Screenshot-Outside.png.
Unity project we're importing: C:\cseliot\1-Development\4-Unity\2-Projects\old_school_model

### Work Review


## Notes


## Clarification Q&A


## Relevant History 


## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
