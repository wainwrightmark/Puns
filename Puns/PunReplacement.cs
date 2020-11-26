namespace Puns
{
    public readonly struct PunReplacement
    {
        public PunReplacement(PunType punType, string replacementString, bool isAmalgam)
        {
            PunType = punType;
            IsAmalgam = isAmalgam;
            ReplacementString = replacementString.Replace('_', ' ');
        }

        public PunType PunType { get; }

        public string ReplacementString { get; }

        public bool IsAmalgam { get; }

        /// <inheritdoc />
        public override string ToString() => ReplacementString;
    }
}