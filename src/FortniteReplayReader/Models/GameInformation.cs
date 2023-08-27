using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FortniteReplayReader.Models.Enums;
using FortniteReplayReader.Models.NetFieldExports;
using FortniteReplayReader.Models.NetFieldExports.Builds;
using FortniteReplayReader.Models.NetFieldExports.ClassNetCaches.Custom;
using FortniteReplayReader.Models.NetFieldExports.ClassNetCaches.Functions;
using FortniteReplayReader.Models.NetFieldExports.Enums;
using FortniteReplayReader.Models.NetFieldExports.Items.Containers;
using FortniteReplayReader.Models.NetFieldExports.Items.Weapons;
using FortniteReplayReader.Models.NetFieldExports.Sets;
using FortniteReplayReader.Models.NetFieldExports.Vehicles;
using Unreal.Core.Models;
using Unreal.Core.Extensions;

namespace FortniteReplayReader.Models
{
    public class GameInformation
    {
#if DEBUG
        public UChannel[] Channels { get; internal set; }
#endif

        public ICollection<Llama> Llamas => _llamas.Values;
        public ICollection<SafeZone> SafeZones => _safeZones;
        public ICollection<StormSurge> StormSurges => _stormSurges;
        public ICollection<Player> Players => _players.Values;
        public ICollection<Team> Teams => _teams.Values;
        public ICollection<SupplyDrop> SupplyDrops => _supplyDrops.Values;
        public ICollection<PlayerReboot> Resurrections => _resurrections;
        public ICollection<InventoryItem> FloorLoot => _floorLoot.Values;
        public ICollection<KillFeedEntry> KillFeed => _killFeed;
        public ICollection<PlayerStructure> PlayerStructures => _playerStructures.Values;

        public GameState GameState { get; private set; } = new GameState();
        public EncryptionKey PlayerStateEncryptionKey { get; internal set; }
        public FortniteReplaySettings Settings { get; internal set; }
        public MinigameInformation MinigameInformation { get; internal set; }

        private Dictionary<uint, uint> _actorToChannel = new Dictionary<uint, uint>(); //Set an amount to use

        private Dictionary<uint, Llama> _llamas = new Dictionary<uint, Llama>(); //Channel to llama
        private Dictionary<uint, SupplyDrop> _supplyDrops = new Dictionary<uint, SupplyDrop>(); //Channel supply drop

        /// <summary>
        /// Requires Full parse mode
        /// </summary>
        private Dictionary<uint, InventoryItem> _floorLoot = new Dictionary<uint, InventoryItem>(); //Channel Id to InventoryItem. These will be removed/added over time

        private Dictionary<uint, Player> _players = new Dictionary<uint, Player>(); //Channel id to Player
        private Dictionary<uint, PlayerPawn> _playerPawns = new Dictionary<uint, PlayerPawn>(); //Channel Id (player pawn) to Actor
        private Dictionary<uint, List<QueuedPlayerPawn>> _queuedPlayerPawns = new Dictionary<uint, List<QueuedPlayerPawn>>();
        private Dictionary<uint, Weapon> _weapons = new Dictionary<uint, Weapon>(); //Channel to weapon
        private Dictionary<uint, Weapon> _unknownWeapons = new Dictionary<uint, Weapon>(); //Channel to weapon
        private Dictionary<string, Container> _containers = new Dictionary<string, Container>(); //Channel to searchable containers
        private Dictionary<uint, PlayerStructure> _playerStructures = new Dictionary<uint, PlayerStructure>(); //Channel to player structures
        private Dictionary<string, uint> _healthSetStartingHandles = new Dictionary<string, uint>(); //Starting handle ids for a health set
        private Dictionary<uint, Vehicle> _vehicles = new Dictionary<uint, Vehicle>(); //Channel id to vehicle
        private Dictionary<string, Player> _playerById = new Dictionary<string, Player>(); //Used only for minigame replays

        private Player _replayPlayer;
        private uint _closedPlayerChannelId = uint.MaxValue; //Used to keep the players in the "Players" list when the channel is closed

        //Delta updates
        private Dictionary<uint, NetDeltaArray<PrivateTeamInfo>> _privateTeamInfo = new Dictionary<uint, NetDeltaArray<PrivateTeamInfo>>(); //Channel id to private team info
        private Dictionary<uint, FortInventory> _inventories = new Dictionary<uint, FortInventory>(); //Channel to inventory items

        private Dictionary<int, Team> _teams = new Dictionary<int, Team>();
        private List<SafeZone> _safeZones = new List<SafeZone>();
        private List<StormSurge> _stormSurges = new List<StormSurge>();
        private List<PlayerReboot> _resurrections = new List<PlayerReboot>();
        private List<KillFeedEntry> _killFeed = new List<KillFeedEntry>();

        internal Dictionary<uint, string> NetGUIDToPathName { get; set; }

        internal void ChannelClosed(uint channel)
        {
            if(_players.TryGetValue(channel, out Player player))
            {
                player.ChannelClosed = true;
            }

            _playerPawns.Remove(channel);
            _vehicles.Remove(channel);
            _weapons.Remove(channel);
            _floorLoot.Remove(channel);
            _playerStructures.Remove(channel);
        }

        internal void AddActor(uint channel, Actor actor)
        {
            _actorToChannel[actor.ActorNetGUID.Value] = channel;
        }

        internal void UpdatePrivateTeamInfo(uint channelId, FortTeamPrivateInfo privateTeamInfo)
        {
            _privateTeamInfo[channelId] = new NetDeltaArray<PrivateTeamInfo>();
        }

        internal void UpdateLlama(uint channelId, SupplyDropLlamaC supplyDropLlama)
        {
            Llama newLlama = new Llama();

            if (!_llamas.TryAdd(channelId, newLlama))
            {
                _llamas.TryGetValue(channelId, out newLlama);
            }

            newLlama.Location = supplyDropLlama.ReplicatedMovement?.Location ?? newLlama.Location;
            newLlama.Opened = supplyDropLlama.Looted ?? newLlama.Opened;
            newLlama.Destroyed = supplyDropLlama.bDestroyed ?? newLlama.Destroyed;
            newLlama.SpawnedItems = supplyDropLlama.bHasSpawnedPickups ?? newLlama.SpawnedItems;
        }

        internal void UpdateSearchableContainer(string staticActorId, SearchableContainer searchableContainer)
        {
            Container newContainer = new Container();

            if (!_containers.TryAdd(staticActorId, newContainer))
            {
                _containers.TryGetValue(staticActorId, out newContainer);
            }

            newContainer.bDestroyed = searchableContainer.bDestroyed ?? newContainer.bDestroyed;
            newContainer.BuildTime = searchableContainer.BuildTime ?? newContainer.BuildTime;
            newContainer.RepairTime = searchableContainer.RepairTime ?? newContainer.RepairTime;
            newContainer.Health = searchableContainer.Health ?? newContainer.Health;
            newContainer.MaxHealth = searchableContainer.MaxHealth ?? newContainer.MaxHealth;
            newContainer.ReplicatedLootTier = searchableContainer.ReplicatedLootTier ?? newContainer.ReplicatedLootTier;
            newContainer.SearchAnimationCount = searchableContainer.SearchAnimationCount ?? newContainer.SearchAnimationCount;
            newContainer.bAlreadySearched = searchableContainer.bAlreadySearched ?? newContainer.bAlreadySearched;
            newContainer.ResourceType = searchableContainer.ResourceType ?? newContainer.ResourceType;
            newContainer.ForceMetadataRelevant = searchableContainer.ForceMetadataRelevant ?? newContainer.ForceMetadataRelevant;
            newContainer.StaticMesh = searchableContainer.StaticMesh ?? newContainer.StaticMesh;
            newContainer.WeaponData  = searchableContainer.WeaponData  ?? newContainer.WeaponData ;
            newContainer.bDestroyOnPlayerBuildingPlacement = searchableContainer.bDestroyOnPlayerBuildingPlacement ?? newContainer.bDestroyOnPlayerBuildingPlacement;
            newContainer.bInstantDeath = searchableContainer.bInstantDeath ?? newContainer.bInstantDeath;
            newContainer.SearchedMesh = searchableContainer.SearchedMesh ?? newContainer.SearchedMesh;
            newContainer.AltMeshIdx = searchableContainer.AltMeshIdx ?? newContainer.AltMeshIdx;
            newContainer.ProxyGameplayCueDamagePhysicalMagnitude = searchableContainer.ProxyGameplayCueDamagePhysicalMagnitude ?? newContainer.ProxyGameplayCueDamagePhysicalMagnitude;
            newContainer.EffectContext = searchableContainer.EffectContext ?? newContainer.EffectContext;
            newContainer.ChosenRandomUpgrade = searchableContainer.ChosenRandomUpgrade ?? newContainer.ChosenRandomUpgrade;
            newContainer.bMirrored = searchableContainer.bMirrored ?? newContainer.bMirrored;
            newContainer.ReplicatedDrawScale3D = searchableContainer.ReplicatedDrawScale3D ?? newContainer.ReplicatedDrawScale3D;
            newContainer.bIsInitiallyBuilding = searchableContainer.bIsInitiallyBuilding ?? newContainer.bIsInitiallyBuilding;
            newContainer.bForceReplayRollback = searchableContainer.bForceReplayRollback ?? newContainer.bForceReplayRollback;
        }

