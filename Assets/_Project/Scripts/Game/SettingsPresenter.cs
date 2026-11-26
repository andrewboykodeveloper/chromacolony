using System;
using Chromacolony.Core;
using UnityEngine.UIElements;

namespace Chromacolony.Game
{
    public sealed class SettingsPresenter
    {
        private const string OnClass = "on";

        private readonly ScreenHost _screens;
        private readonly Localization _text;
        private readonly Progress _progress;
        private readonly AudioService _audio;
        private readonly Action _save;
        private Label _title;
        private Label _languageName;
        private Label _musicName;
        private Label _soundsName;
        private Label _voiceName;
        private Button _russian;
        private Button _english;
        private Button _music;
        private Button _sounds;
        private Button _voice;
        private Button _done;
        private Button _pauseSettings;
        private bool _fromPause;

        public event Action LanguageChanged;

        public SettingsPresenter(ScreenHost screens, Localization text, Progress progress, AudioService audio, Action save)
        {
            _screens = screens;
            _text = text;
            _progress = progress;
            _audio = audio;
            _save = save;
        }

        public void Bind(VisualElement root, ButtonBindings bindings)
        {
            _title = root.Q<Label>(UiNames.SettingsTitle);
            _languageName = root.Q<Label>(UiNames.SettingsLanguage);
            _musicName = root.Q<Label>(UiNames.SettingsMusic);
            _soundsName = root.Q<Label>(UiNames.SettingsSounds);
            _voiceName = root.Q<Label>(UiNames.SettingsVoice);
            _russian = root.Q<Button>(UiNames.LanguageRu);
            _english = root.Q<Button>(UiNames.LanguageEn);
            _music = root.Q<Button>(UiNames.MusicSwitch);
            _sounds = root.Q<Button>(UiNames.SoundsSwitch);
            _voice = root.Q<Button>(UiNames.VoiceSwitch);
            _done = root.Q<Button>(UiNames.SettingsDone);
            _pauseSettings = root.Q<Button>(UiNames.PauseSettings);

            bindings.Click(root.Q<Button>(UiNames.MenuSettings), () => Open(fromPause: false));
            bindings.Click(_pauseSettings, () => Open(fromPause: true));
            bindings.Click(_done, Close);
            bindings.Click(_russian, () => Choose(Languages.Russian));
            bindings.Click(_english, () => Choose(Languages.English));
            bindings.Click(_music, ToggleMusic);
            bindings.Click(_sounds, ToggleSounds);
            bindings.Click(_voice, ToggleVoice);
            ShowTexts();
            ShowChoices();
        }

        private void ShowTexts()
        {
            _title.text = _text.Get(TextKeys.Settings);
            _languageName.text = _text.Get(TextKeys.Language);
            _musicName.text = _text.Get(TextKeys.Music);
            _soundsName.text = _text.Get(TextKeys.Sounds);
            _voiceName.text = _text.Get(TextKeys.Voice);
            _done.text = _text.Get(TextKeys.Done);
            _russian.text = _text.Get(TextKeys.LanguageRu);
            _english.text = _text.Get(TextKeys.LanguageEn);
            _pauseSettings.text = _text.Get(TextKeys.Settings);
        }

        private void Open(bool fromPause)
        {
            if (_screens.CurrentDialog != (fromPause ? UiNames.Pause : null))
            {
                return;
            }

            _fromPause = fromPause;
            ShowChoices();
            _screens.Open(UiNames.Settings);
        }

        public void Close()
        {
            if (_screens.CurrentDialog != UiNames.Settings)
            {
                return;
            }

            if (_fromPause && _screens.CurrentScreen == UiNames.Game)
            {
                _screens.Open(UiNames.Pause);
            }
            else
            {
                _screens.CloseDialog();
            }
        }

        private void ShowChoices()
        {
            _music.EnableInClassList(OnClass, _progress.Music);
            _sounds.EnableInClassList(OnClass, _progress.Sound);
            _voice.EnableInClassList(OnClass, _progress.Voice);
            _russian.EnableInClassList(OnClass, _text.Language == Languages.Russian);
            _english.EnableInClassList(OnClass, _text.Language == Languages.English);
        }

        private void Choose(string language)
        {
            _progress.Language = language;
            _save();
            if (_text.Language != language)
            {
                _text.Use(language);
                ShowTexts();
                LanguageChanged?.Invoke();
            }

            ShowChoices();
        }

        private void ToggleMusic()
        {
            _progress.Music = !_progress.Music;
            _audio.ApplyMusic();
            ShowChoices();
            _save();
        }

        private void ToggleSounds()
        {
            _progress.Sound = !_progress.Sound;
            ShowChoices();
            _save();
        }

        private void ToggleVoice()
        {
            _progress.Voice = !_progress.Voice;
            ShowChoices();
            _save();
        }
    }
}
