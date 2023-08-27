using FortniteReplayReader.Models.NetFieldExports.Enums;
using System.Collections.Generic;
using Unreal.Core.Attributes;
using Unreal.Core.Contracts;
using Unreal.Core.Models;
using Unreal.Core.Models.Enums;

namespace FortniteReplayReader.Models.NetFieldExports
{
	[NetFieldExportGroup("/Script/FortniteGame.FortPlayerStateAthena")]
	public sealed class FortPlayerState : INetFieldExportGroup
	{
		[NetFieldExport("RemoteRole", RepLayoutCmdType.Ignore)]
		public object RemoteRole { get; set; } //Type:  Bits: 2

		[NetFieldExport("Owner", RepLayoutCmdType.PropertyObject)]
		public uint? Owner { get; set; } //Type: AActor* Bits: 16

		[NetFieldExport("Role", RepLayoutCmdType.Ignore)]
		public object Role { get; set; } //Type:  Bits: 2

		[NetFieldExport("Instigator", RepLayoutCmdType.PropertyObject)]
		public uint? Instigator { get; set; } //Type:  Bits: 8

		[NetFieldExport("Score", RepLayoutCmdType.Ignore)]
		public uint? Score { get; set; } //Type:  Bits: 32

		[NetFieldExport("PlayerID", RepLayoutCmdType.PropertyInt)]
		public int? PlayerID { get; set; } //Type: int32 Bits: 32

		[NetFieldExport("Ping", RepLayoutCmdType.PropertyByte)]
		public byte? Ping { get; set; } //Type: uint8 Bits: 8

		[NetFieldExport("bIsABot", RepLayoutCmdType.PropertyBool)]
		public bool? bIsABot { get; set; } //Type:  Bits: 1

		[NetFieldExport("bOnlySpectator", RepLayoutCmdType.PropertyBool)]
		public bool? bOnlySpectator { get; set; } //Type: uint8 Bits: 1

        [NetFieldExport("bInGliderRedeploy", RepLayoutCmdType.PropertyBool)]
        public bool? bInGliderRedeploy { get; set; } //Type: uint8 Bits: 1

        [NetFieldExport("bIsSpectator", RepLayoutCmdType.PropertyBool)]
        public bool? bIsSpectator { get; set; } //Type:  Bits: 1

        [NetFieldExport("StartTime", RepLayoutCmdType.PropertyInt)]
		public int? StartTime { get; set; } //Type: int32 Bits: 32

		[NetFieldExport("UniqueId", RepLayoutCmdType.PropertyNetId)]
		public string UniqueId { get; set; } //Type: FUniqueNetIdRepl Bits: 144

		//Version 26
        [NetFieldExport("UniqueID", RepLayoutCmdType.PropertyNetId)]
        public string UniqueId_ { get; set; } //Type: FUniqueNetIdRepl Bits: 144

        [NetFieldExport("PlayerNamePrivate", RepLayoutCmdType.PropertyString)]
		public string PlayerNamePrivate { get; set; } //Type: FString Bits: 128

		[NetFieldExport("bIsGameSessionOwner", RepLayoutCmdType.PropertyBool)]
		public bool? bIsGameSessionOwner { get; set; } //Type:  Bits: 1

		[NetFieldExport("bHasFinishedLoading", RepLayoutCmdType.PropertyBool)]
		public bool? bHasFinishedLoading { get; set; } //Type: uint8 Bits: 1

		[NetFieldExport("bHasStartedPlaying", RepLayoutCmdType.PropertyBool)]
		public bool? bHasStartedPlaying { get; set; } //Type: uint8 Bits: 1

		[NetFieldExport("PlayerRole", RepLayoutCmdType.Ignore)]
		public int? PlayerRole { get; set; } //Type: EFortPlayerRole Bits: 0

		[NetFieldExport("PartyOwnerUniqueId", RepLayoutCmdType.PropertyNetId)]
		public string PartyOwnerUniqueId { get; set; } //Type: FUniqueNetIdRepl Bits: 144

		[NetFieldExport("WorldPlayerId", RepLayoutCmdType.PropertyInt16)]
		public short? WorldPlayerId { get; set; } //Type: int32 Bits: 32

		[NetFieldExport("HeroType", RepLayoutCmdType.Property)]
		public ItemDefinitionGUID HeroType { get; set; } //Type: UFortHeroType* Bits: 16

		[NetFieldExport("Platform", RepLayoutCmdType.PropertyString)]
		public string Platform { get; set; } //Type: FString Bits: 64

		[NetFieldExport("CharacterGender", RepLayoutCmdType.Enum)]
		public EFortCustomGender CharacterGender { get; set; } = EFortCustomGender.EFortCustomGender_MAX; //Type: TEnumAsByte<EFortCustomGender::Type> Bits: 2

		[NetFieldExport("CharacterBodyType", RepLayoutCmdType.Enum)]
		public EFortCustomBodyType CharacterBodyType { get; set; } = EFortCustomBodyType.EFortCustomBodyType_MAX; //Type: TEnumAsByte<EFortCustomBodyType::Type> Bits: 4

