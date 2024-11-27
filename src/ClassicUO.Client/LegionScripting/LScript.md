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