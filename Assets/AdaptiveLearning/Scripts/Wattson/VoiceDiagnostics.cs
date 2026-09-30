namespace AdaptiveOhmsLaw
{
    public static class VoiceDiagnostics
    {
        public static string WindowsError(int code)
        {
            switch(unchecked((uint)code)) {
                case 0x80045509: return "Windows dictation is turned off. Open Settings > Privacy & security > Speech and enable Online speech recognition. Also allow desktop microphone access under Microphone, then restart the app and try SPEAK again. (0x80045509)";
                case 0x80070005: return "Microphone access was denied. Open Windows Settings > Privacy & security > Microphone and allow microphone access for desktop apps, then try again. (0x80070005)";
                default: return "Windows speech recognition could not start (0x"+unchecked((uint)code).ToString("X8")+"). Check the selected input device, microphone permissions, installed speech language and internet connection. You can still type your question.";
            }
        }
        public const string NoMicrophone="No microphone input device was detected. Connect or enable a microphone in Windows Settings > System > Sound > Input, then press SPEAK again.";
    }
}