		[NetFieldExport("WasReplicatedFlags", RepLayoutCmdType.Ignore)]
		public byte? WasReplicatedFlags { get; set; } //Type: uint8 Bits: 8

		[NetFieldExport("Parts", RepLayoutCmdType.Property)]
		public ItemDefinitionGUID Parts { get; set; } //Type: UCustomCharacterPart* Bits: 16

		[NetFieldExport("WasPartReplicatedFlags", RepLayoutCmdType.Ignore)]
		public uint? WasPartReplicatedFlags { get; set; } //Type:  Bits: 8

		[NetFieldExport("RequiredVariantPartFlags", RepLayoutCmdType.PropertyInt)]
		public int? RequiredVariantPartFlags { get; set; } //Type:  Bits: 32

		[NetFieldExport("VariantRequiredCharacterParts", RepLayoutCmdType.DynamicArray)]
		public ItemDefinitionGUID[] VariantRequiredCharacterParts { get; set; } //Type:  Bits: 160

		[NetFieldExport("PlayerTeamPrivate", RepLayoutCmdType.Property)]
		public ActorGUID PlayerTeamPrivate { get; set; } //Type: AFortTeamPrivateInfo* Bits: 8

		[NetFieldExport("PlatformUniqueNetId", RepLayoutCmdType.PropertyNetId)]
		public string PlatformUniqueNetId { get; set; } //Type: FUniqueNetIdRepl Bits: 96

		[NetFieldExport("TeamIndex", RepLayoutCmdType.Enum)]
		public int? TeamIndex { get; set; } //Type: TEnumAsByte<EFortTeam::Type> Bits: 7

		[NetFieldExport("Place", RepLayoutCmdType.PropertyInt)]
		public int? Place { get; set; } //Type: int32 Bits: 32

		[NetFieldExport("ReplicatedTeamMemberState", RepLayoutCmdType.Enum)]
		public int? ReplicatedTeamMemberState { get; set; } //Type:  Bits: 4

		[NetFieldExport("bHasEverSkydivedFromBus", RepLayoutCmdType.PropertyBool)]
		public bool? bHasEverSkydivedFromBus { get; set; } //Type:  Bits: 1

		[NetFieldExport("bHasEverSkydivedFromBusAndLanded", RepLayoutCmdType.PropertyBool)]
		public bool? bHasEverSkydivedFromBusAndLanded { get; set; } //Type:  Bits: 1

		[NetFieldExport("SquadListUpdateValue", RepLayoutCmdType.Ignore)]
		public int? SquadListUpdateValue { get; set; } //Type: int32 Bits: 32

		[NetFieldExport("SquadId", RepLayoutCmdType.PropertyByte)]
		public byte? SquadId { get; set; } //Type: uint8 Bits: 8

		[NetFieldExport("bInAircraft", RepLayoutCmdType.PropertyBool)]
		public bool? bInAircraft { get; set; } //Type: uint8 Bits: 1

		[NetFieldExport("bThankedBusDriver", RepLayoutCmdType.PropertyBool)]
		public bool? bThankedBusDriver { get; set; } //Type: uint8 Bits: 1

		[NetFieldExport("bDidNotThankBusDriver", RepLayoutCmdType.PropertyBool)]
		public bool? bDidNotThankBusDriver { get; set; } //Type: uint8 Bits: 1

		[NetFieldExport("TeamKillScore", RepLayoutCmdType.PropertyUInt32)]
		public uint? TeamKillScore { get; set; } //Type:  Bits: 32

		[NetFieldExport("bUsingStreamerMode", RepLayoutCmdType.PropertyBool)]
		public bool? bUsingStreamerMode { get; set; } //Type: uint8 Bits: 1

		[NetFieldExport("StreamerModeName", RepLayoutCmdType.Property)]
		public FText StreamerModeName { get; set; } //Type: FText Bits: 448

		[NetFieldExport("TeamScorePlacement", RepLayoutCmdType.PropertyUInt32)]
		public uint? TeamScorePlacement { get; set; } //Type:  Bits: 32

		[NetFieldExport("IconId", RepLayoutCmdType.PropertyString)]
		public string IconId { get; set; } //Type: FString Bits: 144

		[NetFieldExport("TeamScore", RepLayoutCmdType.PropertyUInt32)]
		public uint? TeamScore { get; set; } //Type:  Bits: 32

		[NetFieldExport("ColorId", RepLayoutCmdType.PropertyString)]
		public string ColorId { get; set; } //Type: FString Bits: 152

		[NetFieldExport("Level", RepLayoutCmdType.PropertyInt)]
		public int? Level { get; set; } //Type: int32 Bits: 32

		[NetFieldExport("MapIndicatorPos", RepLayoutCmdType.Ignore)]
		public FVector2D MapIndicatorPos { get; set; } //Type: FVector2D Bits: 64

		[NetFieldExport("KillScore", RepLayoutCmdType.PropertyUInt32)]
		public uint? KillScore { get; set; } //Type:  Bits: 32

		[NetFieldExport("FinisherOrDowner", RepLayoutCmdType.PropertyObject)]
		public uint? FinisherOrDowner { get; set; } //Type: AFortPlayerStateAthena* Bits: 16

