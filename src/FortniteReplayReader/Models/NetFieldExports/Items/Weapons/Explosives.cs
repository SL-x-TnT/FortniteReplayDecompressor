﻿using System;
using System.Collections.Generic;
using System.Text;
using FortniteReplayReader.Models.NetFieldExports;
using Unreal.Core.Attributes;
using Unreal.Core.Models.Enums;

namespace FortniteReplayReader.Models.Items.Weapons
{
    public class Explosives : BaseWeapon
    {
    }

    public class Launcher : Explosives
    {
        [NetFieldExport("ReplicatedMovement", RepLayoutCmdType.RepMovement)]
        public FRepMovementWholeNumber ReplicatedMovement { get; set; }

        [NetFieldExport("GravityScale", RepLayoutCmdType.PropertyFloat)]
        public float? GravityScale { get; set; }

        [NetFieldExport("Team", RepLayoutCmdType.Enum)]
        public int? Team { get; set; }
    }

    [NetFieldExportGroup("/Game/Weapons/FORT_RocketLaunchers/Blueprints/B_RocketLauncher_Generic_Athena.B_RocketLauncher_Generic_Athena_C", ParseType.Normal)]
    public class RocketLauncher : Launcher
    {
    }

    [NetFieldExportGroup("/Game/Weapons/FORT_RocketLaunchers/Blueprints/B_RocketLauncher_Generic_Athena_HighTier.B_RocketLauncher_Generic_Athena_HighTier_C", ParseType.Normal)]
    public class RocketLauncherHighTier : RocketLauncher
    {
    }

    [NetFieldExportGroup("/Game/Weapons/FORT_RocketLaunchers/Blueprints/B_RocketLauncher_Military_Athena.B_RocketLauncher_Military_Athena_C", ParseType.Normal)]
    public class QuadLauncher : Launcher
    {
    }

    [NetFieldExportGroup("/Game/Weapons/FORT_GrenadeLaunchers/Blueprints/B_GrenadeLauncher_Generic_Athena.B_GrenadeLauncher_Generic_Athena_C", ParseType.Normal)]
    public class GrenadeLauncher : Launcher
    {
    }

    [NetFieldExportGroup("/Game/Weapons/FORT_RocketLaunchers/Blueprints/B_Prj_Pumpkin_RPG_Athena_LowTier.B_Prj_Pumpkin_RPG_Athena_LowTier_C", ParseType.Normal)]
    public class PumpkinLauncher : Launcher
    {
    }

    [NetFieldExportGroup("/Game/Weapons/FORT_RocketLaunchers/Blueprints/B_Launcher_Pumpkin_RPG_Athena.B_Launcher_Pumpkin_RPG_Athena_C", ParseType.Normal)]
    public class PumpkinLauncherHighTier : PumpkinLauncher
    {
    }

    [NetFieldExportGroup("/Game/Weapons/FORT_GrenadeLaunchers/Blueprints/B_GrenadeLauncher_Prox_Athena.B_GrenadeLauncher_Prox_Athena_C", ParseType.Normal)]
    public class ProximityLauncher : Launcher
    {
    }

    [NetFieldExportGroup("/Game/Abilities/Player/Generic/UtilityItems/B_Grenade_Frag_Athena.B_Grenade_Frag_Athena_C", ParseType.Normal)]
    public class FragGrenade : Explosives
    {
    }
}