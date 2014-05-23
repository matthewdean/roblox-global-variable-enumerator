roblox-global-variable-enumerator
=================================
Lists all RBX.Lua global variables

Explanation
-----------
Long ago, you could do this to get a list of all global variables in ROBLOX Lua:
```lua
for index, value in pairs(getfenv(0)) do
	print(index)
end
```

However, exploiters started modifying the global environment to get a higher identity level, so ROBLOX did this:
```lua
local env = setmetatable({},{
	__index = {
	  game = ...,
	  workspace = ...,
	},
	__metatable = "The metatable is locked"
})
```

The side effect of this change is that it's no longer possible to enumerate global variables. So I wrote this program which:
  1. Scans RobloxStudioBeta.exe for ASCII strings of length >= 2 because some of those strings are bound to be global variable identifiers.
  2. Performs a brute-force search against the global environment to determine which of these strings are global variable identifiers
  3. Sends the result back to the command-line program

Limitations
-----------
* It is possible that this program will miss some global variables (e.g. if a single-char GV was added)
* Only compatible with versions of studio which have Plugin::GetSetting and Plugin::SetSetting
