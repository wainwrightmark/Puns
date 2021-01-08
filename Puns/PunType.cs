namespace Puns
{

public enum PunType
{
    /// <summary>
    /// The exact same word - not really a pun
    /// </summary>
    SameWord,

    /// <summary>
    /// Bass / Base
    /// </summary>
    Identity,

    /// <summary>
    /// Multiple vowel sounds and all subsequent syllables match
    /// </summary>
    RichRhyme,

    /// <summary>
    /// Final vowel sound and all subsequent syllables match
    /// </summary>
    PerfectRhyme,

    /// <summary>
    /// Final vowel segments are different while the consonants are identical, or vice versa
    /// </summary>
    ImperfectRhyme, //Worse than prefix and infix

    /// <summary>
    /// One word is a prefix to the other
    /// </summary>
    Prefix,

    PrefixRhyme,

    /// <summary>
    /// One word is contained within the other
    /// </summary>
    Infix,

    /// <summary>
    /// Both words share at least four syllables of prefix
    /// </summary>
    SharedPrefix,
    SameConsonants
}

}