        internal void UpdatePlaylistInfo(uint channelId, CurrentPlaylistInfo playlistInfo)
        {
            if (NetGUIDToPathName.TryGetValue(playlistInfo.Id.Value, out string playlistId))
            {
                GameState.PlaylistId = playlistId;
            }
        }

        internal void UpdateSupplyDrop(uint channelId, SupplyDropC supplyDrop)
        {
            SupplyDrop newSupplyDrop = new SupplyDrop
            {
                SpawnTime = GameState.DeltaGameTime
            };

            if (!_supplyDrops.TryAdd(channelId, newSupplyDrop))
            {
                _supplyDrops.TryGetValue(channelId, out newSupplyDrop);
            }

            newSupplyDrop.Location = supplyDrop.LandingLocation ?? newSupplyDrop.Location;
            newSupplyDrop.Opened = supplyDrop.Opened ?? newSupplyDrop.Opened;
            newSupplyDrop.Destroyed = supplyDrop.bDestroyed ?? newSupplyDrop.Destroyed;
            newSupplyDrop.SpawnedItems = supplyDrop.bHasSpawnedPickups ?? newSupplyDrop.SpawnedItems;
            newSupplyDrop.BalloonPopped = supplyDrop.BalloonPopped ?? newSupplyDrop.BalloonPopped;
        }

        internal void UpdateSafeZone(SafeZoneIndicatorC safeZone)
        {
            SafeZone newSafeZone = _safeZones.LastOrDefault();

            if (safeZone.SafeZoneFinishShrinkTime != null)
            {
                newSafeZone = new SafeZone();

                _safeZones.Add(newSafeZone);

                newSafeZone.Radius = safeZone.Radius ?? newSafeZone.Radius;
                newSafeZone.NextNextRadius = safeZone.NextNextRadius ?? newSafeZone.NextNextRadius;
                newSafeZone.NextRadius = safeZone.NextRadius ?? newSafeZone.NextRadius;
                newSafeZone.ShringEndTime = safeZone.SafeZoneFinishShrinkTime ?? newSafeZone.ShringEndTime;
                newSafeZone.ShrinkStartTime = safeZone.SafeZoneStartShrinkTime ?? newSafeZone.ShrinkStartTime;
                newSafeZone.LastCenter = safeZone.LastCenter ?? newSafeZone.LastCenter;
                newSafeZone.NextCenter = safeZone.NextCenter ?? newSafeZone.NextCenter;
                newSafeZone.NextNextCenter = safeZone.NextNextCenter ?? newSafeZone.NextNextCenter;
            }

            newSafeZone.CurrentRadius = safeZone.Radius ?? newSafeZone.CurrentRadius;
        }

        internal void UpdateGameState(GameStateC gameState)
        {
            GameState.AirCraftStartTime = gameState.AircraftStartTime ?? GameState.AirCraftStartTime;

            GameState.InitialSafeZoneStartTime = gameState.SafeZonesStartTime ?? GameState.InitialSafeZoneStartTime;
            GameState.SessionId = gameState.GameSessionId ?? GameState.SessionId;
            GameState.MatchTime = gameState.UtcTimeStartedMatch?.Time ?? GameState.MatchTime;
            GameState.EEventTournamentRound = gameState.EventTournamentRound != EEventTournamentRound.EEventTournamentRound_MAX ? gameState.EventTournamentRound : GameState.EEventTournamentRound;
            GameState.LargeTeamGame = gameState.bIsLargeTeamGame ?? GameState.LargeTeamGame;
            GameState.MaxPlayers = gameState.TeamCount ?? GameState.MaxPlayers;
            GameState.MatchEndTime = gameState.EndGameStartTime ?? GameState.MatchEndTime;
            GameState.TotalTeams = gameState.ActiveTeamNums?.Length ?? GameState.TotalTeams;
            GameState.TotalBots = gameState.PlayerBotsLeft > GameState.TotalBots ? gameState.PlayerBotsLeft.Value : GameState.TotalBots;

            //Internal information to keep track of current state of the game
            GameState.CurrentWorldTime = gameState.ReplicatedWorldTimeSeconds ?? GameState.CurrentWorldTime;
            GameState.RemainingPlayers = gameState.PlayersLeft ?? GameState.RemainingPlayers;
            GameState.CurrentTeams = gameState.TeamsLeft ?? GameState.CurrentTeams;
            GameState.SafeZonePhase = gameState.SafeZonePhase ?? GameState.SafeZonePhase;
            GameState.RemainingBots = gameState.PlayerBotsLeft ?? GameState.RemainingBots;
            GameState.ElapsedTime = gameState.ElapsedTime ?? GameState.ElapsedTime;
            GameState.OldTeamSize = gameState.TeamSize ?? GameState.OldTeamSize;
            GameState.TotalPlayerStructures = gameState.TotalPlayerStructures ?? GameState.TotalPlayerStructures;
            GameState.RecorderActor = gameState.RecorderPlayerState?.Value ?? GameState.RecorderActor;

            if (GameState.GameWorldStartTime == 0)
            {
                GameState.GameWorldStartTime = gameState.ReplicatedWorldTimeSeconds ?? GameState.AirCraftStartTime;
            }

            if (gameState.TeamFlightPaths != null)
            {
                foreach (GameStateC flightPath in gameState.TeamFlightPaths)
                {
                    GameState.BusPaths.Add(new Aircraft
                    {
                        FlightRotation = flightPath.FlightStartRotation,
                        FlightStartLocation = flightPath.FlightStartLocation,
                    });
                }
            }

            if (gameState.StormCapState != EAthenaStormCapState.EAthenaStormCapState_MAX)
            {
                UpdateStormSurges(gameState.StormCapState, GameState.CurrentWorldTime);
            }

            if (gameState.WinningTeam.HasValue)
            {
                GameState.WinningTeam = gameState.WinningTeam.Value;

                if (_teams.TryGetValue((int)GameState.WinningTeam, out Team team))
                {
                    foreach (Player player in team.Players)
                    {
                        if (player.Placement == 0)
                        {
                            player.Placement = 1;
                        }
                    }
                }
            }
        }

        private void UpdateStormSurges(EAthenaStormCapState state, float time)
        {
            if (state == EAthenaStormCapState.Clear && _stormSurges.Count == 0)
            {
                return;
            }

            if (state == EAthenaStormCapState.Warning || _stormSurges.Count == 0)
            {
                _stormSurges.Add(new StormSurge());
            }

            StormSurge surge = _stormSurges.Last();

            // Sometimes will get duplicate states in a row, so
            // make sure we're not overriding the time
            if (state == EAthenaStormCapState.Warning && surge.WarningTime == 0)
            {
                surge.WarningTime = time;
            }
            else if (state == EAthenaStormCapState.Damaging && surge.DamageTime == 0)
            {
                surge.DamageTime = time;
            }
            else if (state == EAthenaStormCapState.Clear && surge.ClearTime == 0)
            {
                surge.ClearTime = time;
            }
        }

        internal void UpdateContainer(uint channelId, BaseProp container)
        {

        }

