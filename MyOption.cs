namespace MoraAudioModify;

internal class MyOption
    {
        public string Url { get; set; } = default!;
        public bool SkipChangeFilename { get; set; }
        public bool SkipFileNameFiltering { get; set; }
        public bool DebugLog { get; set; }
    }