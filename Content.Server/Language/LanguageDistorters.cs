using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Language
{
    [ImplicitDataDefinitionForInheritors]
    public abstract class UnknownLanguageDistorter
    {
        public abstract string Distort(EntityUid source, string message);
    }

    [UsedImplicitly]
    public sealed class GenericDistorter : UnknownLanguageDistorter
    {
        [ViewVariables]
        [DataField("lines")]
        public string[] Lines = default!;

        public override string Distort(EntityUid source, string message)
        {
            var random = IoCManager.Resolve<IRobustRandom>();
            return Loc.GetString(random.Pick(Lines));
        }
    }

    /// <summary>
    /// Distort the meaning while attempting to maintain the emotional content and rhythm.
    /// </summary>
    [UsedImplicitly]
    public sealed class EmotionalDistorter : UnknownLanguageDistorter
    {
        [ViewVariables]
        [DataField("phonemes", required: true)]
        public string[] Phonemes = default!;

        [ViewVariables]
        [DataField("maxExtraPhoneChars")]
        public int MaxExtraPhoneChars = 3;

        public override string Distort(EntityUid source, string message)
        {
            var builder = new StringBuilder();
            var random = IoCManager.Resolve<IRobustRandom>();

            var letterChars = 0;
            var upperChars = 0;

            void AppendPhoneme()
            {
                var phoneme = random.Pick(Phonemes);

                // Take however many letters have been counted so far,
                // randomize it, then repeat the phoneme up to that distance.
                var length = random.Next(1, letterChars + MaxExtraPhoneChars);
                var replacement = String.Concat(Enumerable.Repeat(phoneme, 1 + length / phoneme.Length)).Substring(0, length);

                // If at least half of the characters are uppercase, make the whole block uppercase.
                if (upperChars / letterChars >= 0.5f)
                    replacement = replacement.ToUpper();

                builder.Append(replacement);
            }

            foreach (var character in message)
            {
                if (Char.IsWhiteSpace(character) || Char.IsPunctuation(character))
                {
                    if (letterChars > 0)
                        AppendPhoneme();

                    builder.Append(character);

                    upperChars = 0;
                    letterChars = 0;
                }
                else
                {
                    if (Char.IsUpper(character))
                        upperChars++;

                    letterChars++;
                }
            }

            // Don't forget the leftovers.
            if (letterChars > 0)
                AppendPhoneme();

            return builder.ToString();
        }
    }

    [UsedImplicitly]
    public sealed class UnintelligibleSoundsDistorter : UnknownLanguageDistorter
    {
        public override string Distort(EntityUid source, string message)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            var component = entityManager.GetComponent<LinguisticComponent>(source);

            if (component?.UnintelligibleSounds == null)
            {
                Logger.ErrorS("language", $"UnintelligibleSoundsDistorter found no sounds on {source}");
                return message;
            }

            if (!prototypeManager.TryIndex<UnintelligibleSoundsPrototype>(component.UnintelligibleSounds, out var sounds))
            {
                Logger.ErrorS("language", $"{source} has invalid sounds {component.UnintelligibleSounds}");
                return message;
            }

            var random = IoCManager.Resolve<IRobustRandom>();
            return Loc.GetString(random.Pick(sounds.Lines));
        }
    }
}
