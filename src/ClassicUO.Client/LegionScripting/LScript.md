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

| `waitfortarget` | '0/1/2' |
| - | - |
| | 0 = Any target type, 1 = harmful, 2 = beneficial |  

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




# Aliases

## Values

- `name` <- Char name  
- `hits`, `maxhits`  
- `stam`, `maxstam`  
- `mana`, `maxmana`  
- `x`, `y`, `z`  

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