        internal void UpdateKillFeed(uint channelId, FortPlayerState playerState)
        {
            KillFeedEntry entry = new KillFeedEntry();
            entry.DeltaGameTimeSeconds = GameState.CurrentWorldTime - GameState.GameWorldStartTime;
            entry.DeathTags = playerState.DeathTags?.Tags.Select(x => x.TagName).ToArray();
            entry.Distance = playerState.Distance ?? 0;

            if (!_players.TryGetValue(channelId, out Player channelPlayer))
            {
                //Shouldn't happen
                entry.HasError = true;

                _killFeed.Add(entry);

                return;
            }

            if (playerState.RebootCounter != null)
            {
                entry.CurrentPlayerState = PlayerState.Revived;
                entry.Player = channelPlayer;
                entry.FinisherOrDowner = channelPlayer; //Unknown, so figure this out if possible
                entry.ItemType = ItemType.RebootVan;
            }
            else
            {
                Player eliminator = channelPlayer.LastKnockedEntry?.FinisherOrDowner;

                if (playerState.FinisherOrDowner != null)
                {
                    if (playerState.FinisherOrDowner == 0)
                    {
                        //DBNO revives?
                        if (playerState.DeathCause != EDeathCause.EDeathCause_MAX)
                        {
                            entry.Player = channelPlayer;
                            entry.CurrentPlayerState = PlayerState.Alive;
                            channelPlayer.LastKnockedEntry = null;
                            channelPlayer.StatusChanges.Add(entry);
                            _killFeed.Add(entry);
                        }

                        return;
                    }

                    if (!_actorToChannel.TryGetValue(playerState.FinisherOrDowner.Value, out uint channel) || !_players.TryGetValue(channel, out eliminator))
                    {
                        entry.HasError = true;

                        _killFeed.Add(entry);

                        return;
                    }
                }

                if (playerState.bDBNO != null)
                {
                    if (playerState.bDBNO == true)
                    {
                        channelPlayer.LastKnockedEntry = entry;
                    }

                    entry.Player = channelPlayer;
                    entry.FinisherOrDowner = eliminator;
                    entry.CurrentPlayerState = playerState.bDBNO == true ? PlayerState.Knocked : PlayerState.Killed;

                    entry.DeathCause = playerState.DeathCause;

                    if (entry.DeathCause == EDeathCause.EDeathCause_MAX)
                    {
                        if (channelPlayer.LastKnockedEntry?.DeathCause != null)
                        {
                            entry.DeathCause = channelPlayer.LastKnockedEntry.DeathCause;
                        }
                    }

                    if (entry.DeathTags == null)
                    {
                        entry.DeathTags = channelPlayer.LastKnockedEntry?.DeathTags ?? null;
                    }
                }
                else //Full kill
                {
                    entry.Player = channelPlayer;
                    entry.FinisherOrDowner = eliminator;
                    entry.CurrentPlayerState = PlayerState.Killed;
                    entry.DeathCause = playerState.DeathCause;
                }
            }

            channelPlayer.StatusChanges.Add(entry);
            _killFeed.Add(entry);
        }

        internal void HandleGameplayCue(uint channelId, GameplayCue cue)
        {
            /*
	            GameplayCue.Abilities.Activation.DBNOResurrect.Athena -  - 7862
	            GameplayCue.Abilities.Activation.Traps.ActivateTrap -  - 7911
	            GameplayCue.Abilities.Activation.Traps.DelayBegin -  - 7912
	            GameplayCue.Abilities.Activation.Traps.Placed -  - 7917
	            GameplayCue.Abilities.Activation.Traps.ReloadBegin -  - 7918
	            GameplayCue.Athena.DBNOBleed -  - 8224
	            GameplayCue.Athena.EnvCampfire.Doused -  - 8235
	            GameplayCue.Athena.EnvCampfire.Fire -  - 8236
	            GameplayCue.Athena.EnvItem.Slurp -  - 8240
	            GameplayCue.Athena.Equipping -  - 8241
	            GameplayCue.Athena.Item.FloppingRabbit.Cast -  - 8353
	            GameplayCue.Athena.OutsideSafeZoneDamage -  - 8448
	            GameplayCue.Athena.Player.BeingRevivedFromDBNO -  - 8454
	            GameplayCue.Athena.Player.BigBushMovement -  - 8455
	            GameplayCue.Athena.Player.CornFieldMovement -  - 8458
	            GameplayCue.Damage.Physical -  - 8610
	            GameplayCue.Shield.PotionConsumed -  - 9012
	            GameplayCue.Weapons.Activation -  - 9080
            */
        }

        internal void UpdatePlayerState(uint channelId, FortPlayerState playerState, NetFieldExportGroup networkGameplayTagNode)
        {
            if (playerState.bOnlySpectator == true)
            {
                return;
            }

            bool isNewPlayer = !_players.TryGetValue(channelId, out Player newPlayer) || newPlayer.ChannelClosed;

            if (isNewPlayer)
            {
                //Channel is being replaced by a new player. Store the old player
                if(newPlayer?.ChannelClosed == true)
                {
                    _players.Add(_closedPlayerChannelId--, newPlayer);
                }

                newPlayer = new Player();

                //Check if recording player
                if(_actorToChannel.TryGetValue(GameState.RecorderActor, out uint channel) && channelId == channel)
                {
                    newPlayer.IsPlayersReplay = true;
                    GameState.ReplayRecorder = newPlayer;
                }

                _players[channelId] = newPlayer;
            }

            if (networkGameplayTagNode != null)
            {
                playerState.DeathTags?.UpdateTags(networkGameplayTagNode);
                playerState.VictimTags?.UpdateTags(networkGameplayTagNode);
                playerState.FinisherOrDownerTags?.UpdateTags(networkGameplayTagNode);
            }

            //Attempt to update private team info to correct object
            if (newPlayer.PrivateTeamActorId != null && newPlayer.PrivateTeamInfo == null)
            {
                if (_actorToChannel.TryGetValue(newPlayer.PrivateTeamActorId.Value, out uint teamActorChannel))
                {
                    if (_privateTeamInfo.TryGetValue(teamActorChannel, out NetDeltaArray<PrivateTeamInfo> teamInfo))
                    {
                        newPlayer.PrivateTeamInfo = teamInfo;
                    }
                }
            }

            newPlayer.Actor = playerState.ChannelActor;

            newPlayer.EpicId = playerState.UniqueId ?? playerState.UniqueId_ ?? newPlayer.EpicId;
            //newPlayer.PrivatePlayerName = playerState.PlayerNamePrivate ?? newPlayer.PrivatePlayerName;
            newPlayer.Platform = playerState.Platform ?? newPlayer.Platform;
            newPlayer.Teamindex = playerState.TeamIndex ?? newPlayer.Teamindex;
            newPlayer.PartyOwnerEpicId = playerState.PartyOwnerUniqueId ?? newPlayer.PartyOwnerEpicId;
            newPlayer.IsBot = playerState.bIsABot ?? newPlayer.IsBot;
            newPlayer.BotId = playerState.BotUniqueId ?? newPlayer.BotId;
            newPlayer.IsGameSessionOwner = playerState.bIsGameSessionOwner ?? newPlayer.IsGameSessionOwner;
            newPlayer.Level = playerState.Level ?? newPlayer.Level;
            newPlayer.FinishedLoading = playerState.bHasFinishedLoading ?? newPlayer.FinishedLoading;
            newPlayer.StartedPlaying = playerState.bHasStartedPlaying ?? newPlayer.StartedPlaying;
            newPlayer.StreamerMode = playerState.bUsingStreamerMode ?? newPlayer.StreamerMode;
            newPlayer.ThankedBusDriver = playerState.bThankedBusDriver ?? newPlayer.ThankedBusDriver;
            newPlayer.Placement = playerState.Place ?? newPlayer.Placement;
            newPlayer.TotalKills = playerState.KillScore ?? newPlayer.TotalKills;
            newPlayer.TeamKills = playerState.TeamKillScore ?? newPlayer.TeamKills;
            newPlayer.Disconnected = playerState.bIsDisconnected ?? newPlayer.Disconnected;
            newPlayer.AnonMode = playerState.bUsingAnonymousMode ?? newPlayer.AnonMode;
            newPlayer.PrivateTeamActorId = playerState.PlayerTeamPrivate ?? newPlayer.PrivateTeamActorId;
            newPlayer.Cosmetics.ECharacterBodyType = playerState.CharacterBodyType == EFortCustomBodyType.EFortCustomBodyType_MAX ? playerState.CharacterBodyType : newPlayer.Cosmetics.ECharacterBodyType;
            newPlayer.Cosmetics.HeroType = GetPathName(playerState.HeroType) ?? newPlayer.Cosmetics.HeroType;
            newPlayer.Cosmetics.ECharacterGender = playerState.CharacterGender == EFortCustomGender.EFortCustomGender_MAX ? playerState.CharacterGender : newPlayer.Cosmetics.ECharacterGender;
            newPlayer.Cosmetics.Parts = GetPathName(playerState.Parts) ?? newPlayer.Cosmetics.Parts;
            newPlayer.MovementInformation.InBus = playerState.bInAircraft ?? newPlayer.MovementInformation.InBus;

            // Change how this is handled
            if(playerState.ExternalData != null)
            {
                if(playerState.ExternalData.HandleName == "PlayerNamePrivate")
                {
                    newPlayer.ExternalPlayerData = ExternalPlayerNameData.Parse(playerState.ExternalData.Data);
                }
            }

            if (playerState.VariantRequiredCharacterParts != null)
            {
                newPlayer.Cosmetics.VariantRequiredCharacterParts = playerState.VariantRequiredCharacterParts.Select(x => GetPathName(x)).ToList();
            }

            if (newPlayer.IsPlayersReplay)
            {
                _replayPlayer = newPlayer;
            }

            if (playerState.DeathCause != EDeathCause.EDeathCause_MAX)
            {
                newPlayer.LastDeathOrKnockTime = GameState.CurrentWorldTime;

                //Only occurs on main player or server replays
                if (newPlayer.CurrentInventory.Count > 0)
                {
                    newPlayer.InventoryOnDeath.Clear();
                    newPlayer.InventoryOnDeath.AddRange(newPlayer.InventoryBeforeDeletes);
                }
            }

            if (playerState.TeamIndex != null)
            {
                //Only should populate the teams when we aren't in a round game. Should use the round teams to determine teams
                if (MinigameInformation == null || MinigameInformation.TotalRounds <= 1)
                {
                    if (!_teams.TryGetValue(playerState.TeamIndex.Value, out Team team))
                    {
                        team = new Team();

                        _teams.TryAdd(playerState.TeamIndex.Value, team);
                    }

                    team.Players.Add(newPlayer);
                    newPlayer.Team = team;
                }
            }


            //Add to killfeed
            if (playerState.bDBNO != null || playerState.FinisherOrDowner != null || playerState.RebootCounter != null)
            {
                UpdateKillFeed(channelId, playerState);
            }

            if (playerState.bIsDisconnected == true && newPlayer.StatusChanges.LastOrDefault()?.CurrentPlayerState != PlayerState.Killed)
            {
                KillFeedEntry entry = new KillFeedEntry
                {
                    FinisherOrDowner = newPlayer,
                    CurrentPlayerState = PlayerState.Killed,
                    Player = newPlayer,
                    DeltaGameTimeSeconds = GameState.DeltaGameTime
                };

                newPlayer.StatusChanges.Add(entry);

                _killFeed.Add(entry);
            }

            if (playerState.bHasEverSkydivedFromBusAndLanded != null)
            {
                if (newPlayer.Locations.Count > 0)
                {
                    newPlayer.LandingLocation = newPlayer.Locations.Last();
                }
            }

            //Internal info
            newPlayer.WorldPlayerId = playerState.WorldPlayerId ?? newPlayer.WorldPlayerId;

            if (isNewPlayer)
            {
                HandleQueuedPlayerPawns(newPlayer);
            }

            if (MinigameInformation != null)
            {
                _playerById[newPlayer.EpicId] = newPlayer;
            }
        }

