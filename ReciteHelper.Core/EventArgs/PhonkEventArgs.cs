using System;

public class PhonkEventArgs : EventArgs
{
    public string ImageUri { get; }
    public string SoundFile { get; }

    public PhonkEventArgs(string imageUri, string soundFile)
    {
        ImageUri = imageUri;
        SoundFile = soundFile;
    }
}
