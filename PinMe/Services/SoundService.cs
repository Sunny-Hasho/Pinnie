using System;
using System.IO;
using System.Media;
using System.Windows;

namespace Pinnie.Services
{
    public class SoundService
    {
        private readonly SoundPlayer _soundPlayer;
        private readonly string _soundFilePath;

        public SoundService()
        {
            _soundPlayer = new SoundPlayer();
            
            // Path to the sound file in Assets folder
            _soundFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "mixkit-gear-fast-lock-tap-2857.wav");
            
            // Verify the sound file exists
            if (!File.Exists(_soundFilePath))
            {
                Logger.Log($"SoundService: Warning - Sound file not found at {_soundFilePath}");
            }
            else
            {
                Logger.Log($"SoundService: Initialized with sound file at {_soundFilePath}");
            }
        }

        public void PlayPinSound()
        {
            PlaySound();
        }

        public void PlayUnpinSound()
        {
            // Using the same sound for both pin and unpin
            // If you want different sounds, we can add a second file
            PlaySound();
        }

        private void PlaySound()
        {
            try
            {
                if (File.Exists(_soundFilePath))
                {
                    _soundPlayer.SoundLocation = _soundFilePath;
                    _soundPlayer.Play(); // Play asynchronously (non-blocking)
                }
                else
                {
                    Logger.Log("SoundService: Cannot play sound - file not found");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"SoundService: Error playing sound - {ex.Message}");
            }
        }
    }
}