        internal void UpdatePlayerPawn(uint channelId, PlayerPawnC playerPawnC, bool trackLocations = true)
        {
            if (!_playerPawns.TryGetValue(channelId, out PlayerPawn playerpawn))
            {
                //Check for PlayerState
                if (playerPawnC.PlayerState != null)
                {
                    if (_actorToChannel.TryGetValue(playerPawnC.PlayerState.Value, out uint playerStateChannel) && _players.TryGetValue(playerStateChannel, out Player player))
                    {
                        _players.TryGetValue(playerStateChannel, out Player p);
                        _playerPawns.TryAdd(channelId, player);

                        playerpawn = player;
                    }
                    else
                    {
                        //Queues up player pawn to process for later
                        if (!_queuedPlayerPawns.TryGetValue(playerPawnC.PlayerState.Value, out List<QueuedPlayerPawn> playerPawns))
                        {
                            playerPawns = new List<QueuedPlayerPawn>();

                            _queuedPlayerPawns.TryAdd(playerPawnC.PlayerState.Value, playerPawns);
                        }

                        playerPawns.Add(new QueuedPlayerPawn
                        {
                            ChannelId = channelId,
                            PlayerPawn = (PlayerPawnC)playerPawnC.Clone() //PlayerPawnC is a single instance class, so need to clone it
                        });

                        return;
                    }
                }
                else
                {
                    return;
                }
            }

            switch (playerpawn)
            {
                case Player playerActor:
                    playerActor.LastTransformUpdate = playerPawnC.ReplayLastTransformUpdateTimeStamp ?? playerActor.LastTransformUpdate;

                    //Update movement info
                    playerActor.MovementInformation.Skydiving = playerPawnC.bIsSkydiving ?? playerActor.MovementInformation.Skydiving;
                    playerActor.MovementInformation.Crouched = playerPawnC.bIsCrouched ?? playerActor.MovementInformation.Crouched;
                    playerActor.MovementInformation.GliderOpen = playerPawnC.bIsParachuteOpen ?? playerActor.MovementInformation.GliderOpen;
                    playerActor.MovementInformation.IsEmoting = playerPawnC.bIsPlayingEmote ?? playerActor.MovementInformation.IsEmoting;
                    playerActor.MovementInformation.IsInteracting = playerPawnC.bStartedInteractSearch ?? playerActor.MovementInformation.IsInteracting;
                    playerActor.MovementInformation.IsSlopeSliding = playerPawnC.bIsSlopeSliding ?? playerActor.MovementInformation.IsSlopeSliding;
                    playerActor.MovementInformation.IsTargeting = playerPawnC.bIsTargeting ?? playerActor.MovementInformation.IsTargeting;
                    playerActor.MovementInformation.Sprinting = playerPawnC.CurrentMovementStyle != EFortMovementStyle.EFortMovementStyle_MAX ?
                        playerPawnC.CurrentMovementStyle == EFortMovementStyle.Sprinting : playerActor.MovementInformation.Sprinting;
                    playerActor.MovementInformation.JumpedForceApplied = playerPawnC.bProxyIsJumpForceApplied ?? playerActor.MovementInformation.JumpedForceApplied;
                    playerActor.MovementInformation.IsInWater = playerPawnC.ReplicatedWaterBody != null ? playerPawnC.ReplicatedWaterBody.Value > 0 : playerActor.MovementInformation.IsInWater;

                    playerActor.MovementInformation.MovementStyle = playerPawnC.CurrentMovementStyle != EFortMovementStyle.EFortMovementStyle_MAX ? playerPawnC.CurrentMovementStyle : playerActor.MovementInformation.MovementStyle;

                    float currentMovementDeltaTime = 0;

                    if (playerPawnC.ReplayLastTransformUpdateTimeStamp.HasValue)
                    {
                        if (playerActor.InitialMovementTimestamp == 0)
                        {
                            playerActor.InitialMovementTimestamp = playerPawnC.ReplayLastTransformUpdateTimeStamp.Value;
                        }

                        currentMovementDeltaTime = playerPawnC.ReplayLastTransformUpdateTimeStamp.Value - playerActor.InitialMovementTimestamp;
                    }

                    if (!IgnoreLocationUpdate(playerActor, currentMovementDeltaTime))
                    {
                        PlayerLocationRepMovement fRepMovement = null;

                        if (playerPawnC.Vehicle != null) //Player got in vehicle, so grab vehicle location
                        {
                            if (playerPawnC.Vehicle.Value > 0)
                            {
                                if (_actorToChannel.TryGetValue(playerPawnC.Vehicle.Value, out uint actorChannel) &&
                                    _vehicles.TryGetValue(actorChannel, out Vehicle vehicle))
                                {
                                    fRepMovement = vehicle.CurrentLocation;
                                    fRepMovement.InVehicle = true;
                                    fRepMovement.VehicleChannel = actorChannel;
                                    fRepMovement.CurrentPlayerState = playerActor.StatusChanges.LastOrDefault()?.CurrentPlayerState ?? PlayerState.Alive;
                                    fRepMovement.DeltaGameTimeSeconds = currentMovementDeltaTime;
                                }
                                else
                                {
                                    //Missing vehicle
                                }
                            }
                        }
                        else if (playerActor.LastKnownLocation?.InVehicle == true) //Still in vehicle, use vehicle locations
                        {
                            if (_vehicles.TryGetValue(playerActor.LastKnownLocation.VehicleChannel, out Vehicle vehicle))
                            {
                                fRepMovement = vehicle.CurrentLocation;
                                fRepMovement.InVehicle = true;
                                fRepMovement.VehicleChannel = playerActor.LastKnownLocation.VehicleChannel;
                                fRepMovement.CurrentPlayerState = playerActor.StatusChanges.LastOrDefault()?.CurrentPlayerState ?? PlayerState.Alive;
                                fRepMovement.DeltaGameTimeSeconds = currentMovementDeltaTime;
                            }
                        }

                        if (playerPawnC.ReplicatedMovement != null)
                        {
                            fRepMovement = new PlayerLocationRepMovement
                            {
                                RepLocation = playerPawnC.ReplicatedMovement,
                                WorldTime = GameState.CurrentWorldTime,
                                LastUpdateTime = playerPawnC.ReplayLastTransformUpdateTimeStamp,
                                DeltaGameTimeSeconds = currentMovementDeltaTime,
                                CurrentPlayerState = playerActor.StatusChanges.LastOrDefault()?.CurrentPlayerState ?? PlayerState.Alive,
                                MovementInformation = playerActor.MovementInformation.Copy()
                            };
                        }

                        if (fRepMovement != null)
                        {
                            if (trackLocations)
                            {
                                playerActor.Locations.Add(fRepMovement);
                            }

                            playerActor.LastKnownLocation = fRepMovement;
                        }
                    }

                    //Update current weapon
                    if (!Settings.IgnoreWeaponSwitches && playerPawnC.CurrentWeapon != null && playerPawnC.CurrentWeapon.Value > 0) //0 Occurs on death
                    {
                        if (_actorToChannel.TryGetValue(playerPawnC.CurrentWeapon.Value, out uint weaponChannel))
                        {
                            if (_weapons.TryGetValue(weaponChannel, out Weapon weapon))
                            {
                                //Handle weapon changes
                                playerActor.WeaponSwitches.Add(new WeaponSwitch
                                {
                                    Weapon = weapon,
                                    State = WeaponSwitchState.Equipped,
                                    DeltaGameTimeSeconds = GameState.CurrentWorldTime - GameState.GameWorldStartTime
                                });

                                playerActor.CurrentWeapon = weapon;
                            }
                            else
                            {
                                //Useful to debug weapons that aren't added
                                /*var a = Channels[weaponChannel];

                                Console.WriteLine($"{GameState.DeltaGameTime} - {(a.Group.FirstOrDefault() ?? "Empty")}");
                                */
                            }
                        }
                        else
                        {
                            Weapon weapon = new Weapon();

                            _unknownWeapons.TryAdd(playerPawnC.CurrentWeapon.Value, weapon);

                            playerActor.WeaponSwitches.Add(new WeaponSwitch
                            {
                                Weapon = weapon,
                                State = WeaponSwitchState.Equipped,
                                DeltaGameTimeSeconds = GameState.CurrentWorldTime - GameState.GameWorldStartTime
                            });
                        }
                    }

                    HandleCosmetics(playerActor, playerPawnC);

                    break;
            }

            void HandleCosmetics(Player playerActor, PlayerPawnC playerPawnC)
            {
                playerActor.Cosmetics.Backpack = GetPathName(playerPawnC.Backpack) ?? playerActor.Cosmetics.Backpack;
                playerActor.Cosmetics.BannerColorId = playerPawnC.BannerColorId ?? playerActor.Cosmetics.BannerColorId;
                playerActor.Cosmetics.BannerIconId = playerPawnC.BannerIconId ?? playerActor.Cosmetics.BannerIconId;
                playerActor.Cosmetics.Character = GetPathName(playerPawnC.Character) ?? playerActor.Cosmetics.Character;
                playerActor.Cosmetics.IsDefaultCharacter = playerPawnC.bIsDefaultCharacter ?? playerActor.Cosmetics.IsDefaultCharacter;
                playerActor.Cosmetics.PetSkin = GetPathName(playerPawnC.PetSkin) ?? playerActor.Cosmetics.PetSkin;
                playerActor.Cosmetics.Glider = GetPathName(playerPawnC.Glider) ?? playerActor.Cosmetics.Glider;
                playerActor.Cosmetics.LoadingScreen = GetPathName(playerPawnC.LoadingScreen) ?? playerActor.Cosmetics.LoadingScreen;
                playerActor.Cosmetics.MusicPack = GetPathName(playerPawnC.MusicPack) ?? playerActor.Cosmetics.MusicPack;
                playerActor.Cosmetics.Pickaxe = GetPathName(playerPawnC.Pickaxe) ?? playerActor.Cosmetics.Pickaxe;
                playerActor.Cosmetics.SkyDiveContrail = GetPathName(playerPawnC.SkyDiveContrail) ?? playerActor.Cosmetics.SkyDiveContrail;

                if (playerPawnC.Dances != null)
                {
                    playerActor.Cosmetics.Dances = playerPawnC.Dances.Select(x => GetPathName(x)).ToList();
                }

                if (playerPawnC.ItemWraps != null)
                {
                    playerActor.Cosmetics.ItemWraps = playerPawnC.ItemWraps.Select(x => GetPathName(x)).ToList();
                }
            }
        }

