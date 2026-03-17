# Purchase List (Round 2)
-	Silicone Cover Stranded-Core Wire - 2m 26AWG Orange × 1
-	Silicone Cover Stranded-Core Wire - 2m 26AWG Blue × 1
-	Silicone Cover Stranded-Core Wire - 2m 26AWG Yellow × 1
-	Silicone Cover Stranded-Core Wire - 2m 26AWG Black × 1
-	Silicone Cover Stranded-Core Wire - 2m 26AWG Red × 1
-	IC Socket - for 16-pin 0.3" Chips - Pack of 3 × 1
-	Assembled Pi Cobbler Plus - Breakout Cable (for Pi B+/A+/Pi 2/Pi 3/Pi 4/Pi 5) × 1
-	Universal Proto-board PCBs 4cm x 6cm - 3 Pack × 1
-	Raspberry Pi Micro SD Card with RPi OS Pre-Installed - 32GB × 1
-	2.13" ePaper Display HAT (250x122) × 1
-	Raspberry Pi Zero 2 W - Zero 2 W (with header) × 1
-	MCP3008 - 8-Channel 10-Bit ADC With SPI Interface × 1
-	120-Piece Ultimate Jumper Bumper Pack (Dupont Wire) × 1
-	Half-Size Breadboard - White × 1
-	DHT11 Temperature-Humidity Sensor × 1

# OS Install

I installed Raspberry Pi OS Lite to a micro-SD card and the Pi booted up with SSH available.

Getting all the optional low level system libraries installed is important because we are interested in building a native, high-performance binary instead of relying on python scripts.

Following the guide at: https://www.waveshare.com/wiki/2.13inch_e-Paper_HAT_Manual#Enable_SPI_Interface
Most things worked, except for "wiringpi" where I had to get the latest instructions from their repo at https://github.com/WiringPi/WiringPi

The wiringpi package provides a really nice gpio command line utility:
```
josh@pi1:~/WiringPi $ gpio readall
 +-----+-----+---------+------+---+Pi Zero 2W+---+------+---------+-----+-----+
 | BCM | wPi |   Name  | Mode | V | Physical | V | Mode | Name    | wPi | BCM |
 +-----+-----+---------+------+---+----++----+---+------+---------+-----+-----+
 |     |     |    3.3v |      |   |  1 || 2  |   |      | 5v      |     |     |
 |   2 |   8 |   SDA.1 |   IN | 1 |  3 || 4  |   |      | 5v      |     |     |
 |   3 |   9 |   SCL.1 |   IN | 1 |  5 || 6  |   |      | 0v      |     |     |
 |   4 |   7 | GPIO. 7 |   IN | 1 |  7 || 8  | 1 | ALT5 | TxD     | 15  | 14  |
 |     |     |      0v |      |   |  9 || 10 | 1 | ALT5 | RxD     | 16  | 15  |
 |  17 |   0 | GPIO. 0 |   IN | 0 | 11 || 12 | 0 | IN   | GPIO. 1 | 1   | 18  |
 |  27 |   2 | GPIO. 2 |   IN | 0 | 13 || 14 |   |      | 0v      |     |     |
 |  22 |   3 | GPIO. 3 |   IN | 0 | 15 || 16 | 0 | IN   | GPIO. 4 | 4   | 23  |
 |     |     |    3.3v |      |   | 17 || 18 | 0 | IN   | GPIO. 5 | 5   | 24  |
 |  10 |  12 |    MOSI | ALT0 | 0 | 19 || 20 |   |      | 0v      |     |     |
 |   9 |  13 |    MISO | ALT0 | 0 | 21 || 22 | 0 | IN   | GPIO. 6 | 6   | 25  |
 |  11 |  14 |    SCLK | ALT0 | 0 | 23 || 24 | 1 | OUT  | CE0     | 10  | 8   |
 |     |     |      0v |      |   | 25 || 26 | 1 | OUT  | CE1     | 11  | 7   |
 |   0 |  30 |   SDA.0 |   IN | 1 | 27 || 28 | 1 | IN   | SCL.0   | 31  | 1   |
 |   5 |  21 | GPIO.21 |   IN | 1 | 29 || 30 |   |      | 0v      |     |     |
 |   6 |  22 | GPIO.22 |   IN | 1 | 31 || 32 | 0 | IN   | GPIO.26 | 26  | 12  |
 |  13 |  23 | GPIO.23 |   IN | 0 | 33 || 34 |   |      | 0v      |     |     |
 |  19 |  24 | GPIO.24 |   IN | 0 | 35 || 36 | 0 | IN   | GPIO.27 | 27  | 16  |
 |  26 |  25 | GPIO.25 |   IN | 0 | 37 || 38 | 0 | IN   | GPIO.28 | 28  | 20  |
 |     |     |      0v |      |   | 39 || 40 | 0 | IN   | GPIO.29 | 29  | 21  |
 +-----+-----+---------+------+---+----++----+---+------+---------+-----+-----+
 | BCM | wPi |   Name  | Mode | V | Physical | V | Mode | Name    | wPi | BCM |
 +-----+-----+---------+------+---+Pi Zero 2W+---+------+---------+-----+-----+
```

# Language
Now that I have the C libraries installed for the e-ink display, my language of choice is Rust. https://rust-lang.org
Rust has quickly become the favourite programing language of many developers over the past few years. It is said to be as fast as C++ (kornel, 2026 https://kornel.ski/rust-c-speed) but with extra memory safety and a LOT of libraries (known as cargo packages) ready to use.
It is a quick way to develop a low level, high performance application without doing too much of the low-level coding typically needed with such an undertaking.

# AI Disclaimer
I used Claude Code to generate the boilerplate code conversion from the example C library provided by WaveShare to Rust code.
This provides us with a working example written in Rust, ready to be adapted for our project.
