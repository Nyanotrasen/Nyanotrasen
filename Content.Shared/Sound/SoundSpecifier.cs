using Content.Shared.Audio;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

namespace Content.Shared.Sound
{
    [ImplicitDataDefinitionForInheritors]
    public abstract class SoundSpecifier
    {
        [DataField("params")]
        public AudioParams Params = AudioParams.Default;

        public abstract string GetSound();
    }

    public sealed class SoundPathSpecifier : SoundSpecifier
    {
        public const string Node = "path";

        [DataField(Node, customTypeSerializer: typeof(ResourcePathSerializer), required: true)]
        public ResourcePath? Path { get; }

        [UsedImplicitly]
        public SoundPathSpecifier()
        {
        }

        public SoundPathSpecifier(string path)
        {
            Path = new ResourcePath(path);
        }

        public SoundPathSpecifier(ResourcePath path)
        {
            Path = path;
        }

        public override string GetSound()
        {
            return Path == null ? string.Empty : Path.ToString();
        }
    }

    public sealed class SoundCollectionSpecifier : SoundSpecifier
    {
        public const string Node = "collection";

        [DataField(Node, customTypeSerializer: typeof(PrototypeIdSerializer<SoundCollectionPrototype>), required: true)]
        public string? Collection { get; }

        [UsedImplicitly]
        public SoundCollectionSpecifier()
        {
        }

        public SoundCollectionSpecifier(string collection)
        {
            Collection = collection;
        }

        public override string GetSound()
        {
            return Collection == null ? string.Empty : AudioHelpers.GetRandomFileFromSoundCollection(Collection);
        }
    }
}
