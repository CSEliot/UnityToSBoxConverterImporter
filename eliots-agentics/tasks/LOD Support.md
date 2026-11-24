## Status: STARTED? NO! CLARIFIED? NO! HISTORY? NO! REVIEWED? NO! DONE? NO! WORKER: {subagent_id} OTHER: {state here}

## Task
We got z-fighting which I believe is due to no implementation of LODs?

### Description
Examples:
In SBOX Editor: Floor_stairs_middle (1) at:
{
  "Position": "1255.90295,1413.38306,157.479996",
  "Scale": "1,1,1",
  "Rotation": "0,0,-1,0"
}
has all the LODs from unity just thrown together as components. That's not how sbox handles LoDs.

If you need an "oracle" for correctly writing LODs in S&Box say so. But try first.


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
