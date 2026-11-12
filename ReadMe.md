# Jammy - an Amiga emulator written in C#



[Jammy Releases](https://github.com/jimshawx/jammy/releases)



## Jim's Amiga Emulator

This is a lockdown project I started in December 2020 with the intention of building an Amiga emulation that could interface the UI with Windows instead of Workbench.
That quirky part of the project dropped away as soon as it became clear that you need to build a *really good* emulation first before you can run almost anything.
So here is my humble attempt at an Amiga emulation.



### The good stuff

* It's written in C#
* It runs faster than real Amigas (on my i5-8250 laptop)
* It can emulate most Amigas to at least some level

  * A1000, A500, A500+, A600, A1200, A2000, A3000, A4000, CD32
  * 68000 in C#, verified against the Musashi 68000 CPU
  * Other 680x0 using the Musashi CPU emulation, in C or C# or Moira CPU emulation in C
  * Cycle-exact 68000/chipset using the Moira CPU emulation, in C++
  * Copper, Blitter, CIA emulations
  * Sprite and Playfield collision
  * Sprite and Playfield priorities
  * Good quality Audio with filters
  * OCS, ECS, AGA emulation
  * Floppy Disks, ADF/Zipped ADF/ADZ/DMS/RP9/IPF (read) and ATA Hard Disks (read/write)
  * All kinds of RAM expansions (Chip, Trapdoor, CPU slot, Zorro II/III)
  * Battery-backed clock
  * VT100 Serial terminal
* There's a debugger and disassembler of sorts
* There's a Javascript and Lua API to enable smart code to run on breakpoints
* There's a web API so you can write your own UIs
* There's some automated code analysis to produce good disassemblies
* The disassemblies can be stored in a Sqlite database
* If you have a CPU which supports AVX2 your experience will be significantly improved
* There's a Linux version!
* There's a packaged release to try
* Completely new emulation not based on WinUAE
* It's free! (MIT License)



### The not so good stuff

* There are still a few problems with DMA timing and cycle exactness
* Emulation of CPUs running faster than the standard 7MHz is poor
* CD32 CDROM and NVRAM not working
* There are still some pieces of software that don't work yet
* The Linux version is missing the debugging UIs
* There's no ARM build
* The Javascript, Lua and web APIs need more work
* The database needs more work



## Instructions

For the best emulation for A500 and A1200, pick Moira CPU and add some Trapdoor RAM. Set floppy to Accurate and choose Immediate Blitter. Turn the Audio on.
If you want to use the debugger, tick the Debugging check box.

The joystick is hard-coded to use Up,Down,Left,Right and Z for fire.

Press F11 to take a snapshot of the emulation. When you next start the emulator the snapshot will appear in the snapshots dropdown on the settings screen.



## Debugging

A debug text log window appears when the emulation starts showing useful information about what the emulator is doing at that moment.

Also debug UI appears when the emulation starts. There are windows showing disassembly, memory and many registers and flags.
Using the buttons on the panel will let you stop,start and step through the emulation.
There are buttons to enable/disable/raise interrupts in both Paula and CIAs, and to unstick the blitter.
There are buttons to change the floppy disks, pick the radio button for the relevant drive and use Change to swap the disk. Use Insert to re-trigger the disk inserted flag.

'Analyze' will generate a graph of the currently executing code.

'Gen Disassemblies' will use all the gathered information from tracing the code and generate a disassembly of the kickstart ROM.

'Gfx Scan' opens a window where you can examine the memory as a graphic.

'String scan' opens a window where the entire memory is scanned for strings.

'DMA' opens a window showing the DMA usage for the previous frame.

'Crib Sheet' opens a useful window with documentation for all the Chipset and CIA registers.



Also on the panel there's a dialog box which accepts the following commands:
b address - breakpoint on execute at address
bw address \[size(W)] - breakpoint on write at address
br address \[size(W)] - breakpoint on read at address
brw address \[size(W)] - breakpoint on read/write at address
bc address - remove breakpoint at address
t address - toggle breakpoint at address
bl - list all breakpoints
d address \[length(1000h)] - add an address range to the debugger
m address \[length(1000h)] - add an address range to the memory dump
w address \[value(0)] \[size(W)] - write a value to memory
r address \[size(W)] - read a value from memory
a address \[type(C)] \[length(1)] - set memory type C,B,W,L,S,U
c address text - add a comment
h address text - add a header
g emulation Go
s emulation Step
so emulation Step Out
x emulation Stop
reg regname \[value(0)] - set a register

? display this help in the debug window



When the emulation window is active, press F9 to activate the debug keys.
Once active, use F6 and F7 to position a dotted line over the emulation display. This will cause the state of many chipset registers at that line to be overlayed on the screen. Press F8 to reset the line, and F9 again to turn off the debug keys.
There are many other debug keys active, and pressing some of them might corrupt the display.



## Notes

The C# 68000 is slightly faster than the C Musashi one, not because I have made any specific efforts to optimise it, but I think mostly because thunking out of C# into C and back again isn't particularly fast.
In an effort to fix this, I have ported Musashi and the Musashi CPU generator to C#, and it is significantly faster now.

The C and C# Musashi 68030/040 options support the 68881 and MMU instructions.

The audio is pretty good, if you run with it switched on it will lock the emulation performance down so the sample rate is exact. With it switched off, everything is still emulated but there's no sound output. If the emulation is too slow, the audio will be choppy.

It's been a lot of fun writing this. In almost all cases I have worked from publicly available documents - the Hardware Reference Manual, online bits and pieces about future Amigas, the ATA mode 0 spec, the datasheets for the 68K series and the CIAs and clock chips. I don't have a real Amiga to hand, the trusty A500 my gran bought me in 1989 is trapped in storage somewhere in Australia. I booted it up a couple of years ago and it was still working then.

Anyway, Workbench is totally usable right now, and it plays a pretty mean Buggyboy and Pinball Fantasies. With a gigabyte of RAM.

Thanks to Toni Wilen and Brian King and all the other contributors for [WinUAE](https://www.winuae.net), the most complete Amiga emulation package. What a brilliant piece of software!
Also thanks to Petter Schau, Torsten Enderling and all the others for [WinFellow](http://petschau.github.io/WinFellow), which I have always admired for its simplicity and ability to play games.
Thanks to Karl Stenerud for the amazing [Musashi](https://github.com/kstenerud/Musashi) CPU emulation package.
Thanks to Dirk W. Hoffmann for the superb [Moira](https://dirkwhoffmann.github.io/Moira/) CPU emulation package, and the incredibly useful [vAmiga Test Suite](https://github.com/dirkwhoffmann/vAmigaTS).
Thanks to Heikki Orsila and Andre Rodrigues de la Rocha for [xDMS](https://gitlab.com/heikkiorsila/xdms).

Thanks to everyone who has downloaded the releases to give them a try.



Contributions, PRs and comments welcome!



### Cheers!

### Jim

