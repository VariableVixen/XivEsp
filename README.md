# XivEsp
_You can run, but you can't hide!_

![GitHub build status](https://img.shields.io/github/actions/workflow/status/VariableVixen/XivEsp/build.yml?logo=github)
![GitHub tag (latest by date)](https://img.shields.io/github/v/tag/VariableVixen/XivEsp?label=version&color=informational)
![GitHub last commit (branch)](https://img.shields.io/github/last-commit/VariableVixen/XivEsp/main?label=updated)
[![GitHub issues](https://img.shields.io/github/issues-raw/VariableVixen/XivEsp?label=known%20issues)](https://github.com/VariableVixen/XivEsp/issues?q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc)

[![Support me!](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/V7V7IK9UU)

## About
XivEsp is a plugin designed to help you find things in the world, in a simple and very quick manner. While plugins like [Splatoon](https://github.com/PunishXIV/Splatoon) _can_ be configured to highlight game objects by name, doing so is a slightly involved process - you have to make a layout, then add an element, configure it with the display settings you want, set it to target game objects by name, and then enter the name.

This is clearly useful for things you _always_ want to highlight, but what about doing hunts? If you're just trying to find a particular mob for a few kills, that's a decent bit of work, especially when you then have to open it back up to delete the layout when you're done. In contrast, XivEsp only needs you to run one command to set the name you're looking for, and then another command (or just click the server info bar entry!) to clear it when you're finished. If you just want to quickly locate something by name for a brief time, that's _much_ easier!

## Installation
Type `/xlplugins` in-game to access the plugin installer and updater. Note that you will need to add [my custom plugin repository](https://github.com/VariableVixen/MyDalamudPlugins) (full instructions included at that link) in order to find this plugin.

## In-game usage
There are four commands in XivEsp, although one is optional and two are more advanced than most people will need. The most common one you'll be using is `/esp <text to search for>` - this sets a case-insensitive _substring_ pattern, which means that any game objects (loaded on your client) whose names contain that text will be located. If you're trying to hunt, for instance, the White Joker for your ARR weekly elite hunt, you could do `/esp joker` and it will be matched.

The next most common (although optional) command is `/espc`, which simply clears your search. If you're done looking for your target but don't want other things that incidentally match your search to _also_ be flagged, you can use this command _or_ click on the server info bar entry in order to stop looking for anything.

Probably the third most common command is `/espg <pattern to search for>`, which is similar to `/esp` but sets a (case-insensitive) _glob_ search. There are four special characters in glob searches: `*` matches anything at all, including nothing; `?` matches a _single_ character, which can be anything, but won't match nothing; `[` and `]` combine to match any character within them. **Glob searches are _not_ substrings**, meaning that unless you surround your glob pattern with a `*` on either side, it will only match game objects whose _full name_ matches the pattern provided!

The fourth and most powerful (but also most complicated) command is `/espr <pattern to search for>`, which uses a full regular expression pattern search. This means you can specify the exact thing you're searching for very carefully, but you probably won't really _need_ to in FFXIV. Unlike globs, **regular expressions _are_ "substring-like" searches** because it's possible to specify start-of-string and end-of-string anchors in regular expressions.

Explaining regular expressions is _extremely_ beyond the scope of this documentation.

### Server info bar
XivEsp adds a display to the server info bar indicating what kind of search it's currently doing; `N` for nothing, `S` for substring, `G` for glob, and `R` for regular expression. When any search is active, hovering over this display will remind you of the type and show the full pattern being searched for; you can then click on it to clear your current search. When you don't have any search active, the tooltip text will instead remind you of the three commands to set a search, in case you've forgotten any of them.
