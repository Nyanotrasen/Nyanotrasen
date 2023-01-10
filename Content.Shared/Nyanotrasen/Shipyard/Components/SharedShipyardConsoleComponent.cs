using Content.Shared.Access;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Audio;

namespace Content.Shared.Shipyard.Components
{
    [NetworkedComponent]
    public abstract class SharedShipyardConsoleComponent : Component
    {
        // Put this on shared so we just send the state once in PVS range rather than every time the UI updates.

        [DataField("accessLevels", customTypeSerializer: typeof(PrototypeIdListSerializer<AccessLevelPrototype>))]
        public List<string> AccessLevels = new()
        {
            "Armory",
            "Atmospherics",
            "Bar",
            "Brig",
            // "Detective",
            "Captain",
            "Cargo",
            "Chapel",
            "Chemistry",
            "ChiefEngineer",
            "ChiefMedicalOfficer",
            "Command",
            "Engineering",
            "External",
            "HeadOfPersonnel",
            "HeadOfSecurity",
            "Hydroponics",
            "Janitor",
            "Kitchen",
            "Maintenance",
            "Medical",
            "Quartermaster",
            "Research",
            "ResearchDirector",
            "Salvage",
            "Security",
            "Service",
            "Theatre",
        };

        [DataField("soundError")]
        public SoundSpecifier ErrorSound =
            new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_sigh.ogg");

        [DataField("soundConfirm")]
        public SoundSpecifier ConfirmSound =
            new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");    
    }
}
