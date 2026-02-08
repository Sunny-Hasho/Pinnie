using System;
using System.IO;
using System.Media;
using System.Windows;

namespace Pinnie.Services
{
    public class SoundService
    {
        private readonly SoundPlayer _soundPlayer;

        public SoundService()
        {
            _soundPlayer = new SoundPlayer();
        }

        public void PlayPinSound()
        {
            PlaySound();
        }
        
        public void PlayUnpinSound()
        {
            PlaySound();
        }

        private void PlaySound()
        {
            try
            {
                // Load from Embedded Resource using Pack URI
                var uri = new Uri("pack://application:,,,/Assets/mixkit-gear-fast-lock-tap-2857.wav", UriKind.Absolute);
                var resourceStream = System.Windows.Application.GetResourceStream(uri);
                
                if (resourceStream != null)
                {
                    _soundPlayer.Stream = resourceStream.Stream;
                    _soundPlayer.Play();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"SoundService: Error playing sound - {ex.Message}");
            }
        }
    }
}
