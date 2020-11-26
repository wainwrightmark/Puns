namespace Puns
{
    public readonly struct PunReplacement
    {
        public PunReplacement(PunType punType, string replacementString, bool isAmalgam, string punWord)
        {
            PunType = punType;
            IsAmalgam = isAmalgam;
            PunWord = punWord.Replace('_', ' ');
            ReplacementString = replacementString.Replace('_', ' ');
        }

        public PunType PunType { get; }

        public string PunWord { get; }

        public string ReplacementString { get; }

        public bool IsAmalgam { get; }

        /// <inheritdoc />
        public override string ToString() => ReplacementString;
    }
}