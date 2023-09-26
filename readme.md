# gganki

[![demo](https://i.ytimg.com/vi/sNwjHgGjodQ/maxresdefault.jpg)](https://www.youtube.com/watch?v=sNwjHgGjodQ "demo")
(Click to watch demo on youtube)

# About

This project is a game interface for [anki](https://apps.ankiweb.net/).
The goal of this project is to lessen the tedium of reviewing cards by means of
a monter-hunting game.

## Project status

Discontinued and no longer maintained. There are several reasons for this:

1. The game I've made isn't exactly that much fun to play. It was interesting
   for the few hours, but it quickly got stale and boring, which defeats
   the whole point of the game interface for anki.

2. Having anki dependency (including the addon) multiplies the complexity
   of maintaining the project, moreso when it comes to crealing a release
   that works standalone and reliably. It puts the burden on finding the right
   versions of anki+addon on the users. In hindsight, I could have just
   used a simpler spacing algorithm.

3. Currently, gganki only works on particular kinds of deck, specifically
   the AJT Kanji Transition TSC deck. Making gganki work agnostically with any kind of
   deck is way harder, if not impossible, due to do how unstructured anki cards are.

4. I used an unmaintained library C# wrapper for lua LÖVE. The
   library does work fine for the most part, I found a few bugs
   that I easily found a workaround without needing to fork.

5. I used minimalistic game engine like lua LÖVE. I spent way more
   time reimplementing stuffs that a more full-featured game engine
   would provide out of the box. It was fun, but I side-stepped too much
   from the original path, which resulted to an experimental unmaintainable codebase.

To be clear, only this particular project is discontinued. I think
the idea is interesting and has potential. I'm actually thinking
and planning of creating a rewrite with [godot](https://godotengine.org/)
and without the anki dependency.

### Dependencies

- [anki](https://apps.ankiweb.net/), tested on version v2.1.49
- [anki-connect addon](https://github.com/FooSoft/anki-connect), tested with v22.2.19.0
- [anki deck: AJT KanjiTransition](https://ankiweb.net/shared/info/917377946)

## How to run

1. Install anki and addon (see above)
2. Make sure anki is running, and the anki-connect addon is enabled.
3. Add AJT deck to anki. It's possible the latest version is no longer compatible.
   In that case, you can use this archived [deck](assets/ajt-deck.apkg).
   Note: the deck must have exactly the name "AJT Kanji Transition TSC",
   otherwise, it won't work.
4. Download gganki from [here](https://github.com/nvlled/gganki/releases)
5. Run gganki

Since this is an unfinished and discontinued project, you would most likely
be unable to run gganki without a ton of errors. If that's the case, you
can try building from source instead (see Development section).

## Controls

- **WASD keys**: movement
- **spacebar**: dash
- **mouse movement**: aim
- **left-click**: short-range attack
- **right-click**: long-range attack
- **double right-click**: mid-range attack

## Development

The codebase is in a messy state, typical hacky gamedev/startup code.
Much of the code is dumped on Script.cs. I wouldn't normally
code like this in a more professional, collaboratve setting,
so it would be great it if I wasn't judged based on this
project. That all said, here's how to build from source:

1. Install [dotnet](https://dotnet.microsoft.com/en-us/download)
2. `$ git clone https://github.com/nvlled/gganki`
3. `$ cd gganki; dotnet run`
