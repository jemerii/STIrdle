using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;

namespace STIrdle
{
    internal class Program
    {
        // Import Win32 API functions
        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        // Window style constants
        const int GWL_STYLE = -16;
        const int WS_SIZEBOX = 0x00040000;
        const int WS_MAXIMIZEBOX = 0x00010000;
        const int WS_MINIMIZEBOX = 0x00020000;

        // I have no idea what this is for
        static readonly object consoleLock = new object();


        // Screens
        static void Main()
        {
            int H = 30;
            int W = 80;
            Console.SetWindowSize(W, H);
            Console.SetBufferSize(W, H);
            Console.CursorVisible = false;

            IntPtr handle = GetConsoleWindow();
            DisableResize(handle);

            while (true) // <-- keep the menu active
            {
                Console.Clear();
                DrawBanner(banner, 5, ConsoleColor.DarkCyan, ConsoleColor.Yellow);
                Console.ResetColor();

                Console.SetCursorPosition(0, 15);
                PrintCentered("[1] ", " S T A R T", ConsoleColor.White);
                PrintCentered("[2] ", " H E L P _", ConsoleColor.White);
                PrintCentered("[3] ", " C R E D S", ConsoleColor.White);
                Console.Write("\n");
                PrintCentered("[X] ", " E X I T _", ConsoleColor.Red);

                Console.SetCursorPosition(0, 25);
                PrintCentered("(C) ", "2025 CASTech", ConsoleColor.White);

                ConsoleKey key = Console.ReadKey(true).Key;
                switch (key)
                {
                    case ConsoleKey.D1:
                    case ConsoleKey.NumPad1:
                        FlashLine(15, "[1]  S T A R T", ConsoleColor.Green, 1000, 150);
                        StartGame();
                        break;

                    case ConsoleKey.D2:
                    case ConsoleKey.NumPad2:
                        FlashLine(16, "[2]  H E L P _", ConsoleColor.Green, 1000, 150);
                        Instructions();
                        break;

                    case ConsoleKey.D3:
                    case ConsoleKey.NumPad3:
                        FlashLine(17, "[3]  C R E D S", ConsoleColor.Green, 1000, 150);
                        Credits();
                        break;

                    case ConsoleKey.X:
                        FlashLineAsync(19, "[X]  Y / N _ _", ConsoleColor.Red, 300);
                        var confirmKey = Console.ReadKey(true).Key;
                        if (confirmKey == ConsoleKey.Y)
                        {
                            StopFlashing();
                            Thread.Sleep(300);
                            Console.SetCursorPosition(0, 19);
                            Console.ResetColor();
                            PrintCentered("[X]  C I A O !");
                            Thread.Sleep(750);
                            Exit();
                            break;
                        }
                        else
                        {
                            StopFlashing();
                            Thread.Sleep(300);
                            break;
                        }

                }
            }
        }