        internal void UpdateVehicle(uint channelId, BaseVehicle baseVehicle)
        {
            if (!_vehicles.TryGetValue(channelId, out Vehicle vehicle))
            {
                NetGUIDToPathName.TryGetValue(baseVehicle.ChannelActor.Archetype.Value, out string carType);

                vehicle = new Vehicle
                {
                    SpawnLocation = baseVehicle.ChannelActor.Location,
                    SpawnRotation = baseVehicle.ChannelActor.Rotation,
                    VehicleName = carType
                };

                _vehicles[channelId] = vehicle;
            }

            if (baseVehicle.ReplicatedMovement != null) //Update location
            {
                PlayerLocationRepMovement newLocation = new PlayerLocationRepMovement
                {
                    RepLocation = baseVehicle.ReplicatedMovement,
                    WorldTime = GameState.CurrentWorldTime,
                    DeltaGameTimeSeconds = GameState.DeltaGameTime
                };

                vehicle.CurrentLocation = newLocation;
            }
        }

        internal void UpdateBatchedDamage(uint channelId, BatchedDamage batchedDamage)
        {
            if (!_playerPawns.TryGetValue(channelId, out PlayerPawn playerPawn))
            {
                //Shouldn't happen
                return;
            }

            if (!(playerPawn is Player))
            {
                //Nothing else currently
                return;
            }

            Player player = playerPawn as Player;

            WeaponShot shot = new WeaponShot
            {
                ShotByPlayerPawn = player,
                CriticalHitNonPlayer = batchedDamage.NonPlayerbIsCritical,
                IsCritical = batchedDamage.bIsCritical,
                FatalHitNonPlayer = batchedDamage.NonPlayerbIsFatal,
                IsFatal = batchedDamage.bIsFatal,
                WeaponActivate = batchedDamage.bWeaponActivate,
                IsBallistic = batchedDamage.bIsBallistic,
                IsShield = batchedDamage.bIsShield,
                Location = batchedDamage.Location,
                Damage = batchedDamage.Magnitude,
                Normal = batchedDamage.Normal,
                IsShieldDestroyed = batchedDamage.bIsShieldDestroyed,
                DeltaGameTimeSeconds = GameState.CurrentWorldTime - GameState.GameWorldStartTime,
                Weapon = player.CurrentWeapon
            };

            if (batchedDamage.HitActor > 0)
            {
                if (_actorToChannel.TryGetValue(batchedDamage.HitActor, out uint actorChannel))
                {
                    if (_playerPawns.TryGetValue(actorChannel, out PlayerPawn hitPlayerPawn))
                    {
                        shot.HitPlayerPawn = hitPlayerPawn;

                        Player hitPlayer = hitPlayerPawn as Player;
                        hitPlayer.DamageTaken.Add(shot);
                    }
                    else
                    {
                        //These are non player actors and other objects

                    }
                }
                else
                {
                    //Miss?
                }
            }
            else
            {
                //Hit nothing, not even the ground
            }

            player.Shots.Add(shot);
        }

        internal void UpdatePoiManager(FortPoiManager poiManager, NetFieldExportGroup networkGameplayTagNode)
        {
            if (networkGameplayTagNode == null || poiManager.PoiTagContainerTable == null)
            {
                return;
            }

            foreach (FGameplayTagContainer tagContainer in poiManager.PoiTagContainerTable)
            {
                tagContainer?.UpdateTags(networkGameplayTagNode);
            }

            GameState.PoiManager = (FortPoiManager)poiManager.Clone();
        }

        internal void UpdateFortInventory(uint channelId, FortInventory inventory)
        {
            if (inventory.ReplayPawn > 0)
            {
                _inventories[channelId] = (FortInventory)inventory.Clone();
            }
        }