		[NetFieldExport("SeasonLevelUIDisplay", RepLayoutCmdType.PropertyUInt32)]
		public uint? SeasonLevelUIDisplay { get; set; } //Type:  Bits: 32

		[NetFieldExport("bInitialized", RepLayoutCmdType.PropertyBool)]
		public bool? bInitialized { get; set; } //Type: bool Bits: 1

		[NetFieldExport("DeathCircumstance", RepLayoutCmdType.PropertyInt)]
		public int? DeathCircumstance { get; set; } //Type: int32 Bits: 32

		[NetFieldExport("bUsingAnonymousMode", RepLayoutCmdType.PropertyBool)]
		public bool? bUsingAnonymousMode { get; set; } //Type:  Bits: 1

		[NetFieldExport("bIsDisconnected", RepLayoutCmdType.PropertyBool)]
		public bool? bIsDisconnected { get; set; } //Type:  Bits: 1

		[NetFieldExport("bDBNO", RepLayoutCmdType.PropertyBool)]
		public bool? bDBNO { get; set; } //Type:  Bits: 1

        [NetFieldExport("bWasDBNOOnDeath", RepLayoutCmdType.PropertyBool)]
        public bool? bWasDBNOOnDeath { get; set; } //Type:  Bits: 1

		[NetFieldExport("DeathCause", RepLayoutCmdType.Enum)]
		public EDeathCause DeathCause { get; set; } = EDeathCause.EDeathCause_MAX; //Type:  Bits: 6

		[NetFieldExport("Distance", RepLayoutCmdType.PropertyFloat)]
		public float? Distance { get; set; } //Type:  Bits: 32

		[NetFieldExport("DeathTags", RepLayoutCmdType.Property)]
		public FGameplayTagContainer DeathTags { get; set; } //Type:  Bits: 40

		[NetFieldExport("VictimTags", RepLayoutCmdType.Property)]
		public FGameplayTagContainer VictimTags { get; set; } //Type:  Bits: 40

		[NetFieldExport("FinisherOrDownerTags", RepLayoutCmdType.Property)]
		public FGameplayTagContainer FinisherOrDownerTags { get; set; } //Type:  Bits: 40

		[NetFieldExport("bResurrectionChipAvailable", RepLayoutCmdType.PropertyBool)]
		public bool? bResurrectionChipAvailable { get; set; } //Type:  Bits: 1

		[NetFieldExport("ResurrectionExpirationTime", RepLayoutCmdType.PropertyFloat)]
		public float? ResurrectionExpirationTime { get; set; } //Type:  Bits: 32

		[NetFieldExport("ResurrectionExpirationLength", RepLayoutCmdType.PropertyFloat)]
		public float? ResurrectionExpirationLength { get; set; } //Type:  Bits: 32

		[NetFieldExport("WorldLocation", RepLayoutCmdType.PropertyVector)]
		public FVector? WorldLocation { get; set; } //Type:  Bits: 96

		[NetFieldExport("bResurrectingNow", RepLayoutCmdType.PropertyBool)]
		public bool? bResurrectingNow { get; set; } //Type:  Bits: 1

		[NetFieldExport("RebootCounter", RepLayoutCmdType.PropertyUInt32)]
		public uint? RebootCounter { get; set; } //Type:  Bits: 32

		[NetFieldExport("bHoldsRebootVanLock", RepLayoutCmdType.PropertyBool)]
		public bool? bHoldsRebootVanLock { get; set; } //Type:  Bits: 1

		[NetFieldExport("BotUniqueId", RepLayoutCmdType.PropertyNetId)]
		public string BotUniqueId { get; set; } //Type:  Bits: 128

        [NetFieldExport("PlayerNameCustomOverride", RepLayoutCmdType.Property)]
        public FText PlayerNameCustomOverride { get; set; } 
        
        [NetFieldExport("SimulatedAttributes", RepLayoutCmdType.Ignore)]
        public DebuggingObject SimulatedAttributes { get; set; }

		[NetFieldExport("KickedFromSessionReason", RepLayoutCmdType.Enum)]
		public EFortKickReason KickedFromSessionReason { get; set; } = EFortKickReason.EFortKickReason_MAX;

		[NetFieldExport("DeathLocation", RepLayoutCmdType.PropertyVector)]
		public FVector? DeathLocation { get; set; }

		[NetFieldExport("Location", RepLayoutCmdType.PropertyVector)]
		public FVector? Location { get; set; }

		[NetFieldExport("CategoryBudgets", RepLayoutCmdType.DynamicArray)]
		public int[] CategoryBudgets { get; set; }

		//3 bits each
		[NetFieldExport("CategoryNames", RepLayoutCmdType.DynamicArray)]
		public int[] CategoryNames { get; set; }

		[NetFieldExport("CategoryValues", RepLayoutCmdType.DynamicArray)]
		public int[] CategoryValues { get; set; }

		[NetFieldExport("NeighborHeatValues", RepLayoutCmdType.PropertyByte)]
		public byte? NeighborHeatValues { get; set; }
	}
}