        static void StartGame()
        {
            Console.CursorVisible = false;
            Stopwatch timer = new Stopwatch();

            string[] secretWord = GetWordLibrary();

            // Build a list of only words
            List<string> wordList = new List<string>();
            for (int i = 0; i < secretWord.Length; i += 2)
                wordList.Add(secretWord[i]);

            Random rand = new Random();
            string findsecretWord = wordList[rand.Next(wordList.Count)].ToUpper();

            // Get its original index in secretWord
            int secretIndex = Array.IndexOf(secretWord, findsecretWord);

            // Grab the definition
            string definition = secretWord[secretIndex + 1];

            int maxAttempts = 6;
            int attemptIndex = 0;

            Console.CursorVisible = false;
            Console.Clear();
            DrawBanner(banner, 3, ConsoleColor.DarkCyan, ConsoleColor.Yellow);

            PrintCentered("[1] ", "Reveal Definition", ConsoleColor.White);

            int resultsTop = 13;
            int vertical = 25;
            int hangmanLeft = findsecretWord.Length * 3 + 30;

            // Reserve guess space
            for (int i = 0; i < maxAttempts; i++)
            {
                Console.SetCursorPosition(0, resultsTop + i);
                Console.Write(new string(' ', findsecretWord.Length * 2));
            }

            DrawHangman(hangmanStages[0], hangmanLeft, resultsTop);

            while (attemptIndex < maxAttempts)
            {
                char[] currentGuess = new char[findsecretWord.Length];
                int charIndex = 0;

                // Draw initial blank guess row
                Console.SetCursorPosition(vertical, resultsTop + attemptIndex);
                Console.Write(string.Join(" ", new string('_', findsecretWord.Length).ToCharArray()));

                bool firstInput = true;

                Thread timerThread = new Thread(() =>
                {
                    while (true)
                    {
                        if (timer.IsRunning)
                        {
                            PrintAtLine(25, $"Time: {timer.Elapsed.TotalSeconds:F0}s", ConsoleColor.Cyan);
                        }
                        Thread.Sleep(1000);
                    }
                });
                timerThread.IsBackground = true;
                timerThread.Start();

                // Per-letter input
                while (true)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    if (firstInput)
                    {
                        timer.Start();
                        firstInput = false;
                        PrintAtLine(25, $"Time: {timer.Elapsed.TotalSeconds:F0}s", ConsoleColor.Cyan);
                    }

                    if (key.Key == ConsoleKey.D1 || key.Key == ConsoleKey.NumPad1)
                    {
                        Console.SetCursorPosition(0, 10);
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write(new string(' ', Console.WindowWidth));
                        Console.SetCursorPosition(0, 10);
                        Console.Write($"// Definition: {definition}");
                        Console.ResetColor();
                    }
                    else if (key.Key == ConsoleKey.Enter)
                    {
                        // Only allow Enter if full word is typed
                        if (charIndex == findsecretWord.Length) break;
                        else continue;
                    }
                    else if (key.Key == ConsoleKey.Backspace)
                    {
                        if (charIndex > 0)
                        {
                            charIndex--;
                            currentGuess[charIndex] = '\0';
                            Console.SetCursorPosition(vertical + charIndex * 2, resultsTop + attemptIndex);
                            Console.Write("_ ");
                            Console.SetCursorPosition(vertical + charIndex * 2, resultsTop + attemptIndex);
                        }
                    }
                    else if (char.IsLetter(key.KeyChar))
                    {
                        if (charIndex < findsecretWord.Length)
                        {
                            char letter = char.ToUpper(key.KeyChar);
                            currentGuess[charIndex] = letter;
                            Console.SetCursorPosition(vertical + charIndex * 2, resultsTop + attemptIndex);
                            Console.Write(letter);
                            charIndex++;
                        }
                    }
                }

                string guessWord = new string(currentGuess);

                // Compare with secret
                ConsoleColor[] colors = new ConsoleColor[findsecretWord.Length];
                bool[] matched = new bool[findsecretWord.Length];

                for (int i = 0; i < findsecretWord.Length; i++)
                {
                    if (guessWord[i] == findsecretWord[i])
                    {
                        colors[i] = ConsoleColor.Green;
                        matched[i] = true;
                    }
                }

                for (int i = 0; i < findsecretWord.Length; i++)
                {
                    if (colors[i] == ConsoleColor.Green) continue;
                    bool found = false;

                    for (int j = 0; j < findsecretWord.Length; j++)
                    {
                        if (!matched[j] && guessWord[i] == findsecretWord[j])
                        {
                            matched[j] = true;
                            found = true;
                            break;
                        }
                    }

                    colors[i] = found ? ConsoleColor.Yellow : ConsoleColor.DarkGray;
                }

                // Redraw guess with colors
                Console.SetCursorPosition(vertical, resultsTop + attemptIndex);
                for (int i = 0; i < findsecretWord.Length; i++)
                {
                    Console.ForegroundColor = colors[i];
                    Console.Write(guessWord[i] + " ");
                }
                Console.ResetColor();

                if (guessWord == findsecretWord)
                {
                    timer.Stop();
                    int hangmanHeight = Math.Min(attemptIndex, hangmanStages.Length - 2);
                    Console.SetCursorPosition(0, resultsTop + maxAttempts + 2);
                    Console.WriteLine($"You got it! The word was {findsecretWord}, and you took {timer.Elapsed.TotalSeconds:F2} seconds!");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"> Definition: {definition}\n");
                    Console.ResetColor();

                    PrintCentered("[2] ", " R E T R Y", ConsoleColor.White);
                    PrintCentered("[3] ", " B A C K _", ConsoleColor.White);

                    while (true)
                    {
                        ConsoleKey key = Console.ReadKey(true).Key;

                        if (key == ConsoleKey.D2 || key == ConsoleKey.NumPad2)
                        {
                            StartGame(); // Retry the same game logic
                            break;
                        }
                        else if (key == ConsoleKey.D3 || key == ConsoleKey.NumPad3)
                        {
                            Main(); // Go back to main menu
                            break;
                        }
                    }
                }

                attemptIndex++;
                DrawHangman(hangmanStages[attemptIndex], hangmanLeft, resultsTop); ;
            }

            if (attemptIndex == maxAttempts)
            {
                timer.Stop();
                Console.SetCursorPosition(0, resultsTop + attemptIndex + 2);
                Console.WriteLine($"Out of tries! The word was {findsecretWord}, and you took {timer.Elapsed.TotalSeconds:F2} seconds!");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"// Definition: {definition}\n");
                Console.ResetColor();

                PrintCentered("[2] ", " R E T R Y", ConsoleColor.White);
                PrintCentered("[3] ", " B A C K _", ConsoleColor.White);

