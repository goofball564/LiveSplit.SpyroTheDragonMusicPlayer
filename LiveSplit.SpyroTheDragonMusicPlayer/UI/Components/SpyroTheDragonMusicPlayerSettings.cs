using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Xml;

namespace LiveSplit.UI.Components
{
    public partial class SpyroTheDragonMusicPlayerSettings : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private const string DEFAULT_MUSIC_DIRECTORY = "Spyro1Music";
        private const int MAX_VOLUME = 100;
        private const int MIN_VOLUME = 0;

        private readonly FolderBrowserDialog folderBrowserDialog;

        private int _volume;
        private string _musicDirectory;

        public string MusicDirectory 
        {
            get => _musicDirectory;
            set 
            {
                _musicDirectory = value;
                NotifyPropertyChanged();
            } 
        }
        public int Volume 
        {
            get => _volume;
            set
            {
                if (value > MAX_VOLUME)
                    _volume = MAX_VOLUME;
                else if (value < MIN_VOLUME)
                    _volume = MIN_VOLUME;
                else
                    _volume = value;
                NotifyPropertyChanged();
            }
        }
        public LayoutMode Mode { get; set; }

        public SpyroTheDragonMusicPlayerSettings()
        {
            InitializeComponent();
            folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.ShowNewFolderButton = true;
            Volume = 100;
            MusicDirectory = Path.Combine(Directory.GetCurrentDirectory(), DEFAULT_MUSIC_DIRECTORY);
        }

        private void SpyroTheDragonMusicPlayerSettings_Load(object sender, EventArgs e)
        {
            buttonChangeMusicDirectory.Click += buttonChangeMusicDirectory_Click;
            buttonResetMusicDirectory.Click += buttonResetMusicDirectory_Click;

            trackBarVolume.DataBindings.Clear();
            textBoxVolume.DataBindings.Clear();
            textBoxMusicDirectory.DataBindings.Clear();

            trackBarVolume.DataBindings.Add("Value", this, "Volume", false, DataSourceUpdateMode.OnPropertyChanged);
            textBoxVolume.DataBindings.Add("Text", this, "Volume", false, DataSourceUpdateMode.OnPropertyChanged);
            textBoxMusicDirectory.DataBindings.Add("Text", this, "MusicDirectory", false, DataSourceUpdateMode.OnPropertyChanged);
        }

        public XmlNode GetSettings(XmlDocument document)
        {
            XmlElement settingsNode = document.CreateElement("Settings");

            SettingsHelper.CreateSetting(document, settingsNode, "MusicDirectory", MusicDirectory);
            SettingsHelper.CreateSetting(document, settingsNode, "Volume", Volume);

            return settingsNode;
        }

        public void SetSettings(XmlNode node)
        {
            var element = (XmlElement)node;
            MusicDirectory = SettingsHelper.ParseString(element["MusicDirectory"]);
            Volume = SettingsHelper.ParseInt(element["Volume"]);
        }

        private void buttonChangeMusicDirectory_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                MusicDirectory = folderBrowserDialog.SelectedPath;
            }
        }

        private void buttonResetMusicDirectory_Click(object sender, EventArgs e)
        {
            MusicDirectory = Path.Combine(Directory.GetCurrentDirectory(), DEFAULT_MUSIC_DIRECTORY);
        }
    }
}
