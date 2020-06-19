﻿namespace RAGENativeUI.Internals
{
    using System;
    using System.Drawing;
    using System.Runtime.InteropServices;

    using Rage;

    using static RAGENativeUI.IL.Unsafe;
    using static RAGENativeUI.IL.Invoker;

    internal static unsafe class Memory
    {
        public static readonly IntPtr Screen_GetActualWidth, Screen_GetActualHeight;
        public static readonly IntPtr CTextStyle_ScriptStyle;
        public static readonly IntPtr CScaleformMgr_IsMovieRendering;
        public static readonly IntPtr CScaleformMgr_BeginMethod;
        public static readonly IntPtr CBusySpinner_InstructionalButtons;
        public static readonly IntPtr scrProgramRegistry_sm_Instance;
        public static readonly IntPtr scrProgramRegistry_Find;
        public static readonly int TimershudSharedGlobalId = -1;
        public static readonly int TimershudSharedTimerbarsTotalHeightOffset = -1;
        public static readonly int TimerbarsPrevTotalHeightGlobalId = -1;
        public static readonly int TimershudSharedInstructionalButtonsNumRowsOffset = -1;

        static Memory()
        {
            const string GetActualResolutionPattern = "48 83 EC 38 0F 29 74 24 ?? 66 0F 6E 35 ?? ?? ?? ?? 48 8D 0D ?? ?? ?? ??";

            Screen_GetActualHeight = FindAddress(() => Game.FindPattern(GetActualResolutionPattern));
            if (Screen_GetActualHeight != IntPtr.Zero)
            {
                Screen_GetActualWidth = FindAddress(() => Game.FindPattern(GetActualResolutionPattern, Screen_GetActualHeight + 1));
            }

            CTextStyle_ScriptStyle = FindAddress(() =>
            {
                IntPtr addr = Game.FindPattern("48 8D 0D ?? ?? ?? ?? 44 8A C3 E8 ?? ?? ?? ?? 83 25");
                if (addr != IntPtr.Zero)
                {
                    addr += *(int*)(addr + 3) + 7;
                }
                return addr;
            });

            CScaleformMgr_IsMovieRendering = FindAddress(() =>
            {
                IntPtr addr = Game.FindPattern("65 48 8B 04 25 ?? ?? ?? ?? 8B F9 48 8B 04 D0 B9 ?? ?? ?? ?? 8B 14 01");
                if (addr != IntPtr.Zero)
                {
                    addr -= 0x10;
                }
                return addr;
            });

            CScaleformMgr_BeginMethod = FindAddress(() => Game.FindPattern("48 83 EC 38 41 83 C9 FF 44 89 4C 24 ?? E8"));

            CBusySpinner_InstructionalButtons = FindAddress(() =>
            {
                IntPtr addr = Game.FindPattern("0F B7 05 ?? ?? ?? ?? 33 DB 8B F8 85 C0");
                if (addr != IntPtr.Zero)
                {
                    addr += *(int*)(addr + 3) + 7 - 8;
                }
                return addr;
            });

            IntPtr scrProgramRegistryAddr = FindAddress(() => Game.FindPattern("48 8D 0D ?? ?? ?? ?? 8B D0 44 8B F0 89 85"));
            if (scrProgramRegistryAddr != IntPtr.Zero)
            {
                scrProgramRegistry_sm_Instance = scrProgramRegistryAddr + * (int*)(scrProgramRegistryAddr + 3) + 7;
                scrProgramRegistry_Find = scrProgramRegistryAddr + *(int*)(scrProgramRegistryAddr + 19) + 23;
            }

            if (Shared.MemoryInts[0] == 0 || Shared.MemoryInts[1] == 0 || Shared.MemoryInts[2] == 0)
            {
                IntPtr ingamehudAddr = FindPatternInScript("ingamehud",
                                                        new byte[] { 0x6F, 0x39, 0x08, 0x5F, 0x00, 0x00, 0x00, 0x5E, 0x00, 0x00, 0x00, 0x47 },
                                                        new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0xFF, 0x00, 0x00, 0x00, 0xFF });
                if (ingamehudAddr != IntPtr.Zero)
                {
                    Shared.MemoryInts[0] = TimerbarsPrevTotalHeightGlobalId = *(int*)(ingamehudAddr + 4) & 0xFFFFFF;
                    Shared.MemoryInts[1] = TimershudSharedGlobalId = *(int*)(ingamehudAddr + 8) & 0xFFFFFF;
                    Shared.MemoryInts[2] = TimershudSharedTimerbarsTotalHeightOffset = *(short*)(ingamehudAddr + 12);
                }
            }
            else
            {
                TimerbarsPrevTotalHeightGlobalId = Shared.MemoryInts[0];
                TimershudSharedGlobalId = Shared.MemoryInts[1];
                TimershudSharedTimerbarsTotalHeightOffset = Shared.MemoryInts[2];
            }

            if (Shared.MemoryInts[3] == 0)
            {
                IntPtr ingamehudAddr = FindPatternInScript("ingamehud",
                                                        new byte[] { 0x47, 0x00, 0x00, 0x22, 0x10, 0x0E, 0x39, },
                                                        new byte[] { 0xFF, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, });
                if (ingamehudAddr != IntPtr.Zero)
                {
                    Shared.MemoryInts[3] = TimershudSharedInstructionalButtonsNumRowsOffset = *(short*)(ingamehudAddr + 1);
                }
            }
            else
            {
                TimershudSharedInstructionalButtonsNumRowsOffset = Shared.MemoryInts[3];
            }
        }

        private static int findAddressKey = 0;
        private static IntPtr FindAddress(Func<IntPtr> find)
        {
            int key = findAddressKey++;
            if (Shared.MemoryAddresses[key] == 0)
            {
                IntPtr addr = find();
                Shared.MemoryAddresses[key] = addr.ToInt64();
                return addr;
            }
            else
            {
                return (IntPtr)Shared.MemoryAddresses[key];
            }
        }

        private static IntPtr FindPatternInScript(string name, byte[] pattern, byte[] mask)
        {
            if (!scrProgramRegistry.Available)
            {
                return IntPtr.Zero;
            }

            scrProgram* prog = scrProgramRegistry.Instance.Find(name);

            if (prog == null)
            {
                return IntPtr.Zero;
            }

            const uint CodePageMaxSize = 0x4000;

            int numCodePages = (int)(prog->CodeLength + (CodePageMaxSize - 1)) >> 14;
            for (int i = 0; i < numCodePages; i++)
            {
                uint size = i == (numCodePages - 1) ? (prog->CodeLength & 0x3FFF) : CodePageMaxSize;

                IntPtr match = FindPatternInRange(prog->CodePages[i], size, pattern, mask);
                if (match != IntPtr.Zero)
                {
                    return match;
                }
            }

            return IntPtr.Zero;
        }

        private static IntPtr FindPatternInRange(byte* address, uint size, byte[] pattern, byte[] mask)
        {
            byte* endAddress = address + (size - pattern.Length);
            for (; address <= endAddress; address += 1)
            {
                bool found = true;
                for (int i = 0; i < pattern.Length; i++)
                {
                    if ((address[i] & mask[i]) != pattern[i])
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    return (IntPtr)address;
                }
            }

            return IntPtr.Zero;
        }
    }

    internal static unsafe class ScriptGlobals
    {
        public static readonly bool TimersBarsTotalHeightAvailable = Memory.TimershudSharedGlobalId != -1 && Memory.TimershudSharedTimerbarsTotalHeightOffset != -1 && Memory.TimerbarsPrevTotalHeightGlobalId != -1;

        public static ref float TimerBarsTotalHeight => ref AsRef<float>(Game.GetScriptGlobalVariableAddress(Memory.TimershudSharedGlobalId) + Memory.TimershudSharedTimerbarsTotalHeightOffset * 8);
        public static ref float TimerBarsPrevTotalHeight => ref AsRef<float>(Game.GetScriptGlobalVariableAddress(Memory.TimerbarsPrevTotalHeightGlobalId));

        public static readonly bool TimerBarsInstructionButtonsNumRowsAvailable = Memory.TimershudSharedGlobalId != -1 && Memory.TimershudSharedInstructionalButtonsNumRowsOffset != -1;

        public static ref int TimerBarsInstructionButtonsNumRows => ref AsRef<int>(Game.GetScriptGlobalVariableAddress(Memory.TimershudSharedGlobalId) + Memory.TimershudSharedInstructionalButtonsNumRowsOffset * 8);

#if false // not needed for now
        public static readonly bool MenuIdsAvailable = true;
        public static ref ScriptUIntArray MenuIds => ref AsRef<ScriptUIntArray>(Game.GetScriptGlobalVariableAddress(22350) + 5721 * 8);

        [StructLayout(LayoutKind.Explicit)]
        public struct ScriptUIntArray
        {
            [FieldOffset(0x0)] public int Size;

            public ref uint this[int index] => ref *(uint*)((byte*)AsPointer(ref this) + 8 + (8 * (Size - index - 1)));

            public Enumerator GetEnumerator() => new Enumerator(ref this);

            public ref struct Enumerator
            {
                private readonly ScriptUIntArray* array;
                private int index;

                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                public Enumerator(ref ScriptUIntArray arr)
                {
                    array = (ScriptUIntArray*)AsPointer(ref arr);
                    index = -1;
                }

                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                public bool MoveNext()
                {
                    int newIndex = index + 1;
                    if (newIndex < array->Size)
                    {
                        index = newIndex;
                        return true;
                    }

                    return false;
                }

                public ref uint Current
                {
                    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                    get => ref (*array)[index];
                }
            }
        }
#endif
    }

    internal static class Screen
    {
        private static readonly bool Available = Memory.Screen_GetActualWidth != IntPtr.Zero && Memory.Screen_GetActualHeight != IntPtr.Zero;

        /// <summary>
        /// Gets the resolution of the main screen, the one containing the UI in case multiple screens are used.
        /// </summary>
        public static SizeF ActualResolution
        {
            get
            {
                if (Available)
                {
                    using var tls = UsingTls.Scope();

                    return new SizeF(InvokeRetFloat(Memory.Screen_GetActualWidth), InvokeRetFloat(Memory.Screen_GetActualHeight));
                }
                else
                {
                    return Game.Resolution; // not the same since this the resolution of all screens combined, but good enough as a fallback
                }
            }
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x28)]
    internal struct CTextStyle
    {
        [FieldOffset(0x00)] public int Color;

        [FieldOffset(0x08)] public float Scale;
        [FieldOffset(0x0C)] public (float Start, float End) Wrap;
        [FieldOffset(0x14)] public int Font;

        [FieldOffset(0x1C)] public byte Justification;

        [FieldOffset(0x1E), MarshalAs(UnmanagedType.I1)] public bool DropShadow;
        [FieldOffset(0x1F), MarshalAs(UnmanagedType.I1)] public bool Outline;


        public static ref CTextStyle ScriptStyle => ref AsRef<CTextStyle>(Memory.CTextStyle_ScriptStyle);

        public static readonly bool ScriptStyleAvailable = Memory.CTextStyle_ScriptStyle != IntPtr.Zero;
    }

    internal static class CScaleformMgr
    {
        public static bool BeginMethod(int index, int baseClass, string methodName)
        {
            // set thread type to 1 so CScaleformMgr::BeginMethod adds the method call to the same queue as if it was done using natives
            long v = UsingTls.Get(0xB4);
            UsingTls.Set(0xB4, 1);
            bool b = InvokeRetBool(Memory.CScaleformMgr_BeginMethod, index, baseClass, methodName);
            UsingTls.Set(0xB4, v);
            return b;
        }

        public static bool IsMovieRendering(int index)
        {
            // CScaleformMgr::IsMovieRendering checks if the current thread is the render thread,
            // so temporarily set this thread type to the render thread type (2)
            // doesn't seem to cause any issue in this case
            long v = UsingTls.Get(0xB4);
            UsingTls.Set(0xB4, 2);
            bool b = InvokeRetBool(Memory.CScaleformMgr_IsMovieRendering, index);
            UsingTls.Set(0xB4, v);
            return b;
        }

        public static readonly bool Available = Memory.CScaleformMgr_IsMovieRendering != IntPtr.Zero && Memory.CScaleformMgr_BeginMethod != IntPtr.Zero;
    }

    internal static class CBusySpinner
    {
        public static ref atArray<int> InstructionalButtons => ref AsRef<atArray<int>>(Memory.CBusySpinner_InstructionalButtons);

        public static readonly bool Available = Memory.CBusySpinner_InstructionalButtons != IntPtr.Zero;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x658)]
    internal unsafe struct scrProgramRegistry
    {
        public scrProgram* Find(string name) => Find(Game.GetHashKey(name));
        public scrProgram* Find(uint name) => (scrProgram*)InvokeRetPointer(Memory.scrProgramRegistry_Find, AsPointer(ref this), name);

        public static ref scrProgramRegistry Instance => ref AsRef<scrProgramRegistry>(Memory.scrProgramRegistry_sm_Instance);

        public static readonly bool Available = Memory.scrProgramRegistry_sm_Instance != IntPtr.Zero && Memory.scrProgramRegistry_Find != IntPtr.Zero;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x80)]
    internal unsafe struct scrProgram
    {
        [FieldOffset(0x10)] public byte** CodePages;
        [FieldOffset(0x18)] public uint Hash;
        [FieldOffset(0x1C)] public uint CodeLength;
        [FieldOffset(0x20)] public uint ArgsCount;
        [FieldOffset(0x24)] public uint StaticsCount;
        [FieldOffset(0x28)] public uint GlobalsCount;
        [FieldOffset(0x2C)] public uint NativesCount;
        [FieldOffset(0x30)] public scrValue* StaticsInitialValues;
        [FieldOffset(0x38)] public scrValue** GlobalsInitialValues;
        [FieldOffset(0x40)] public void** Natives;
        
        [FieldOffset(0x58)] public uint NameHash;
        [FieldOffset(0x5C)] public uint NumRefs;
        [FieldOffset(0x60)] public IntPtr Name;
        [FieldOffset(0x68)] public byte** Strings;
        [FieldOffset(0x70)] public uint StringsCount;

        public byte* IP(uint ip) => &CodePages[ip >> 14][ip & 0x3FFF];
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x8)]
    internal unsafe struct scrValue 
    {
        [FieldOffset(0x0)] public IntPtr Val;
        [FieldOffset(0x0)] public float AsFloat;
        [FieldOffset(0x0)] public int AsInt;
    }
}
