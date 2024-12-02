# Legion Scripting




# Commands

## `msg`

`msg` *text*  
Make your player say something in game  
Example:  
`msg I banish thee!`  

## `togglefly`

If your player is a gargoyle this will send a toggle fly request  

## `useprimaryability`

Use your primary ability

## `usesecondaryability`

Use your secondary ability

## `clickobject`

`clickobject` 'serial'  
Example:
`clickobject 'self'` or `clickobject '0x1234567'`

## `attack`

`attack` 'serial'  
Example:  
`attack 'self'` or `attack '0x1234567'`


## `bandageself`

Attempt to bandage yourself


## `useobject`

Use an object(Double click)

| `useobject` | 'serial' | *'true/false'* |
| - | - | - |
| | Object serial | Use double click queue (Not required, default true) |

Example:  
`useobject '0x1234567'` or `useobject '0x1234567' false`


## `target`

Target an object or mobile  
`target` 'serial'  

Example:  
`target 'self'`



## `waitfortarget`

| `waitfortarget` | '0/1/2' | '10000' |
| - | - | - |
| | 0 = Any target type, 1 = harmful, 2 = beneficial | Timeout, 10000 is default(10 seconds) |

Example:  
`waitfortarget '0'`


## `usetype`

| `usetype` | 'container' | 'objtype(graphic)' | 'hue(optional)' |
| - | - | - | - |
| | Container serial | Object type | Hue(If not included, any hue will match) |

Example:  
`usetype 'backpack' '0x456'` or `usetype 'backpack' '0x456' '32'`


## `pause`

Pause the script for a duration of time in milliseconds  
`pause` 'duration in ms'  
Example: `pause 1000`


## `useskill`

Use a skill from skill name  
`useskill` 'skillname(Can be a partial name)'  
Example: `useskill 'evaluate'`


## `walk` or `run`

Send a walk or run request to the server  
`walk`/`run` 'direction'  
 
| Directions |
| - |
| north |
| right |
| east |
| down |
| south |
| left |
| west |
| up |  

Example: `run 'north'`


## `canceltarget`

Clear a target cursor if there is one


## `sysmsg`

Display a message in your system messages(Not sent to the server)  

| `sysmsg` | msg | hue |
| - |
| | required | optional |

Example: `sysmsg 'No more tools!' '33'`


## `moveitem`

Move an item to a container  
`moveitem` 'item' 'container' 'amount(optional)'  
If amount is 0 or not used it will attempt to move the entire stack  

Example: `moveitem '0x23553221' 'backpack'`


## `moveitemoffset`

Move an item to the ground near you  

| `moveitemoffset` | 'item' | 'amt' | 'x offset' | 'y offset' | 'z offset' |
| - |
| | | Use 0 to grab the full stack | | | |

Example: `moveitemoffset '0x32323535' '0' '1' '0' '0'`


## `cast`

Cast a spell by name  
`cast` 'spell name'  
Spell name can be a partial match  

Example: `cast 'greater he'`


## `waitforjournal`

Wait for text to appear in journal  
`waitforjournal` 'the text' 'duration in ms'  
Example: `waitforjournal 'you begin' '15000'` <- this waits for up to 15 seconds


## `settimer`

Create a timer. If the timer already exists, this is ignored until the timer expires.  
`settimer` 'name' 'duration'  
Note: Timers are shared between scripts so make sure to name them uniquely.  
Example: `createtimer '123bandage' '10000'`   //Create a timer named 123bandage with a duration of 10 seconds


## `setalias`

Set an alias to a serial  
`setalias` 'name' 'serial'  
Example: `setalias 'pet' '0x1234567'`  


## `unsetalias`

Unset an alias  
`unsetalias 'pet'`  


## `movetype`

Move any object matching the type  
`movetype 'graphic' 'source' 'destination'  [amount] [color] [range]`  
Amount and color are optional  
Example: `movetype 0x55 'backpack' 'bank'`  


## `findtype`

Find an object by type  
`findtype 'graphic' 'source' [color] [range]`  
Example: `findtype '0x1bf1' 'any' 'any' '2'` <- Find items in containers or ground within 2 tiles





# Expressions

## `timerexists`

Check if a timer exists  
`timerexists` '123bandage'  
returns `true`/`false`


## `timerexpired`

Check if a timer has expired  
`timerexpired` '123bandage'  
Example: `if timerexpired '123bandage'`








# Aliases

## Values

- `name` <- Char name  
- `hits`, `maxhits`  
- `stam`, `maxstam`  
- `mana`, `maxmana`  
- `x`, `y`, `z`  
- `true`, `false`

## Objects

- `backpack`
- `bank`
- `lastobject`
- `lasttarget`
- `lefthand`
- `righthand`
- `mount`
- `self`
- `bandage`
- `any` <- Can be used in place of containers
- `anycolor` <- Match any hue





# Syntax

`if 'condition'`  
	`elseif 'condition'`  
	`else`  
`endif`


`while 'condition'`  
`endwhile`  


`for 'count'`  
`endfor`  


`foreach 'item' in 'list'`  
`endfor`  


`break`, `continue`  


`stop` <- Stop the script  
`replay` <- Start the script over  
