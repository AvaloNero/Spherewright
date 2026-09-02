using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Factory;
using Spherewright.Contracts.Logistics;
using Spherewright.Contracts.Players;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class CanonicalStateHashTests
{
    [Fact]
    public void PlayerAction_IgnoresContinuousEnergyRecharge_ButBindsInventory()
    {
        var snapshot = new PlayerStateSnapshot
        {
            SessionId = "session",
            PlanetId = 103,
            IsAlive = true,
            IsOnPlanet = true,
            MovementState = "Walk",
            CoreEnergy = 100d,
            CoreEnergyCapacity = 1_000d,
            ReactorEnergy = 120d,
            ReactorItemId = 1006,
            ReactorItemInc = 0,
            AutoReplenishFuel = true,
            FuelStorageSlotCount = 4,
            InventorySlotCount = 40,
            ConstructionDrones = new ConstructionDroneSnapshot
            {
                Enabled = true,
                ConstructionEnabled = true,
                Total = 3,
                Alive = 3,
                Idle = 3,
            },
        };

        var beforeRecharge = CanonicalStateHash.PlayerAction(snapshot);
        snapshot.CoreEnergy = 200d;
        Assert.Equal(beforeRecharge, CanonicalStateHash.PlayerAction(snapshot));
        snapshot.ReactorEnergy = 60d;
        Assert.Equal(beforeRecharge, CanonicalStateHash.PlayerAction(snapshot));

        snapshot.FuelStorage.Add(new PlayerInventoryItem
        {
            ItemId = 1006,
            Count = 3,
            SlotCount = 1,
        });
        snapshot.FuelStorageOccupiedSlotCount = 1;
        var withFuel = CanonicalStateHash.PlayerAction(snapshot);
        Assert.NotEqual(beforeRecharge, withFuel);

        snapshot.Inventory.Add(new PlayerInventoryItem
        {
            ItemId = 1001,
            Count = 1,
            SlotCount = 1,
        });
        snapshot.InventoryOccupiedSlotCount = 1;
        Assert.NotEqual(withFuel, CanonicalStateHash.PlayerAction(snapshot));
    }

    [Fact]
    public void Factory_BindsSorterTopologyFilterAndHeldCargo()
    {
        var snapshot = new FactoryEntitySnapshot
        {
            SessionId = "session",
            PlanetId = 103,
            ObjectId = 12,
            ObjectKind = FactoryObjectKinds.Entity,
            ItemId = 2011,
            ComponentKind = "inserter",
            PickTargetObjectId = 10,
            InsertTargetObjectId = 20,
            InserterStage = "Picking",
            InserterStackCount = 0,
        };

        var unfiltered = CanonicalStateHash.Factory(snapshot);
        snapshot.FilterItemId = 1120;
        var filtered = CanonicalStateHash.Factory(snapshot);
        Assert.NotEqual(unfiltered, filtered);

        snapshot.Buffers.Add(new FactoryBufferSnapshot
        {
            Role = "inserter-held",
            ItemId = 1120,
            Count = 1,
        });
        snapshot.InserterStage = "Sending";
        snapshot.InserterStackCount = 1;
        Assert.NotEqual(filtered, CanonicalStateHash.Factory(snapshot));
    }

    [Fact]
    public void FactoryEndpoint_IgnoresProductionProgressAndBuffers_ButBindsConnections()
    {
        var snapshot = new FactoryEntitySnapshot
        {
            SessionId = "session",
            PlanetId = 103,
            ObjectId = 12,
            ObjectKind = FactoryObjectKinds.Entity,
            ItemId = 2301,
            ComponentKind = "miner",
            Position = new Vector3Snapshot { X = 1f, Y = 2f, Z = 3f },
            Rotation = new QuaternionSnapshot { W = 1f },
            Progress = 10,
        };
        snapshot.Buffers.Add(new FactoryBufferSnapshot
        {
            Role = "mined-output",
            ItemId = 1001,
            Count = 1,
        });

        var endpoint = CanonicalStateHash.FactoryEndpoint(snapshot);
        snapshot.Progress = 20;
        snapshot.Buffers[0].Count = 50;
        Assert.Equal(endpoint, CanonicalStateHash.FactoryEndpoint(snapshot));

        snapshot.Connections.Add(new FactoryConnectionSnapshot
        {
            Slot = 0,
            IsOutput = true,
            OtherObjectId = 13,
            OtherSlot = 1,
        });
        Assert.NotEqual(endpoint, CanonicalStateHash.FactoryEndpoint(snapshot));
    }

    [Fact]
    public void LogisticsStation_SeparatesVolatileInventoryFromConfiguration()
    {
        var snapshot = new LogisticsStationSnapshot
        {
            SessionId = "session",
            PlanetId = 103,
            EntityId = 12,
            StationId = 4,
            GalacticStationId = 9,
            BuildingItemId = 2104,
            IsInterstellar = true,
            Energy = 100,
            EnergyCapacity = 10_000,
            EnergyPerTick = 200,
            IdleDroneCount = 10,
            DroneTripRangeRaw = 180d,
            VesselTripRangeRaw = 12d,
            RemoteRoutePriority = "Ignore",
        };
        snapshot.StorageSlots.Add(new LogisticsStationStorageSlotSnapshot
        {
            Index = 0,
            ItemId = 1004,
            Count = 20,
            MaximumCount = 1_000,
            LocalLogic = "Supply",
            RemoteLogic = "Demand",
        });
        snapshot.BeltSlots.Add(new LogisticsStationBeltSlotSnapshot
        {
            Index = 0,
            Direction = "Input",
            BeltComponentId = 8,
            BeltEntityId = 19,
            StorageIndex = 0,
            Counter = 3,
        });

        var live = CanonicalStateHash.LogisticsStation(snapshot);
        var configuration = CanonicalStateHash.LogisticsStationConfiguration(snapshot);

        snapshot.Energy = 500;
        snapshot.StorageSlots[0].Count = 40;
        snapshot.BeltSlots[0].Counter = 4;
        Assert.NotEqual(live, CanonicalStateHash.LogisticsStation(snapshot));
        Assert.Equal(configuration, CanonicalStateHash.LogisticsStationConfiguration(snapshot));

        snapshot.StorageSlots[0].MaximumCount = 2_000;
        Assert.NotEqual(configuration, CanonicalStateHash.LogisticsStationConfiguration(snapshot));
    }
}