                while (true)
                {
                    ConsoleKey key = Console.ReadKey(true).Key;

                    if (key == ConsoleKey.D2 || key == ConsoleKey.NumPad2)
                    {
                        StartGame(); // Retry the same game logic
                        break;
                    }
                    else if (key == ConsoleKey.D3 || key == ConsoleKey.NumPad3)
                    {
                        Main(); // Go back to main menu
                        break;
                    }
                }
            }
            Console.ReadKey();
        }

        static void Instructions()
        {
            Console.CursorVisible = false;
            Console.Clear();
            DrawBanner(banner, 3, ConsoleColor.DarkCyan, ConsoleColor.Yellow);

            Console.SetCursorPosition(0, 11);
            PrintCentered("STIrdle is a Wordle-like game where your goal is to guess");
            PrintCentered("a hidden 5-letter word within 6 attempts.");
            PrintCentered("After each guess, you'll receive color clues indicated by the");
            PrintCentered("text color of each letter:\n");

            PrintCentered("Green ", "letters indicate a letter in the correct position", ConsoleColor.Green);
            PrintCentered("Yellow ", "letters indicate a letter that's present, and lastly...", ConsoleColor.Yellow);
            PrintCentered("Gray ", "letters indicate a letter that's absent from the solution word\n", ConsoleColor.DarkGray);

            PrintCentered("Use these clues to narrow down your guesses and find the");
            PrintCentered("secret word before you run out of tries!\n\n");

            PrintCentered("[1]  B A C K _");
            while (true)
            {
                ConsoleKey key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.D1 || key == ConsoleKey.NumPad1)
                {
                    Main();
                    break;
                }
            }
        }

        static void Credits()
        {
            Console.CursorVisible = false;
            Console.Clear();
            DrawBanner(banner, 3, ConsoleColor.DarkCyan, ConsoleColor.Yellow);

            PrintCentered("Gameplay, Programmer, Project Lead");
            Console.ForegroundColor = ConsoleColor.Green;
            PrintCentered("// Jeremy Charle Barrera");
            PrintCentered("// \"nunquam minus solus, quam cum solus\"\n");

            Console.ResetColor();

            PrintCentered("UI, Gameplay Assistant, Programmer");
            Console.ForegroundColor = ConsoleColor.Green;
            PrintCentered("// Steffano Andrei Alvaran");
            PrintCentered("// \"It's a Landmark!\"\n");
            Console.ResetColor();

            PrintCentered("Special Thanks, Verbal Assistance");
            Console.ForegroundColor = ConsoleColor.Green;
            PrintCentered("// @acquiescentmind");
            PrintCentered("// \"Play maimai DX PRiSM Plus NOW!\"\n");
            Console.ResetColor();

            PrintCentered("Bystander on-call");
            Console.ForegroundColor = ConsoleColor.Green;
            PrintCentered("// @siritoriyowaifan3470");
            PrintCentered("// \"Eclipse first, the rest nowhere.\"\n");
            Console.ResetColor();

            PrintCentered("[1]  B A C K _");
            while (true)
            {
                ConsoleKey key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.D1 || key == ConsoleKey.NumPad1)
                {
                    Main();
                    break;
                }
            }
        }

        // Common Variables
        public static string[] GetWordLibrary()
        {
            return new string[]
            {
                "ABORT", "Terminate or stop a running process before it completes, often used when a program encounters an error or needs to be forcefully ended.",
                "AUDIO", "Refers to sound data, signals, or output produced by a computer system, such as music, voice, or sound effects.",
                "ALERT", "A system-generated notification or warning that requires user attention, often for errors, security, or important status changes.",
                "ASCII", "American Standard Code for Information Interchange – a character encoding standard used to represent text in computers using numeric codes.",
                "ARIAL", "A widely used sans-serif font type, common in documents, software, and web design due to its readability.",
                "ASSET", "A digital resource such as an image, sound, model, or file that is used within software, games, or applications.",
                "AUTHS", "Short for 'authentications', referring to systems or methods used to verify identity and grant access to secure resources.",
                "ALLOW", "Grant permission for an action or process to occur, often controlled by user settings, security policies, or system rules.",
                "ADDON", "An extra feature, plugin, or extension that adds new functionality to a program, browser, or game.",
                "APPLE", "A major technology company known for its hardware (iPhone, Mac, iPad) and software (iOS, macOS, iCloud).",
                "BASIC", "Beginner’s All-purpose Symbolic Instruction Code – one of the earliest and simplest programming languages designed for ease of learning.",
                "BATCH", "A collection of commands or jobs executed automatically in sequence, often using batch files or scripts.",
                "BYTES", "A fundamental unit of digital data, typically 8 bits, capable of representing 256 unique values such as text characters.",
                "BLOCK", "A contiguous chunk of data stored or transmitted as a unit; also refers to grouped code or blockchain segments.",
                "BOARD", "Short for 'circuit board', the hardware component that connects and supports electronic parts like CPUs, RAM, and storage.",
                "BUILD", "The process of compiling and assembling source code into an executable program or application.",
                "BUGGY", "Describes software or hardware that contains many errors, glitches, or malfunctions.",
                "BINDS", "Keyboard shortcuts, key bindings, or control mappings that link specific actions to inputs.",
                "BENCH", "Short for 'benchmark', a performance test used to evaluate the speed or efficiency of hardware or software.",
                "BUSES", "Communication pathways that transfer data between components inside a computer, such as between the CPU and memory.",
                "CACHE", "A small, high-speed storage layer that temporarily holds frequently used data for quick access.",
                "CABLE", "A physical medium (wired connection) used to transfer power or data between devices.",
                "CDROM", "Compact Disc Read-Only Memory – an optical storage medium used to distribute software, music, or data.",
                "CHMOD", "A Unix/Linux command used to change file permissions, controlling who can read, write, or execute a file.",
                "CHIPS", "Integrated circuits that contain millions of transistors, forming the brains of computers and other electronics.",
                "CHUNK", "A segment or piece of data processed separately, often used in networking, memory allocation, or file storage.",
                "CLICK", "The act of pressing a mouse button or touchscreen, often used to trigger actions in graphical interfaces.",
                "CLOUD", "Remote servers accessed over the internet that provide storage, computing power, or services instead of local hardware.",
                "CLONE", "To duplicate data, a program, or hardware exactly; also refers to software repositories copied from a source.",
                "CODEC", "Short for 'coder-decoder', a program or device that compresses and decompresses audio or video files.",
                "COBOL", "Common Business-Oriented Language – a programming language designed for business and administrative systems.",
                "COMMS", "Short for 'communications', referring to the exchange of data between systems, networks, or devices.",
                "CRACK", "To bypass or disable security features, often illegally, in order to gain access to protected software or systems.",
                "CRASH", "A failure in software or hardware that causes the system or program to stop functioning abruptly.",
                "CRYPT", "A shortened form of 'encryption' or 'cryptography', the practice of securing data through coded algorithms.",
                "CYCLE", "A repeated loop or process, often referring to CPU clock cycles or control flow in programming.",
                "CYBER", "Relating to digital technology, the internet, or virtual environments (e.g., cybersecurity, cyberspace).",
                "CORES", "Individual processing units inside a CPU that allow parallel computing and multitasking.",
                "COUNT", "A function or process of calculating the number of items, values, or operations.",
                "CURVE", "A smooth line or function often used in graphics, math operations, or data plotting.",
                "CISCO", "A major networking company that produces routers, switches, and enterprise communication systems.",
                "DEBUG", "The process of identifying and fixing errors or 'bugs' in software or hardware systems.",
                "DISKS", "Storage devices such as hard drives, SSDs, or floppy disks used to store data permanently.",
                "DRIVE", "A general term for storage devices like hard drives, SSDs, or external drives.",
                "DATUM", "A single piece of information; the singular form of 'data'.",
                "DELAY", "A pause or wait state in processing, networking, or hardware signaling.",
                "DELTA", "Represents a change or difference, commonly used in version control, math, or updates.",
                "DEPTH", "Often refers to bit depth in images or color depth in displays, defining how much detail is possible.",
                "DWORD", "Double word – a data type that is usually 32 bits in length.",
                "DRAMS", "Dynamic RAM chips, commonly used for main system memory in computers.",
                "DRONE", "Unmanned aerial vehicle (UAV) that can be controlled remotely, often enhanced with ICT components like GPS and cameras.",
                "EMAIL", "Electronic mail – digital communication exchanged over the internet.",
                "EMBED", "To insert data, media, or code directly within another document or program.",
                "EBOOK", "Electronic book – a digital version of text that can be read on devices like Kindles, tablets, or PCs.",
                "EPOCH", "A reference point in time used by computers to measure time, e.g., Unix epoch (Jan 1, 1970).",
                "EPROM", "Erasable Programmable Read-Only Memory – a type of memory chip that can be erased with UV light and rewritten.",
                "ETHER", "Short for Ethernet, a wired networking technology used for local area networks (LANs).",
                "EVENT", "An action or occurrence detected by software, often used in event-driven programming.",
                "ERROR", "A problem or incorrect result in computing, which may stop execution or require correction.",
                "EXCEL", "Microsoft Excel – spreadsheet software widely used for calculations, data visualization, and analysis.",
                "EXECS", "Short for 'executables', referring to runnable program files.",
                "EXITS", "Points at which a program or function stops execution, or system calls to terminate.",
                "ENUMS", "Short for enumerations – a programming data type that consists of named values.",
                "FLASH", "Non-volatile memory used for storage; also Adobe Flash (now deprecated software).",
                "FIBER", "Fiber optic cable used in high-speed networking to transmit data using light.",
                "FIBRE", "Alternate spelling of fiber, commonly used in UK English.",
                "FILES", "Collections of data stored in a computer system, typically with a filename and extension.",
                "FILER", "A program or tool for managing files, or a user who submits a request/report.",
                "FORUM", "An online discussion board where users post and reply to messages.",
                "FORMS", "Input fields in web pages or software that collect user data.",
                "FETCH", "The step in CPU operation where instructions or data are retrieved from memory.",
                "FRAME", "A single image in a sequence of video; also refers to structures in networking packets.",
                "FIFOS", "First-In-First-Out data structures or file pipes in Unix systems.",
                "FUSER", "A command in Unix/Linux to identify processes using a file or socket.",
                "FONTS", "Sets of text characters designed in a particular style and size.",
                "FATAL", "A severe error that causes a program to terminate immediately.",
                "FINDS", "Searches for files or patterns, often via the Unix 'find' command.",
                "FLOOD", "An overwhelming number of requests sent to a system, often in denial-of-service attacks.",
                "FLOPS", "Floating Point Operations Per Second – a measure of computer performance.",
                "FORKS", "In Unix, to duplicate a process; in software projects, to branch or copy a repository.",
                "FRAGS", "Data fragments; also slang for successful kills in online games.",
                "FRONT", "The 'front end' of an application or system, which users interact with.",
                "FUSES", "Protective components that break a circuit when overloaded; also 'fuse' libraries in software.",
                "FDISK", "A disk partitioning utility used in DOS/Windows systems.",
                "FAULT", "An error or issue in hardware or software, such as a segmentation fault.",
                "FOCUS", "Refers to the currently active window or input area on a computer screen.",
                "FLAGS", "Binary indicators used in programming and CPU instructions to represent states or options.",
                "GATES", "Logic gates in circuits, which perform boolean operations like AND, OR, NOT.",
                "GLIDE", "3dfx Glide – a graphics API for rendering 3D graphics (90s era).",
                "GNOME", "A popular Linux desktop environment with a graphical user interface.",
                "GROUP", "A collection of users, processes, or resources managed as a unit in software or networking.",
                "GRANT", "To provide permission or rights, often in databases or operating systems.",
                "GRAPH", "A data structure made of nodes and edges; also used for charts and plots.",
                "GRIDS", "Layouts dividing space into structured areas; also used in high-performance 'grid computing'.",
                "GRUBS", "GNU GRUB (bootloader) plural, referring to software that loads operating systems at startup.",
                "GUIDE", "Documentation or interactive help that explains how to use software or systems.",
                "GAMER", "A person who plays video games; also refers to hardware marketed for gaming.",
                "GAMMA", "A measure of brightness in displays and images.",
                "GLYPH", "A visual representation of a character in a font.",
                "GMAIL", "Google’s email service.",
                "HACKS", "Clever or unauthorized modifications to hardware or software.",
                "HOSTS", "Files or entries that map domain names to IP addresses; also refers to computers in a network.",
                "HEAPS", "Data structures used for memory management, or regions of dynamic memory allocation.",
                "HTMLS", "HyperText Markup Language files used for structuring web pages.",
                "HEXES", "Hexadecimal values, base-16 representations of numbers.",
                "HTTPS", "Secure HyperText Transfer Protocol, encrypted communication over the web.",
                "ICONS", "Small graphical symbols representing files, apps, or actions.",
                "INPUT", "Data provided to a computer or program, usually by the user.",
                "INDEX", "A data structure for fast lookups; also an HTML index page.",
                "INODE", "An index node in Unix/Linux file systems, storing file metadata.",
                "INTEL", "A major semiconductor company best known for producing CPUs.",
                "IMAGE", "A digital picture; in computing, also a disk image or system snapshot.",
                "IMACS", "Apple’s line of desktop computers.",
                "IONIC", "A framework for building cross-platform mobile apps.",
                "ISCSI", "Internet Small Computer Systems Interface – a protocol for linking storage devices over networks.",
                "IPSEC", "Internet Protocol Security – a protocol suite for securing IP communications.",
                "INFOS", "General shorthand for 'information'.",
                "ISSUE", "A problem or task tracked in software development tools like GitHub.",
                "JSONS", "JavaScript Object Notation files or objects, widely used for data exchange.",
                "JAVAS", "Short for JavaScript, a programming language of the web.",
                "JUICE", "Slang for battery power; also used to describe system resources.",
                "JUMPS", "Programming jumps in assembly or flow control.",
                "JPEGS", "Image files compressed using the JPEG standard.",
                "JOLTS", "Slang for sudden spikes in electricity or computing load.",
                "JUNIT", "A unit testing framework for Java programs.",
                "KPROC", "Kernel process in Unix/Linux systems.",
                "LOGIN", "The process of entering credentials to access a computer system.",
                "LOGON", "Similar to login; often used in Windows systems.",
                "LINUX", "A family of open-source Unix-like operating systems.",
                "LIBRE", "Short for LibreOffice – an open-source office suite.",
                "LANES", "Communication pathways, often referring to PCIe lanes on a motherboard.",
                "LATCH", "A circuit component that stores a state until changed by input.",
                "LAYER", "A level in networking, graphics, or architecture that separates functionality.",
                "LEARN", "General reference to machine learning or tutorials.",
                "LINKS", "References to other resources, such as hyperlinks on the web.",
                "LOOPS", "Programming constructs that repeat actions until a condition is met.",
                "LOADS", "To bring a program or data into memory for execution.",
                "LOGIC", "The structured rules and operations that govern how software or hardware works.",
                "LOCAL", "Refers to data, users, or systems confined to one machine or network.",
                "LOWER", "Programming function to convert text to lowercase; also refers to lower memory regions.",
                "LUMIX", "Panasonic’s digital camera brand.",
                "LIDAR", "Light Detection and Ranging – a sensing technology used in autonomous vehicles and mapping.",
                "LUNAR", "Used metaphorically in naming (like Lunar Linux); sometimes refers to cycles or themes in ICT.",
                "MACRO", "A sequence of commands or keystrokes automated into one shortcut.",
                "MACOS", "Apple’s operating system for Mac computers.",
                "MEDIA", "Refers to digital content like audio, video, or images.",
                "MERGE", "Combining code, data, or files into one unified set.",
                "MODAL", "A window or interface that takes focus and requires user interaction.",
                "MODEL", "In machine learning, a trained representation used to make predictions.",
                "MODES", "Different states of operation, like safe mode, edit mode, or command mode.",
                "MOUNT", "To attach a file system or storage device to an operating system.",
                "MOUSE", "A pointing device used to interact with graphical interfaces.",
                "MOVED", "Files or processes that have been relocated in storage or memory.",
                "MOVER", "A utility or function that relocates files or data.",
                "MSDOS", "Microsoft Disk Operating System, widely used in early PCs.",
                "MYSQL", "An open-source relational database management system.",
                "MAILS", "Electronic mail messages.",
                "MATCH", "A found result that corresponds to a pattern or query.",
                "NODES", "Connection points in a network or elements in a data structure like a graph.",
                "NANOS", "Nanoseconds, often used in CPU timing or precision measurement.",
                "NEONS", "Refers to Neon SIMD instructions used in ARM processors.",
                "NEXUS", "A line of Android devices from Google; also refers to a central hub or link.",
                "NETFX", ".NET Framework – Microsoft’s software development framework.",
                "NGINX", "A high-performance open-source web server and reverse proxy.",
                "NOOBS", "New Out Of Box Software – a Raspberry Pi OS installer; also slang for beginners.",
                "OAUTH", "An open standard authorization protocol used for secure login and data sharing.",
                "OCTAL", "Number system with base 8, used in computing and Unix permissions.",
                "OPNET", "Network simulation software used in research and education.",
                "OPENS", "General term for open-source or opening files.",
                "ORBIT", "Refers to orbital paths in simulations or software names.",
                "OSCAR", "Open System for Communication in Realtime (messaging protocol).",
                "OSINT", "Open Source Intelligence – the practice of gathering data from publicly available sources.",
                "OXIDE", "Used in names like Rust or Chromium Oxide; also refers to semiconductor layers.",
                "PATCH", "A software update that fixes bugs or security issues.",
                "PINGS", "Network diagnostic requests to test connectivity between computers.",
                "PASTE", "To insert copied data from the clipboard.",
                "PATHS", "Directory or file locations in a computer system.",
                "PHPBB", "An open-source forum software package.",
                "PIPES", "Unix mechanism for inter-process communication, sending data between commands.",
                "PIXEL", "The smallest unit of a digital image or display.",
                "PLUGS", "Hardware connectors or software plugins/extensions.",
                "PORTS", "Communication endpoints for networking or physical connectors.",
                "POSIX", "Portable Operating System Interface – a standard for Unix-like systems.",
                "PRINT", "To produce output on paper or screen; also a programming function.",
                "PROXY", "A server that acts as an intermediary for requests between a client and another server.",
                "QUERY", "A request for data from a database or search engine.",
                "QUBIT", "Quantum bit – the fundamental unit of quantum computing.",
                "QUADS", "Groups of four; often refers to quad-core processors.",
                "QUEUE", "A data structure or process where items are handled in order, typically First-In-First-Out.",
                "QSTAT", "A command-line tool for checking job queue status in computing clusters.",
                "RANDS", "Random values generated for use in programming or simulations.",
                "REACT", "A JavaScript library for building interactive user interfaces.",
                "REPOS", "Repositories – storage locations for software code and version control.",
                "RESET", "To restore a system or device to its initial state.",
                "RETRY", "To attempt an operation again after failure.",
                "ROBOT", "A programmable machine capable of performing automated tasks.",
                "ROUTE", "The path taken by data packets across a network.",
                "SHELL", "A command-line interface for interacting with an operating system.",
                "SHIFT", "A keyboard key modifier or bitwise operation in programming.",
                "SHARE", "To distribute files, resources, or access rights with others.",
                "SPOOL", "Simultaneous Peripheral Operations On-Line – managing queued tasks like print jobs.",
                "STACK", "A Last-In-First-Out data structure or the call stack in programs.",
                "STATE", "The condition or status of a system, variable, or machine at a given time.",
                "STATS", "Statistical data used for analysis or monitoring.",
                "STORE", "To save data in memory, databases, or files.",
                "SYNCS", "Processes to keep data consistent across devices or systems.",
                "SWIFT", "A programming language by Apple for iOS and macOS development.",
                "SWIPE", "A touchscreen gesture to move items or navigate.",
                "SWAPS", "Exchange of memory pages or resources between disk and RAM.",
                "SCALA", "A programming language combining object-oriented and functional programming concepts.",
                "SCANS", "The process of examining data, files, or hardware for issues, viruses, or content.",
                "SAVES", "Stored progress or data in software or games.",
                "SIGNS", "Symbols or indicators used in computing, math, or interfaces.",
                "SORTS", "Arranging data into a specific order, such as alphabetical or numerical.",
                "SPAMS", "Unwanted or unsolicited digital messages, often emails.",
                "SPECS", "Specifications that define hardware or software capabilities.",
                "SQLDB", "Structured Query Language database, used for managing relational data.",
                "SQUAD", "A small group of users or team in games or applications.",
                "STYLE", "Visual appearance settings, like CSS for web design.",
                "STEAM", "A digital distribution platform for video games.",
                "SUPER", "Often refers to the 'superuser' with administrative privileges.",
                "STARS", "Objects in space; also GitHub stars marking project popularity.",
                "TABLE", "Structured data arrangement in rows and columns.",
                "TAPES", "Magnetic storage media for archiving data.",
                "TASKS", "Individual jobs or processes managed by an operating system.",
                "TERMS", "Conditions of use or agreements in software and online services.",
                "THEME", "A set of visual customizations for software or systems.",
                "TOKEN", "A digital identifier for authentication, security, or blockchain systems.",
                "TOOLS", "Software utilities that help perform tasks or manage systems.",
                "TRACE", "To follow program execution step by step, often for debugging.",
                "TRACK", "To follow changes, positions, or states of data, users, or media.",
                "TREND", "Patterns of data or usage behavior over time.",
                "TRUST", "Reliance on verified systems, certificates, or user permissions.",
                "TUNER", "A device or software for adjusting frequencies or settings.",
                "TUNES", "Music files or audio tracks.",
                "TURBO", "Refers to turbo mode in CPUs, providing short bursts of higher performance.",
                "TWEAK", "Small adjustments or optimizations to improve performance or behavior.",
                "UNION", "SQL operation combining results of multiple queries; also refers to grouping.",
                "UNITS", "Individual items of measurement or objects in software or games.",
                "UPLAY", "Ubisoft’s online gaming service and client.",
                "UTILS", "Short for utilities – small helper programs that perform tasks.",
                "USAGE", "The way resources, software, or systems are consumed or utilized.",
                "USERS", "People interacting with a system or application.",
                "VALUE", "The assigned data or content of a variable, key, or parameter.",
                "VAULT", "A secure storage location for sensitive files, passwords, or keys.",
                "VIRAL", "Content that spreads rapidly across the internet, often through social sharing.",
                "VIRUS", "Malicious software designed to damage or disrupt systems.",
                "VISTA", "Microsoft Windows Vista – an operating system released in 2007.",
                "VITAL", "Essential or critical components, processes, or data.",
                "VLANS", "Virtual Local Area Networks, used to logically separate network segments.",
                "VOLTS", "Electrical potential difference, used in hardware specifications.",
                "VRAMS", "Video Random Access Memory used in graphics cards for rendering images.",
                "VSYNC", "Vertical synchronization – a display option that matches frame rate with monitor refresh rate.",
                "WIFIS", "Wireless networking standards that allow devices to connect without cables.",
                "WINCE", "Windows CE – a compact operating system by Microsoft.",
                "WINNT", "Windows NT – an operating system family with enterprise features.",
                "WINME", "Windows Millennium Edition, an operating system released in 2000.",
                "WINXP", "Windows XP – one of Microsoft’s most popular operating systems.",
                "WIRED", "Refers to physical, cable-based networking connections.",
                "WORDS", "Data unit sizes in CPUs; also text made of characters.",
                "WORLD", "Virtual environments in games or simulations.",
                "WRITE", "The process of saving data to storage or memory.",
                "WRIST", "Often used in wearable tech, like wrist-mounted smart devices.",
                "XEROS", "Likely a play on 'Xerox', a company known for printers and copiers.",
                "XARGS", "A Unix command to build and execute commands using input arguments.",
                "XCODE", "Apple’s official integrated development environment (IDE) for macOS and iOS development.",
                "XENON", "A gas used in lighting; also the codename for Xbox 360’s CPU.",
                "XHTML", "Extensible HyperText Markup Language, a stricter form of HTML.",
                "YAHOO", "An internet company known for its search engine, email, and media services.",
                "YAMLS", "YAML files, a human-readable data serialization format often used in configurations.",
                "YIELD", "A programming keyword in some languages (e.g., Python) that returns control but saves state for resuming later.",
                "YOTTA", "The largest SI unit prefix (10^24), used for describing extremely large data quantities."
            };
        }

        public static string[] banner = new string[]
            {
                "███████╗████████╗██╗             ██╗██╗       ",
                "██╔════╝╚══██╔══╝██║             ██║██║       ",
                "███████╗   ██║   ██║██╗████╗ ██████║██║██████╗",
                "╚════██║   ██║   ██║████╔══╝██╔══██║██║████╔═╝",
                "███████║   ██║   ██║██╔═╝   ╚██████║██║██████╗",
                "╚══════╝   ╚═╝   ╚═╝╚═╝      ╚═════╝╚═╝╚═════╝",
                ""
            };

        public static string[][] hangmanStages = new string[][]
            {
                new string[] {
                    " +---+",
                    " |   |",
                    "     |",
                    "     |",
                    "     |",
                    "     |",
                    "======="
                },
                new string[] {
                    " +---+",
                    " |   |",
                    " O   |",
                    "     |",
                    "     |",
                    "     |",
                    "======="
                },
                new string[] {
                    " +---+",
                    " |   |",
                    " O   |",
                    " |   |",
                    "     |",
                    "     |",
                    "======="
                },
                new string[] {
                    " +---+",
                    " |   |",
                    " O   |",
                    "/|   |",
                    "     |",
                    "     |",
                    "======="
                },
                new string[] {
                    " +---+",
                    " |   |",
                    " O   |",
                    "/|\\  |",
                    "     |",
                    "     |",
                    "======="
                },
                new string[] {
                    " +---+",
                    " |   |",
                    " O   |",
                    "/|\\  |",
                    "/    |",
                    "     |",
                    "======="
                },
                new string[] {
                    " +---+",
                    " |   |",
                    " O   |",
                    "/|\\  |",
                    "/ \\  |",
                    "     |",
                    "======="
                }
            };

        public static bool stopFlashing;

        // Helper Functions
        static void DrawHangman(string[] stage, int left, int top)
        {
            for (int i = 0; i < stage.Length; i++)
            {
                Console.SetCursorPosition(left, top + i);
                Console.Write(stage[i]);
            }
        }

        static void DisableResize(IntPtr hWnd)
        {
            int style = GetWindowLong(hWnd, GWL_STYLE);

            // Remove resizing and the maximize/minimize buttons
            style &= ~WS_SIZEBOX;       // disable window edge drag
            style &= ~WS_MAXIMIZEBOX;   // remove maximize button
            style &= ~WS_MINIMIZEBOX;   // remove minimize button

            SetWindowLong(hWnd, GWL_STYLE, style);
        }

        static void DrawBanner(string[] banner, int top, ConsoleColor blockColor, ConsoleColor lineColor)
        {
            for (int i = 0; i < banner.Length; i++)
            {
                string line = banner[i];
                int left = (Console.WindowWidth - line.Length) / 2;
                if (left < 0) left = 0;

                Console.SetCursorPosition(left, top + i);

                foreach (char c in line)
                {
                    switch (c)
                    {
                        case '█':
                            Console.ForegroundColor = blockColor;
                            break;
                        case '═':
                        case '║':
                        case '╝':
                        case '╚':
                        case '╗':
                        case '╔':
                            Console.ForegroundColor = lineColor;
                            break;
                        default:
                            Console.ForegroundColor = ConsoleColor.White;
                            break;
                    }

                    Console.Write(c);
                }

                Console.WriteLine();
            }

            Console.ResetColor();
        }

        public static void PrintCentered(string text)
        {
            int currentLine = Console.CursorTop;

            // Clear current line
            Console.SetCursorPosition(0, currentLine);
            Console.Write(new string(' ', Console.WindowWidth));

            // Center plain text
            int left = (Console.WindowWidth - text.Length) / 2;
            if (left < 0) left = 0;

            Console.SetCursorPosition(left, currentLine);
            Console.WriteLine(text);
        }

        public static void PrintCentered(string coloredText, string plainText, ConsoleColor color)
        {
            int currentLine = Console.CursorTop;

            // Clear current line
            Console.SetCursorPosition(0, currentLine);
            Console.Write(new string(' ', Console.WindowWidth));

            // Combine text for centering
            string fullText = coloredText + plainText;
            int left = (Console.WindowWidth - fullText.Length) / 2;
            if (left < 0) left = 0;

            // Print colored + plain text centered
            Console.SetCursorPosition(left, currentLine);
            Console.ForegroundColor = color;
            Console.Write(coloredText);
            Console.ResetColor();
            Console.WriteLine(plainText);
        }

        public static void FlashLine(int line, string text, ConsoleColor flashColor, int durationMs = 2000, int intervalMs = 300)
        {
            // Save current cursor and color state
            int originalTop = Console.CursorTop;
            int originalLeft = Console.CursorLeft;
            ConsoleColor originalColor = Console.ForegroundColor;

            // Calculate end time
            DateTime endTime = DateTime.Now.AddMilliseconds(durationMs);
            bool isColored = false;

            while (DateTime.Now < endTime)
            {
                // Switch color each cycle
                Console.ForegroundColor = isColored ? originalColor : flashColor;

                // Write the text on the specified line
                Console.SetCursorPosition(0, line);
                Console.Write(new string(' ', Console.WindowWidth)); // Clear the line
                Console.SetCursorPosition(0, line);
                PrintCentered(text);

                // Toggle and wait
                isColored = !isColored;
                Thread.Sleep(intervalMs);
            }

            // Restore original text and cursor position
            Console.ForegroundColor = originalColor;
            Console.SetCursorPosition(0, line);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, line);
            PrintCentered(text);
            Console.SetCursorPosition(originalLeft, originalTop);
        }

        public static Thread FlashLineAsync(int line, string text, ConsoleColor flashColor, int intervalMs = 300)
        {
            stopFlashing = false;

            Thread flashThread = new Thread(() =>
            {
                int originalTop = Console.CursorTop;
                int originalLeft = Console.CursorLeft;
                ConsoleColor originalColor = Console.ForegroundColor;

                bool isColored = false;

                while (!stopFlashing)
                {
                    Console.ForegroundColor = isColored ? originalColor : flashColor;
                    Console.SetCursorPosition(0, line);
                    Console.Write(new string(' ', Console.WindowWidth));
                    Console.SetCursorPosition(0, line);
                    PrintCentered(text);

                    isColored = !isColored;
                    Thread.Sleep(intervalMs);
                }

                // Restore final state
                Console.ForegroundColor = originalColor;
                Console.SetCursorPosition(0, line);
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, line);
                PrintCentered(text);
                Console.SetCursorPosition(originalLeft, originalTop);
            });

            flashThread.IsBackground = true;
            flashThread.Start();
            return flashThread;
        }

        public static void StopFlashing()
        {
            stopFlashing = true;
        }

        static void PrintAtLine(int line, string text, ConsoleColor color = ConsoleColor.Gray)
        {
            lock (consoleLock)
            {
                int oldLeft = Console.CursorLeft;
                int oldTop = Console.CursorTop;
                ConsoleColor oldColor = Console.ForegroundColor;

                Console.ForegroundColor = color;
                Console.SetCursorPosition(0, line);
                Console.Write(new string(' ', Console.WindowWidth)); // clear line
                Console.SetCursorPosition(0, line);
                PrintCentered(text);
                Console.ForegroundColor = oldColor;

                Console.SetCursorPosition(oldLeft, oldTop);
            }
        }

        static void Exit()
        {
            Environment.Exit(0);
        }
    }
}