        //Only occur on main player's inventory or server replays
        internal void UpdateNetDeltaFortInventory(NetDeltaUpdate deltaUpdate)
        {
            uint channel = 0;

            if (!_inventories.TryGetValue(deltaUpdate.ChannelIndex, out FortInventory inventory) ||
               !_actorToChannel.TryGetValue(inventory.ReplayPawn.Value, out channel) ||
               !_playerPawns.TryGetValue(channel, out PlayerPawn playerPawn))
            {
                QueuedPlayerPawn queuedPlayerSpawn = _queuedPlayerPawns.Values.FirstOrDefault(x => x.FirstOrDefault()?.ChannelId == channel)?.First();

                if (queuedPlayerSpawn == null)
                {
                    //Gets here when entering the bus. Items are deleted later
                    return;
                }

                queuedPlayerSpawn.InventoryUpdates.Add(deltaUpdate);

                return;
            }

            if (!(playerPawn is Player player))
            {
                return;
            }

            //Got on bus, possibly respawn too?
            if (player.ReplayPawnId != inventory.ReplayPawn.Value)
            {
                player.InventoryBeforeDeletes.Clear();
                player.InventoryBeforeDeletes.AddRange(player.CurrentInventory.Items);
                player.CurrentInventory.Clear();

                player.ReplayPawnId = inventory.ReplayPawn.Value;
            }

            if (deltaUpdate.Deleted)
            {
                //Cache inventory when deleting to hold on to inventory prior to death
                if (player.InventoryBaseReplicationKey != deltaUpdate.Header.BaseReplicationKey)
                {
                    player.InventoryBeforeDeletes.Clear();
                    player.InventoryBeforeDeletes.AddRange(player.CurrentInventory.Items);
                }

                player.CurrentInventory.DeleteIndex(deltaUpdate.ElementIndex);

                player.InventoryBaseReplicationKey = deltaUpdate.Header.BaseReplicationKey;
            }
            else
            {
                FortInventory fortInventory = deltaUpdate.Export as FortInventory;

                if (player.CurrentInventory.TryGetItem(deltaUpdate.ElementIndex, out InventoryItem item))
                {
                    item.LoadedAmmo = fortInventory.LoadedAmmo ?? item.LoadedAmmo;
                    item.Count = fortInventory.Count ?? item.Count;
                }
                else
                {
                    string itemName = String.Empty;

                    if (fortInventory.ItemDefinition != null)
                    {
                        NetGUIDToPathName.TryGetValue(fortInventory.ItemDefinition.Value, out itemName);
                    }

                    InventoryItem inventoryItem = new InventoryItem
                    {
                        Count = fortInventory.Count ?? 0,
                        ItemDefinition = fortInventory.ItemDefinition?.Value ?? 0,
                        Item = new ItemName(itemName),
                        LoadedAmmo = fortInventory.LoadedAmmo ?? 0,
                        UniqueWeaponId = new UniqueItemId
                        {
                            A = fortInventory.A ?? 0,
                            B = fortInventory.B ?? 0,
                            C = fortInventory.C ?? 0,
                            D = fortInventory.D ?? 0
                        }
                    };

                    player.CurrentInventory.TryAddItem(deltaUpdate.ElementIndex, inventoryItem);
                }
            }
        }

        internal void UpdateNetDeltaPrivateTeamInfo(NetDeltaUpdate deltaUpdate)
        {
            if (!_privateTeamInfo.TryGetValue(deltaUpdate.ChannelIndex, out NetDeltaArray<PrivateTeamInfo> teamInfo))
            {
                return;
            }

            if (deltaUpdate.Deleted)
            {
                teamInfo.DeleteIndex(deltaUpdate.ElementIndex);
            }
            else
            {
                FortTeamPrivateInfo privateInfo = deltaUpdate.Export as FortTeamPrivateInfo;

                if (!teamInfo.TryGetItem(deltaUpdate.ElementIndex, out PrivateTeamInfo item))
                {
                    item = new PrivateTeamInfo();
                    teamInfo.TryAddItem(deltaUpdate.ElementIndex, item);
                }

                item.Value = privateInfo.Value ?? item.Value;
                item.PlayerId = privateInfo.PlayerID ?? item.PlayerId;

                item.LastYaw = privateInfo.LastRepYaw ?? item.LastYaw;
                item.PawnStateMask = privateInfo.PawnStateMask != EFortPawnState.EFortPawnState_MAX ? privateInfo.PawnStateMask : item.PawnStateMask;

                if (privateInfo.PlayerState != null)
                {
                    if (_actorToChannel.TryGetValue(privateInfo.PlayerState.Value, out uint playerStateChannel))
                    {
                        if (_players.TryGetValue(playerStateChannel, out Player player))
                        {
                            item.PlayerState = player;
                        }
                    }
                }

                //Ignores issue with playstate actor id not being found at first
                if (item.PlayerState != null && !IgnoreLocationUpdate(item.PlayerState, GameState.DeltaGameTime) && privateInfo.LastRepLocation != null)
                {
                    item.LastLocation = privateInfo.LastRepLocation;
                    item.PlayerState.PrivateTeamLocations.Add(new PlayerLocation
                    {
                        WorldTime = GameState.CurrentWorldTime,
                        Location = item.LastLocation,
                        Yaw = item.LastYaw
                    });
                }
            }
        }

        internal void UpdateFortPickup(uint channelId, FortPickup pickup)
        {
            bool isNewItem = !_floorLoot.TryGetValue(channelId, out InventoryItem newItem);

            if (isNewItem)
            {
                newItem = new InventoryItem();
                _floorLoot.TryAdd(channelId, newItem);
            }

            if (pickup.ItemDefinition != null)
            {
                if (NetGUIDToPathName.TryGetValue(pickup.ItemDefinition.Value, out string weaponPathName))
                {
                    newItem.Item = new ItemName(weaponPathName);
                }
            }

            if (pickup.A != null)
            {
                newItem.UniqueWeaponId = new UniqueItemId
                {
                    A = pickup.A ?? 0,
                    B = pickup.B ?? 0,
                    C = pickup.C ?? 0,
                    D = pickup.D ?? 0
                };
            }

            newItem.ItemDefinition = pickup.ItemDefinition?.Value ?? newItem.ItemDefinition;
            newItem.InitialPosition = pickup.LootInitialPosition ?? newItem.InitialPosition;
            newItem.Count = pickup.Count ?? newItem.Count;
            newItem.LoadedAmmo = pickup.LoadedAmmo ?? newItem.LoadedAmmo;

            //Need to test if it's correct
            if (pickup.CombineTarget != null)
            {
                if (_actorToChannel.TryGetValue(pickup.CombineTarget.Value, out uint actorChannel))
                {
                    if (_floorLoot.TryGetValue(actorChannel, out InventoryItem combinedItem))
                    {
                        newItem.CombineTarget = combinedItem;
                    }
                }
            }

            if (pickup.PawnWhoDroppedPickup != null)
            {
                if (_actorToChannel.TryGetValue(pickup.PawnWhoDroppedPickup.Value, out uint actorChannel))
                {
                    if (_playerPawns.TryGetValue(actorChannel, out PlayerPawn playerPawn))
                    {
                        newItem.LastDroppedBy = playerPawn as Player;
                    }
                }
            }
        }

        internal void UpdateHealth(uint channelId, HealthSet health, NetGuidCache cache)
        {
            if (_players.TryGetValue(channelId, out Player player))
            {
                if (_healthSetStartingHandles.Count == 0 && cache.NetFieldExportGroupMap.TryGetValue("FortRegenHealthSet", out NetFieldExportGroup healthSetExport))
                {
                    List<NetFieldExport> maxHandles = healthSetExport.NetFieldExports.Where(x => x?.Name == "Maximum").ToList();

                    if (maxHandles.Count > 0)
                    {
                        _healthSetStartingHandles.TryAdd("Health", maxHandles[0].Handle - 3);
                    }

                    if (maxHandles.Count > 1)
                    {
                        _healthSetStartingHandles.TryAdd("Shield", maxHandles[1].Handle - 3);
                    }
                }

                if (_healthSetStartingHandles.TryGetValue("Health", out uint healthHandle))
                {
                    health.HealthFortSet = CreateFortSet(healthHandle);
                }

                if (_healthSetStartingHandles.TryGetValue("Shield", out uint shieldHandle))
                {
                    health.ShieldFortSet = CreateFortSet(shieldHandle);
                }

                HealthUpdate update = new HealthUpdate
                {
                    Health = (HealthSet)health.Clone(),
                    DeltaGameTimeSeconds = GameState.CurrentWorldTime - GameState.GameWorldStartTime
                };

                player.HealthChanges.Add(update);
            }

            FortSet CreateFortSet(uint startingHandle)
            {
                FortSet fortSet = new FortSet
                {
                    BaseValue = GetValue(startingHandle),
                    CurrentValue = GetValue(startingHandle + 1),
                    Maximum = GetValue(startingHandle + 3),
                    UnclampedBaseValue = GetValue(startingHandle + 7),
                    UnclampedCurrentValue = GetValue(startingHandle + 8)
                };

                return fortSet;
            }

            float? GetValue(uint handle)
            {
                if (health.UnknownHandles.Remove(handle, out object val))
                {
                    return (float)val;
                }

                return null;
            }
        }

