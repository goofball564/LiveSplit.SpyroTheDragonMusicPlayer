using System;
using System.Diagnostics;
using System.Threading;
using LiveSplit.ComponentUtil;
using PropertyHook;

namespace LiveSplit.SpyroTheDragonMusicPlayer.Hook
{
    public class SpyroTheDragonEmulatorHook : PHook
    {
        private const string MEDNAFEN_NAME = "mednafen";
        private const string DUCKSTATION_NOGUI_NAME = "duckstation-nogui-x64-ReleaseLTCG";
        private const string DUCKSTATION_QT_NAME = "duckstation-qt-x64-ReleaseLTCG";
        private const string EPSXE_NAME = "ePSXe";

        private const int MUSIC_VOLUME_OFFSET = 0x75748;
        private const int GAME_STATE_OFFSET = 0x757d8;
        private const int WORLD_ID_OFFSET = 0x7596c;
        private const int NEXT_WORLD_ID_OFFSET = 0x758b4;

        // All values read from memory are unsigned.
        // If that changes, this must change as well.
        public const int NOT_FOUND = -1;

        // Arguments to PHook constructor.
        private const int REFRESH_INTERVAL = 5000;
        private const int MIN_LIFESPAN = 5000;
        private static Func<Process, bool> processSelector = (p) =>
        {
            return (p.ProcessName == MEDNAFEN_NAME)
            || (p.ProcessName == DUCKSTATION_NOGUI_NAME)
            || (p.ProcessName == DUCKSTATION_QT_NAME)
            || (p.ProcessName == EPSXE_NAME);
        };

        public int MusicVolume
        {
            get => ReadGameMemory<byte>(MUSIC_VOLUME_OFFSET, out byte musicVolume) ? (int)musicVolume : NOT_FOUND;
        }

        public int GameState
        {
            get => ReadGameMemory<uint>(GAME_STATE_OFFSET, out uint gameState) ? (int)gameState : NOT_FOUND;
        }

        public int WorldID
        {
            get => ReadGameMemory<uint>(WORLD_ID_OFFSET, out uint worldID) ? (int)worldID : NOT_FOUND;
        }

        public int NextWorldID
        {
            get => ReadGameMemory<uint>(NEXT_WORLD_ID_OFFSET, out uint nextWorldID) ? (int)nextWorldID : NOT_FOUND;
        }

        public bool Ready
        {
            get => Hooked && Emulator != null;
        }

        private Emulator Emulator { get; set; }

        public SpyroTheDragonEmulatorHook() : base(REFRESH_INTERVAL, MIN_LIFESPAN, processSelector)
        {
            OnHooked += Hook_OnHooked;
            OnUnhooked += Hook_OnUnhooked;
            Start();
        }

        public void Dispose()
        {
            Stop();
            if (Emulator != null)
                Emulator.Dispose();
        }

        private bool ReadGameMemory<T>(int offset, out T memoryValue) where T : struct
        {
            try
            {
                if (Ready)
                {
                    IntPtr baseRAMAddress = Emulator.BaseRAMAddress;
                    if (baseRAMAddress != IntPtr.Zero)
                    {
                        memoryValue = Process.ReadValue<T>(baseRAMAddress + offset);
                        return true;
                    }
                }
            }
            catch (NullReferenceException)
            {
                memoryValue = default;
                return false;
            }

            memoryValue = default;
            return false;
        }

        private void Hook_OnHooked(object sender, PHEventArgs e)
        {
            switch(Process.ProcessName)
            {
                case MEDNAFEN_NAME:
                    Emulator = new Mednafen(Process);
                    break;
                case DUCKSTATION_NOGUI_NAME:
                case DUCKSTATION_QT_NAME:
                    Emulator = new Duckstation(Process);
                    break;
                case EPSXE_NAME:
                    Emulator = new EPSXe(Process);
                    break;
                default:
                    Emulator = null;
                    break;
            }
        }

        private void Hook_OnUnhooked(object sender, PHEventArgs e)
        {
            if (Emulator != null)
            {
                Emulator.Dispose();
                Emulator = null;
            }
        }
    }
}
