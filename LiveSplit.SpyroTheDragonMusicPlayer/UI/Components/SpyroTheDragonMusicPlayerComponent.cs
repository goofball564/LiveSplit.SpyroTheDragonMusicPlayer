using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CSCore.SoundOut;
using LiveSplit.Model;
using LiveSplit.SpyroTheDragonMusicPlayer.Hook;
using LiveSplit.SpyroTheDragonMusicPlayer.Music;
using LiveSplit.UI;
using LiveSplit.UI.Components;

namespace LiveSplit.SpyroTheDragonMusicPlayer
{
    public class SpyroTheDragonMusicPlayerComponent : LogicComponent
    {
        private int oldGameState = SpyroTheDragonEmulatorHook.NOT_FOUND;
        private int oldWorldID = SpyroTheDragonEmulatorHook.NOT_FOUND;

        // This is how we will access all the settings that the user has set.
        public SpyroTheDragonMusicPlayerSettings Settings { get; set; }
        public MusicPlayer MusicPlayer { get; set; }
        public SpyroTheDragonEmulatorHook Spyro { get; set; }
        
        // This object contains all of the current information about the splits, the timer, etc.
        protected LiveSplitState State { get; set; }

        public override string ComponentName => "Spyro the Dragon Music Player";

        // This function is called when LiveSplit creates your component. This happens when the
        // component is added to the layout, or when LiveSplit opens a layout with this component
        // already added.
        public SpyroTheDragonMusicPlayerComponent(LiveSplitState state)
        {
            Spyro = new SpyroTheDragonEmulatorHook();
            MusicPlayer = new MusicPlayer();
            Settings = new SpyroTheDragonMusicPlayerSettings();
            State = state;

            Spyro.OnUnhooked += Component_OnUnhooked;
        }

        public override Control GetSettingsControl(LayoutMode mode)
        {
            return Settings;
        }

        public override System.Xml.XmlNode GetSettings(System.Xml.XmlDocument document)
        {
            return Settings.GetSettings(document);
        }

        public override void SetSettings(System.Xml.XmlNode settings)
        {
            Settings.SetSettings(settings);
        }

        public override void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
        {
            MusicPlayer.Volume = Settings.Volume;

            if (Spyro.Ready && Spyro.MusicVolume == 0)
            {
                int gameState = Spyro.GameState;
                int worldID = Spyro.WorldID;
                int nextWorldID = Spyro.NextWorldID;

                if (worldID != oldWorldID && !MusicPlayer.Open(GetSongPath(worldID)))
                {
                    oldGameState = SpyroTheDragonEmulatorHook.NOT_FOUND;
                    oldWorldID = SpyroTheDragonEmulatorHook.NOT_FOUND;
                    MusicPlayer.PauseAndReset();
                    return;
                }

                if (gameState != oldGameState)
                {
                    // 0: In Game, 11: Save Menu (Fairy), 12: Balloonist (Talking To or Travelling)
                    if (gameState  == 0 || gameState == 11 || gameState == 12)
                    {
                        MusicPlayer.Play();
                    }
                    // 4: Death, 5: Game Over, 7: Results screen after flight level
                    else if (gameState == 4 || gameState == 5 || gameState == 7)
                    {
                        MusicPlayer.PauseAndReset();
                    }
                    else
                    {
                        MusicPlayer.Pause();
                    }
                }

                // Pause when entering balloon
                if (gameState == 12 && worldID != nextWorldID)
                {
                    MusicPlayer.Pause();
                }

                oldGameState = gameState;
                oldWorldID = worldID;
            }
            else
            {
                oldGameState = SpyroTheDragonEmulatorHook.NOT_FOUND;
                oldWorldID = SpyroTheDragonEmulatorHook.NOT_FOUND;
                MusicPlayer.Dispose();
            }
        }

        // This function is called when the component is removed from the layout, or when LiveSplit
        // closes a layout with this component in it.
        public override void Dispose()
        {
            Spyro.Dispose();
            MusicPlayer.Dispose();
        }

        private string GetSongPath(int worldID)
        {
            try
            {
                string[] files = Directory.GetFiles(Settings.MusicDirectory);
                return files.Where(f => Path.GetFileNameWithoutExtension(f) == worldID.ToString()).FirstOrDefault();
            }
            catch (Exception)
            {
                return null;
            }
            
        }

        private void Component_OnUnhooked(object sender, EventArgs e)
        {
            MusicPlayer.Dispose();
        }
    }
}
