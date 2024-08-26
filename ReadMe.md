# Jammy

## Jim's Amiga Emulator

This is a lockdown project I started in December 2020 with the intention of building an Amiga emulation that could interface the UI with Windows instead of Workbench.
That quirky part of the project dropped away as soon as it became clear that you need to build a _really good_ emulation first before you can run almost anything.
So here is my humble attempt at an Amiga emulation.

### The good stuff
* It's written in C#
* It runs faster than real Amigas (on my i5-8250 laptop)
* It can emulate most Amigas to at least some level
  * A1000, A500, A500+, A600, A1200, A2000, A3000, A4000, CD32
  * 68000 in C#, verified against the Musashi 68000 CPU
  * Other 680x0 using the Musashi CPU emulation, in C or C#
  * Copper, Blitter, CIA emulations
  * Sprite and bitplane collision
  * Good quality Audio
  * OCS, ECS, AGA emulation
  * Floppy Disks (read) and ATA Hard Disks (read/write)
  * All kinds of RAM expansions (Chip, Trapdoor, CPU slot, Zorro II/III)
  * Battery-backed clock
  * VT100 Serial terminal
* There's a debugger and disassembler of sorts
* There's some automated code analysis to produce good disassemblies
* It's free! (MIT License)

### The not so good stuff
* There are lot of problems with DMA timing
* More efforts need to be made to get closer to cycle exact
* Akiko not implemented
* Emulation of CPUs running faster than the standard 7MHz is poor

The C# 68000 is slightly faster than the C Musashi one, not because I have made any specific efforts to optimise it, but I think mostly because thunking out of C# into C and back again isn't particularly fast.
In an effort to fix this, I have ported most of Musashi to C#, and it is significantly faster now.

The C Musashi 68030 option supports the 68881 and MMU instructions. There didn't seem any real need to add a 68040 or 68060 option because of this.

The audio is pretty good, if you run with it switched on it will lock the emulation performance down so the sample rate is exact. With it switched off, everything is still emulated but there's no sound output. If the emulation is too slow, the audio will be choppy. There's some high-frequency hiss I'd like to get rid of.

It's been a lot of fun writing this. In almost all cases I have worked from publicly available documents - the Hardware Reference Manual, online bits and pieces about future Amigas, the ATA mode 0 spec, the datasheets for the 68K series and the CIAs and clock chips. I don't have a real Amiga to hand, the trusty A500 my gran bought me in 1989 is trapped in storage somewhere in Australia. I booted it up a couple of years ago and it was still working then.

Anyway, Workbench is totally usable right now, and it plays a pretty mean Buggyboy and Pinball Fantasies. With a gigabyte of RAM.

Thanks to Toni Wilen and Brian King and all the other contributors for [WinUAE](https://www.winuae.net), the most complete Amiga emulation package. What a brilliant piece of software!
Also thanks to Petter Schau, Torsten Enderling and all the others for [WinFellow](http://petschau.github.io/WinFellow), which I have always admired for its simplicity and ability to play games.
Thanks to Karl Stenerud for the amazing [Musashi](https://github.com/kstenerud/Musashi) CPU emulation package.

Contributions, PRs and comments welcome!

Cheers,

Jim



