using System;

namespace ReciteHelper.Core.EventArgs;

public class PhonkEventArgs : System.EventArgs
{
    public string ImageUri { get; }
    public string SoundFile { get; }

    public PhonkEventArgs(string imageUri, string soundFile)
    {
        ImageUri = imageUri;
        SoundFile = soundFile;
    }
}