        internal void HandleWeapon(uint channelId, BaseWeapon weapon)
        {
            bool isNewWeapon = !_weapons.TryGetValue(channelId, out Weapon newWeapon);

            //Updates the current weapon instead of creating a new one
            if (isNewWeapon)
            {
                isNewWeapon = !_unknownWeapons.TryGetValue(weapon.ChannelActor.ActorNetGUID.Value, out newWeapon);
                _unknownWeapons.Remove(weapon.ChannelActor.ActorNetGUID.Value);

                //Pulled from unknown weapons, so add to current weapons
                if (!isNewWeapon)
                {
                    _weapons.TryAdd(channelId, newWeapon);
                }
            }

            if (isNewWeapon)
            {
                newWeapon = new Weapon();

                _weapons.TryAdd(channelId, newWeapon);
            }

            newWeapon.IsEquipping = weapon.bIsEquippingWeapon ?? newWeapon.IsEquipping;
            newWeapon.IsReloading = weapon.bIsReloadingWeapon ?? newWeapon.IsReloading;
            newWeapon.WeaponLevel = weapon.WeaponLevel ?? newWeapon.WeaponLevel;
            newWeapon.Ammo = weapon.AmmoCount ?? newWeapon.Ammo;
            newWeapon.LastFireTime = weapon.LastFireTimeVerified ?? newWeapon.LastFireTime;

            if (weapon.A != null)
            {
                newWeapon.UniqueItemId = new UniqueItemId
                {
                    A = weapon.A ?? 0,
                    B = weapon.B ?? 0,
                    C = weapon.C ?? 0,
                    D = weapon.D ?? 0
                };
            }

            if (weapon.WeaponData != null)
            {
                NetGUIDToPathName.TryGetValue(weapon.WeaponData.Value, out string itemName);

                newWeapon.Item = new ItemName(itemName);
                newWeapon.ItemId = weapon.WeaponData.Value;
            }

            if (weapon.Owner != null)
            {
                if (_actorToChannel.TryGetValue(weapon.Owner.Value, out uint channel))
                {
                    if (_playerPawns.TryGetValue(channel, out PlayerPawn playerPawn))
                    {
                        Player player = playerPawn as Player;

                        newWeapon.Owner = player;

                        if (player.CurrentInventory.Count > 0)
                        {
                            InventoryItem inventoryItem = player.CurrentInventory.Items.FirstOrDefault(x => x.UniqueWeaponId.Equals(newWeapon.UniqueItemId));

                            if (inventoryItem != null)
                            {
                                inventoryItem.Weapon = newWeapon;
                                newWeapon.InventoryItem = inventoryItem;
                            }
                        }
                    }
                }
            }
        }

        internal void HandleDeltaNetRead(NetDeltaUpdate deltaUpdate)
        {
            switch (deltaUpdate.ExportGroup.PathName)
            {
                case "FortTeamPrivateInfo_ClassNetCache":
                    UpdateNetDeltaPrivateTeamInfo(deltaUpdate);
                    break;
                case "FortInventory_ClassNetCache":
                    if (Settings.IgnoreInventory)
                    {
                        return;
                    }

                    UpdateNetDeltaFortInventory(deltaUpdate);
                    break;
            }

            //GameMemberInfoArray
            //Can be used to get remaining teams/players in game
        }

        private void HandleQueuedPlayerPawns(Player player)
        {
            if (_queuedPlayerPawns.Remove(player.Actor.ActorNetGUID.Value, out List<QueuedPlayerPawn> playerPawns))
            {
                foreach (QueuedPlayerPawn playerPawn in playerPawns)
                {
                    UpdatePlayerPawn(playerPawn.ChannelId, playerPawn.PlayerPawn);

                    foreach (NetDeltaUpdate inventoryUpdate in playerPawn.InventoryUpdates)
                    {
                        UpdateNetDeltaFortInventory(inventoryUpdate);
                    }
                }
            }
        }

        private bool IgnoreLocationUpdate(Player player, float currentUpdateDeltaTime)
        {
            bool isPlayer = player.IsPlayersReplay == true;
            bool isTeammate = _replayPlayer?.Teamindex == player.Teamindex;

            bool shouldIgnore = false;

            switch (Settings.PlayerLocationType)
            {
                case LocationTypes.Team:
                    shouldIgnore = !isTeammate || !isPlayer;
                    break;
                case LocationTypes.User:
                    shouldIgnore = !isPlayer;
                    break;
                case LocationTypes.None:
                    shouldIgnore = true;
                    break;
            }

            if (shouldIgnore || Settings.LocationChangeDeltaMS == 0)
            {
                return shouldIgnore;
            }

            //Check to see if should update
            PlayerLocationRepMovement lastLocation = player.Locations.LastOrDefault();
            PlayerLocation privateTeamLocation = player.PrivateTeamLocations.LastOrDefault();

            if (lastLocation == null && privateTeamLocation == null)
            {
                return shouldIgnore;
            }

            double lastLocationDeltaTime = lastLocation?.DeltaGameTimeSeconds ?? privateTeamLocation.DeltaGameTimeSeconds;

            double delta = (double)Settings.LocationChangeDeltaMS / 1000;

            if (delta != 0 && currentUpdateDeltaTime - lastLocationDeltaTime < delta)
            {
                return true;
            }

            return shouldIgnore;
        }

        private string GetPathName(ItemDefinitionGUID guid)
        {
            if (guid == null || !guid.IsValid())
            {
                return null;
            }

            if (NetGUIDToPathName.TryGetValue(guid.Value, out string pathname))
            {
                return pathname;
            }

            return String.Empty;
        }

        internal void UpdateBuild(uint channelId, BaseStructure baseBuild)
        {
            PlayerStructure newStructure = new PlayerStructure();

            if (!_playerStructures.TryAdd(channelId, newStructure))
            {
                _playerStructures.TryGetValue(channelId, out newStructure);
            }

            newStructure.Location = baseBuild.ChannelActor.Location;
            newStructure.Rotation = baseBuild.ChannelActor.Rotation;
            newStructure.CurrentHealth = baseBuild.Health ?? newStructure.CurrentHealth;
            newStructure.MaxHealth = baseBuild.MaxHealth ?? newStructure.MaxHealth;

            if (baseBuild.TeamIndex != null)
            {
                if (_teams.TryGetValue(baseBuild.TeamIndex.Value, out Team team))
                {
                    newStructure.Team = team;
                }
            }

            switch (baseBuild)
            {
                case IWoodStructure wood:
                    newStructure.MaterialType = MaterialType.Wood;
                    break;
                case IBrickStructure brick:
                    newStructure.MaterialType = MaterialType.Brick;
                    break;
                case IMetalStructure meta:
                    newStructure.MaterialType = MaterialType.Metal;
                    break;
            }

            //Can do individual edits here later
            switch (baseBuild)
            {
                case BaseWallStructure wall:
                    newStructure.BaseStructureType = BaseStructureType.Wall;
                    break;
                case BaseFloorStructure floor:
                    newStructure.BaseStructureType = BaseStructureType.Floor;
                    break;
                case BaseRoofStructure roof:
                    newStructure.BaseStructureType = BaseStructureType.Roof;
                    break;
                case BaseStairsStructure stairs:
                    newStructure.BaseStructureType = BaseStructureType.Stairs;
                    break;
            }
        }

        internal void MiniGameUpdate(uint channelId, MiniGameCreated minigame, NetFieldExportGroup networkGameplayTagNode)
        {
            MinigameInformation ??= new MinigameInformation();

            MinigameInformation.TimeLimit = minigame.TimeLimit ?? MinigameInformation.TimeLimit;
            MinigameInformation.TotalRounds = minigame.TotalRounds ?? MinigameInformation.TotalRounds;
            MinigameInformation.WarmupDuration = minigame.WarmupDuration ?? MinigameInformation.WarmupDuration;
            MinigameInformation.GameResetDelay = minigame.PostGameResetDelay ?? MinigameInformation.GameResetDelay;
            MinigameInformation.RoundScoreDisplayTime = minigame.RoundScoreDisplayTime ?? MinigameInformation.RoundScoreDisplayTime;
            MinigameInformation.GameScoreDisplayTime = minigame.GameScoreDisplayTime ?? MinigameInformation.GameScoreDisplayTime;
            MinigameInformation.RoundWinnerDisplayTime = minigame.RoundWinnerDisplayTime ?? MinigameInformation.RoundWinnerDisplayTime;
            MinigameInformation.WinnerDisplayTime = minigame.GameWinnerDisplayTime ?? MinigameInformation.WinnerDisplayTime;
            MinigameInformation.WinCondition = minigame.WinCondition != EMinigameWinCondition.EMinigameWinCondition_MAX ? minigame.WinCondition : MinigameInformation.WinCondition;
            MinigameInformation.State = minigame.CurrentState != EFortMinigameState.EFortMinigameState_MAX ? minigame.CurrentState : MinigameInformation.State;
            MinigameInformation.CurrentRound = minigame.CurrentRound ?? MinigameInformation.CurrentRound;

            GameRound currentRound = MinigameInformation.Rounds.LastOrDefault();
            bool roundOver = MinigameInformation.State == EFortMinigameState.PostGameReset || MinigameInformation.State == EFortMinigameState.PostGameEnd;

            if (currentRound != null)
            {
                if (minigame.StartTime != null)
                {
                    currentRound.DeltaStartTime = minigame.StartTime.Value - GameState.GameWorldStartTime + MinigameInformation.WarmupDuration;
                }
                else if (minigame.ResetTime.HasValue)
                {
                    currentRound.DeltaEndTime = minigame.ResetTime.Value -
                        (GameState.GameWorldStartTime + MinigameInformation.RoundWinnerDisplayTime + MinigameInformation.RoundScoreDisplayTime);
                }
                else if (minigame.EndTime.HasValue)
                {
                    currentRound.DeltaEndTime = minigame.EndTime.Value -
                        (GameState.GameWorldStartTime + MinigameInformation.WinnerDisplayTime + MinigameInformation.GameScoreDisplayTime);
                }
            }

            //New round
            if (minigame.CurrentState == EFortMinigameState.Transitioning)
            {
                MinigameInformation.Rounds.Add(new GameRound());
            }
            else if (minigame.CurrentState == EFortMinigameState.InProgress && MinigameInformation.Rounds.Count == 0) //Join in progress?
            {
                currentRound = new GameRound();
                MinigameInformation.Rounds.Add(currentRound);
            }

            if (minigame.TeamArray != null)
            {
                for (int i = 0; i < minigame.TeamArray.Length; i++)
                {
                    if (MinigameInformation.TeamInfo == null)
                    {
                        MinigameInformation.TeamInfo = new RoundTeam[minigame.TeamArray.Length];
                    }

                    if (MinigameInformation.TeamInfo[i] == null)
                    {
                        MinigameInformation.TeamInfo[i] = new RoundTeam();
                    }
                    else if (roundOver)
                    {
                        MinigameInformation.TeamInfo[i] = new RoundTeam(MinigameInformation.TeamInfo[i]);

                        //Update bucket reference
                        var bucket = MinigameInformation.PlayerBuckets.FirstOrDefault(x => x.TeamIndex == MinigameInformation.TeamInfo[i].TeamIndex);

                        if (bucket != null)
                        {
                            bucket.Team = MinigameInformation.TeamInfo[i];
                        }
                    }

                    var teamInfo = minigame.TeamArray[i];
                    RoundTeam roundTeam = MinigameInformation.TeamInfo[i];

                    if (teamInfo != null)
                    {
                        roundTeam.CurrentTeamSize = teamInfo.TeamSize ?? roundTeam.CurrentTeamSize;
                        roundTeam.MaxSize = teamInfo.MaxInitTeamSize ?? roundTeam.MaxSize;
                        roundTeam.TeamIndex = teamInfo.TeamIndex ?? roundTeam.TeamIndex;
                        roundTeam.TeamColorIndex = teamInfo.TeamColorIndex ?? roundTeam.TeamColorIndex;
                        roundTeam.Eliminations = teamInfo.EliminatedCount ?? roundTeam.Eliminations;

                        if (roundTeam.MaxSize == -1)
                        {
                            roundTeam.MaxSize = 1;
                        }
                    }
                }
            }

            //Updates the team at the start to handle scoreboard
            if (minigame.CurrentState == EFortMinigameState.InProgress)
            {
                MinigameInformation.CurrentRoundTeams = new HashSet<int>(MinigameInformation.TeamInfo.Where(x => x.CurrentTeamSize > 0).Select(x => x.TeamIndex));
            }

            if (roundOver)
            {
                currentRound.Teams = MinigameInformation.TeamInfo.Where(x => MinigameInformation.CurrentRoundTeams.Contains(x.TeamIndex)).ToList();
            }

            if (minigame.PlayerBuckets != null && (int)MinigameInformation.State > 0)
            {
                if (MinigameInformation.PlayerBuckets == null)
                {
                    MinigameInformation.PlayerBuckets = new MinigameInformation.PlayerBucket[minigame.PlayerBuckets.Length];
                }
                else if (minigame.PlayerBuckets.Length > MinigameInformation.PlayerBuckets.Length)
                {
                    MinigameInformation.PlayerBucket[] arr = MinigameInformation.PlayerBuckets;

                    Array.Resize(ref arr, minigame.PlayerBuckets.Length);

                    MinigameInformation.PlayerBuckets = arr;
                }

                for (int i = 0; i < minigame.PlayerBuckets.Length; i++)
                {
                    if (MinigameInformation.PlayerBuckets[i] == null)
                    {
                        MinigameInformation.PlayerBuckets[i] = new MinigameInformation.PlayerBucket();
                    }

                    var bucket = minigame.PlayerBuckets[i];
                    var miniGameBucket = MinigameInformation.PlayerBuckets[i];

                    if (bucket != null)
                    {
                        miniGameBucket.TeamIndex = bucket.TeamIdAtGameStart ?? miniGameBucket.TeamIndex;

                        if (bucket.TeamIdAtGameStart > 0)
                        {
                            RoundTeam teamInfo = MinigameInformation.TeamInfo.FirstOrDefault(x => x.TeamIndex == bucket.TeamIdAtGameStart);

                            miniGameBucket.Team = teamInfo;
                        }

                        if (miniGameBucket.Team.PlayerIds == null)
                        {
                            miniGameBucket.Team.PlayerIds = new RoundPlayer[MinigameInformation.TeamInfo[i].MaxSize];
                        }

                        if (bucket.PlayerIds != null)
                        {
                            if (bucket.PlayerIds.RemoveAll)
                            {
                                foreach (RoundPlayer player in miniGameBucket.Team.PlayerIds)
                                {
                                    if (player != null)
                                    {
                                        player.HasLeft = true;
                                    }
                                }
                            }
                            else if (bucket.PlayerIds.RemoveId.HasValue)
                            {
                                RoundPlayer roundPlayer = miniGameBucket.Team.PlayerIds[bucket.PlayerIds.RemoveId.Value];

                                if (roundPlayer != null)
                                {
                                    roundPlayer.HasLeft = true;
                                }
                                else
                                {

                                }
                            }
                            else
                            {
                                for (int z = 0; z < (bucket.PlayerIds.Ids?.Length ?? 0); z++)
                                {
                                    string id = bucket.PlayerIds.Ids[z];

                                    if (id == null)
                                    {
                                        continue;
                                    }

                                    //Player left + reordering of indexes
                                    if (miniGameBucket.Team.PlayerIds[z] != null)
                                    {
                                        RoundPlayer leftPlayer = miniGameBucket.Team.PlayerIds[z];

                                        leftPlayer.HasLeft = true;

                                        for (int x = z + 1; x < miniGameBucket.Team.PlayerIds.Length; x++)
                                        {
                                            //Swap locations to handle "has left" property
                                            if (miniGameBucket.Team.PlayerIds[x].PlayerId == id)
                                            {
                                                miniGameBucket.Team.PlayerIds[z] = miniGameBucket.Team.PlayerIds[x];
                                                miniGameBucket.Team.PlayerIds[x] = leftPlayer;

                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        RoundPlayer roundPlayer = new RoundPlayer
                                        {
                                            PlayerId = id
                                        };

                                        if (_playerById.TryGetValue(id, out Player player))
                                        {
                                            roundPlayer.Player = player;
                                        }

                                        miniGameBucket.Team.PlayerIds[z] = roundPlayer;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
